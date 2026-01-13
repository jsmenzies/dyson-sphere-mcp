using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DSPMCP.Handlers;

namespace DSPMCP
{
    /// <summary>
    /// Handles research and technology-related queries.
    /// Exposes: current research, research speed per planet, tech tree status, upgrades.
    ///
    /// **Research Speed (Hashes/s) Derivation:**
    /// Based on the decompiled `LabComponent.InternalUpdateResearch` method, the effective hash generation per lab
    /// is a combination of several factors:
    /// - `GameMain.history.techSpeed`: A global research speed multiplier (e.g., 30 for a 3000% bonus).
    /// - `LabComponent.extraSpeed`: A property of the individual lab, significantly boosted by proliferation,
    ///   which contributes to 'extraHashBytes' generation.
    /// - `power`: A dynamic float factor, representing the power utilization ratio (0.0 to 1.0, or higher for bonuses)
    ///   of the lab's connected power network. This value is obtained from `powerSystem.networkServes` and
    ///   `PowerConsumerComponent.networkId`.
    ///
    /// The formula for hash generated per tick per lab is:
    /// `hash_per_tick_per_lab = GameMain.history.techSpeed * (1.0 + LabComponent.extraSpeed / 100000.0) * power`
    ///
    /// To convert this to **hash per second (Hashes/s)**, we multiply by `60.0` (60 ticks per second):
    /// `hash_per_second_per_lab = hash_per_tick_per_lab * 60.0`
    ///
    /// The `hashPerSecond` values reported by `get_research_by_planet`, `get_research_progress`,
    /// and `get_lab_details` now reflect this accurate per-second calculation.
    /// </summary>
    public class ResearchHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "get_research_progress", "Get current research tech, progress, and total hash rate" },
                { "get_research_by_planet", "Get research hash rate breakdown by planet" },
                { "get_tech_queue", "Get queued technologies for research" },
                { "get_upgrades", "Get mecha and infinity research upgrade levels" },
                {
                    "get_lab_details",
                    "Get details for all labs on a specific planet, including individual research speed and the globally researched tech."
                }
            };
        }

        public string Handle(string method, string id, string paramsJson)
        {
            if (GameMain.data == null)
                return JsonRpc.GameNotLoaded(id);

            try
            {
                switch (method)
                {
                    case "get_research_progress":
                        return GetResearchProgress(id);
                    case "get_research_by_planet":
                        return GetResearchByPlanet(id);
                    case "get_tech_queue":
                        return GetTechQueue(id);
                    case "get_upgrades":
                        return GetUpgrades(id);
                    case "get_lab_details":
                        return GetLabDetails(id, paramsJson);
                    default:
                        return JsonRpc.Error(id, -32601, $"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ResearchHandler.{method} error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        /// <summary>
        /// Get current research progress and total hash rate.
        /// </summary>
        private string GetResearchProgress(string id)
        {
            var history = GameMain.history;
            var currentTechId = history.currentTech;

            var json = new JsonBuilder().StartObject();

            // Current research
            if (currentTechId > 0)
            {
                var techProto = LDB.techs.Select(currentTechId);
                var techState = history.techStates[currentTechId];

                json.Key("currentTech").StartObject()
                    .Prop("id", currentTechId)
                    .Prop("name", techProto?.name?.Translate() ?? "Unknown")
                    .Prop("hashNeeded", techState.hashNeeded)
                    .Prop("hashUploaded", techState.hashUploaded)
                    .Prop("progressPercent", techState.hashNeeded > 0
                        ? Math.Round((double)techState.hashUploaded / techState.hashNeeded * 100, 2)
                        : 0)
                    .EndObject();
            }
            else
            {
                json.Key("currentTech").Value((string)null);
            }

            // Calculate total hash rate from all planets
            // Pass gameData to helper to access PlanetFactory for power calculations
            var totalHashPerTick = CalculateTotalHashPerTick(GameMain.data);
            var hashPerSecond = totalHashPerTick * 60.0; // 60 ticks per second

            json.Prop("totalHashPerSecond", Math.Round(hashPerSecond, 2));
            json.Prop("totalLabCount", CountTotalLabs());

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        /// <summary>
        /// Get research hash rate breakdown by planet.
        /// </summary>
        private string GetResearchByPlanet(string id)
        {
            var json = new JsonBuilder().StartObject();
            var gameData = GameMain.data;

            var totalHashPerTick = 0.0;
            var totalLabs = 0;

            json.Key("planets").StartArray();

            for (int i = 0; i < gameData.factoryCount; i++)
            {
                var factory = FindFactoryByPlanetId(gameData, gameData.factories[i].planet.id);
                if (factory == null) continue;


                var planet = factory.planet;
                var labPool = factory.factorySystem?.labPool;
                if (labPool == null) continue;

                var labCursor = factory.factorySystem.labCursor;
                var planetHashPerTick = 0.0;
                var planetLabCount = 0;
                var workingLabCount = 0;
                var idleLabCount = 0;

                // Get power system data for dynamic power calculation
                var powerSystem = factory.powerSystem;
                var networkServes = powerSystem.networkServes;
                var consumerPool = powerSystem.consumerPool;

                for (int j = 1; j < labCursor; j++)
                {
                    var lab = labPool[j];
                    if (lab.id != j) continue; // Skip invalid entries

                    // Only count labs in research mode (not matrix production mode)
                    if (lab.researchMode)
                    {
                        planetLabCount++;

                        if (lab.replicating)
                        {
                            workingLabCount++;
                            // Dynamically get the power utilization ratio for this lab
                            float power = networkServes[consumerPool[lab.pcId].networkId];
                            
                            // Hash per tick calculation, incorporating power and extraSpeed
                            // This matches the logic found in LabComponent.InternalUpdateResearch
                            planetHashPerTick += GameMain.history.techSpeed * (1.0 + (double)lab.extraSpeed / 100000.0) * power;
                        }
                        else
                        {
                            idleLabCount++;
                        }
                    }
                }

                if (planetLabCount > 0)
                {
                    var hashPerSecond = planetHashPerTick * 60.0;

                    json.StartObject()
                        .Prop("planetId", planet.id)
                        .Prop("planetName", planet.displayName ?? "Unknown")
                        .Prop("starName", planet.star?.displayName ?? "Unknown")
                        .Prop("labCount", planetLabCount)
                        .Prop("workingLabs", workingLabCount)
                        .Prop("idleLabs", idleLabCount)
                        .Prop("hashPerSecond", Math.Round(hashPerSecond, 2))
                        .EndObject();

                    totalHashPerTick += planetHashPerTick;
                    totalLabs += planetLabCount;
                }
            }

            json.EndArray();

            json.Prop("totalHashPerSecond", Math.Round(totalHashPerTick * 60.0, 2));
            json.Prop("totalLabCount", totalLabs);

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        /// <summary>
        /// Get queued technologies.
        /// </summary>
        private string GetTechQueue(string id)
        {
            var history = GameMain.history;
            var json = new JsonBuilder().StartObject();

            json.Key("queue").StartArray();

            // Current tech
            if (history.currentTech > 0)
            {
                var techProto = LDB.techs.Select(history.currentTech);
                var techState = history.techStates[history.currentTech];

                json.StartObject()
                    .Prop("id", history.currentTech)
                    .Prop("name", techProto?.name?.Translate() ?? "Unknown")
                    .Prop("position", 0)
                    .Prop("hashNeeded", techState.hashNeeded)
                    .Prop("hashUploaded", techState.hashUploaded)
                    .EndObject();
            }

            // Tech queue (if any) - techQueue is an int array
            var queueLength = history.techQueue?.Length ?? 0;
            for (int i = 0; i < queueLength; i++)
            {
                var techId = history.techQueue[i];
                if (techId <= 0) continue; // Skip empty slots

                var techProto = LDB.techs.Select(techId);
                var techState = history.techStates[techId];

                json.StartObject()
                    .Prop("id", techId)
                    .Prop("name", techProto?.name?.Translate() ?? "Unknown")
                    .Prop("position", i + 1)
                    .Prop("hashNeeded", techState.hashNeeded)
                    .Prop("hashUploaded", techState.hashUploaded)
                    .EndObject();
            }

            json.EndArray();

            // Count non-zero entries in queue
            var actualQueueCount = 0;
            if (history.techQueue != null)
            {
                foreach (var t in history.techQueue)
                    if (t > 0)
                        actualQueueCount++;
            }

            json.Prop("queueLength", actualQueueCount + (history.currentTech > 0 ? 1 : 0));

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        /// <summary>
        /// Get upgrade levels (mecha, research speed, etc.)
        /// </summary>
        private string GetUpgrades(string id)
        {
            var history = GameMain.history;
            var player = GameMain.mainPlayer;
            var mecha = player?.mecha;

            var json = new JsonBuilder().StartObject();

            // Research speed multiplier
            json.Key("research").StartObject()
                .Prop("techSpeed", history.techSpeed)
                .Prop("universeObserveLevel", history.universeObserveLevel)
                .EndObject();

            // Mecha upgrades
            if (mecha != null)
            {
                json.Key("mecha").StartObject()
                    .Prop("coreEnergyCap", mecha.coreEnergyCap)
                    .Prop("coreEnergy", mecha.coreEnergy)
                    .Prop("corePowerGen", mecha.corePowerGen)
                    .Prop("reactorPowerGen", mecha.reactorPowerGen)
                    .Prop("walkSpeed", mecha.walkSpeed)
                    .Prop("jumpSpeed", (double)mecha.jumpSpeed)
                    .Prop("maxSailSpeed", mecha.maxSailSpeed)
                    .Prop("maxWarpSpeed", mecha.maxWarpSpeed)
                    .Prop("buildArea", mecha.buildArea)
                    .EndObject();

                // Inventory size
                json.Key("inventory").StartObject()
                    .Prop("inventorySize", player.package?.size ?? 0)
                    .EndObject();
            }

            // Dyson sphere related unlocks
            json.Key("dyson").StartObject()
                .Prop("solarSailLife", history.solarSailLife)
                .Prop("solarEnergyLossRate", history.solarEnergyLossRate)
                .Prop("useIonLayer", history.useIonLayer)
                .EndObject();

            // Logistics unlocks
            json.Key("logisticsCapacity").StartObject()
                .Prop("stationDroneCount", history.logisticDroneCarries)
                .Prop("stationShipCount", history.logisticShipCarries)
                .Prop("stationWarperNecessary", history.logisticShipWarpDrive)
                .EndObject();

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        /// <summary>
        /// Get details for specific labs on a planet.
        /// </summary>
        private string GetLabDetails(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
            if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'planetId' parameter.");
            }

            {
                var json = new JsonBuilder().StartObject();
                var gameData = GameMain.data;
                var history = GameMain.history;

                // Get current research info
                var currentTechId = history.currentTech;
                if (currentTechId > 0)
                {
                    var techProto = LDB.techs.Select(currentTechId);
                    json.Key("currentTech").StartObject()
                        .Prop("id", currentTechId)
                        .Prop("name", techProto?.name?.Translate() ?? "Unknown")
                        .EndObject();
                }
                else
                {
                    json.Key("currentTech").Value((string)null);
                }

                // Find the factory for the given planetId
                PlanetFactory factory = FindFactoryByPlanetId(gameData, planetId);

                if (factory == null)
                {
                    return JsonRpc.Error(id, -32602, $"Planet with ID {planetId} not found.");
                }

                json.Prop("planetId", planetId);
                json.Prop("planetName", factory.planet.displayName ?? "Unknown");
                json.Key("labs").StartArray();

            var labPool = factory.factorySystem?.labPool;
            var labCursor = factory.factorySystem?.labCursor ?? 0;
            // var techSpeed = history.techSpeed; // No longer needed, directly use history.techSpeed

            // Get power system data for dynamic power calculation
            var powerSystem = factory.powerSystem;
            var networkServes = powerSystem.networkServes;
            var consumerPool = powerSystem.consumerPool;

            // Ensure labPool is not null before iterating
            if (labPool == null)
            {
                json.EndArray(); // Close labs array even if no labs
                json.EndObject();
                return JsonRpc.Success(id, json.ToString());
            }

            for (int j = 1; j < labCursor; j++)
            {
                var lab = labPool[j]; // Access directly after null check for labPool
                if (lab.id == 0) continue; // Check for invalid lab entry based on common struct patterns

                if (lab.researchMode) // Only interested in labs in research mode
                {
                    // Dynamically get the power utilization ratio for this lab
                    float power = networkServes[consumerPool[lab.pcId].networkId];

                    json.StartObject()
                        .Prop("id", lab.id)
                        .Prop("researchMode", lab.researchMode)
                        .Prop("replicating", lab.replicating)
                        .Prop("techId", lab.techId)
                        .Prop("techName", LDB.techs.Select(lab.techId)?.name?.Translate() ?? "Unknown");
                    
                    // Add other relevant speed properties from LabComponent
                    json.Prop("speed", lab.speed)
                        .Prop("speedOverride", lab.speedOverride)
                        .Prop("extraSpeed", lab.extraSpeed);

                    // Add the dynamically retrieved power value
                    json.Prop("power", Math.Round(power, 4));

                        // New fields related to hash and state
                        json.Prop("hashBytes", lab.hashBytes)
                            .Prop("extraHashBytes", lab.extraHashBytes)
                            .Prop("time", lab.time)
                            .Prop("cycleCount", lab.cycleCount)
                            .Prop("extraTime", lab.extraTime)
                            .Prop("extraCycleCount", lab.extraCycleCount)
                            .Prop("extraPowerRatio", lab.extraPowerRatio)
                            .Prop("productive", lab.productive)
                            .Prop("forceAccMode", lab.forceAccMode)
                            .Prop("incUsed", lab.incUsed)
                            .Prop("recipeId", lab.recipeId)
                            .Prop("timeSpend", lab.timeSpend)
                            .Prop("extraTimeSpend", lab.extraTimeSpend);

                        // Arrays - convert to JSON arrays
                        json.Key("matrixPoints").StartArray();
                        if (lab.matrixPoints != null)
                        {
                            foreach (var val in lab.matrixPoints)
                            {
                                json.Value(val);
                            }
                        }

                        json.EndArray();

                        json.Key("matrixServed").StartArray();
                        if (lab.matrixServed != null)
                        {
                            foreach (var val in lab.matrixServed)
                            {
                                json.Value(val);
                            }
                        }

                        json.EndArray();

                        json.Key("matrixIncServed").StartArray();
                        if (lab.matrixIncServed != null)
                        {
                            foreach (var val in lab.matrixIncServed)
                            {
                                json.Value(val);
                            }
                        }

                        json.EndArray();

                                            json.Prop("hashPerSecondContribution", lab.replicating ? (history.techSpeed * (1.0 + (double)lab.extraSpeed / 100000.0) * power * 60.0) : 0);
                        json.EndObject();
                    }
                }

                json.EndArray();

                json.EndObject();
                return JsonRpc.Success(id, json.ToString());
            }
        }

        // Helper method to find a PlanetFactory by its planetId
        private PlanetFactory FindFactoryByPlanetId(GameData gameData, int planetId)
        {
            for (int i = 0; i < gameData.factoryCount; i++)
            {
                if (gameData.factories[i]?.planet?.id == planetId)
                {
                    return gameData.factories[i];
                }
            }
            return null;
        }

        // ===== Helper Methods =====

        private double CalculateTotalHashPerTick(GameData gameData)
        {
            var totalHash = 0.0;
            var history = GameMain.history;

            for (int i = 0; i < gameData.factoryCount; i++)
            {
                var factory = gameData.factories[i];
                if (factory?.factorySystem?.labPool == null) continue;

                var labPool = factory.factorySystem.labPool;
                var labCursor = factory.factorySystem.labCursor;

                // Get power system data for dynamic power calculation
                var powerSystem = factory.powerSystem;
                var networkServes = powerSystem.networkServes;
                var consumerPool = powerSystem.consumerPool;

                for (int j = 1; j < labCursor; j++)
                {
                    var lab = labPool[j];
                    if (lab.id != j || !lab.researchMode || !lab.replicating) continue;

                    // Dynamically get the power utilization ratio for this lab
                    float power = networkServes[consumerPool[lab.pcId].networkId];
                    
                    // Hash per tick calculation, incorporating power and extraSpeed
                    // This matches the logic found in LabComponent.InternalUpdateResearch
                    totalHash += history.techSpeed * (1.0 + (double)lab.extraSpeed / 100000.0) * power;
                }
            }

            return totalHash;
        }

        private int CountTotalLabs()
        {
            var gameData = GameMain.data;
            var count = 0;

            for (int i = 0; i < gameData.factoryCount; i++)
            {
                var factory = gameData.factories[i];
                if (factory?.factorySystem?.labPool == null) continue;

                var labPool = factory.factorySystem.labPool;
                var labCursor = factory.factorySystem.labCursor;

                for (int j = 1; j < labCursor; j++)
                {
                    var lab = labPool[j];
                    if (lab.id != j || !lab.researchMode) continue;
                    count++;
                }
            }

            return count;
        }
    }
}