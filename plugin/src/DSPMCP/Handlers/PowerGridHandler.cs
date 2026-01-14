using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DSPMCP.Handlers;

namespace DSPMCP
{
    /// <summary>
    /// Handles power grid related queries.
    /// Exposes: power network status per planet, power generation/consumption stats.
    /// </summary>
    public class PowerGridHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "get_power_grid_status", "Get power grid status across all planets" },
                { "get_power_grids_by_planet", "Get detailed power network info for a specific planet" }
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
                    case "get_power_grid_status":
                        return GetPowerGridStatus(id);
                    case "get_power_grids_by_planet":
                        return GetPowerGridsByPlanet(id, paramsJson);
                    default:
                        return JsonRpc.Error(id, -32601, $"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"PowerGridHandler.{method} error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        /// <summary>
        /// Get power grid status across all planets.
        /// Uses ProductionStatistics to match the game UI exactly.
        /// Energy values are in Watts (per-second). Divide by 1,000,000 for MW, 1,000,000,000 for GW.
        /// </summary>
        private string GetPowerGridStatus(string id)
        {
            var json = new JsonBuilder().StartObject();
            var gameData = GameMain.data;
            var stats = gameData.statistics.production;

            json.Key("planets").StartArray();

            for (int i = 0; i < gameData.factoryCount; i++)
            {
                var factory = gameData.factories[i];
                if (factory == null || factory.powerSystem == null) continue;

                var planet = factory.planet;
                var powerSystem = factory.powerSystem;

                // Refresh ProductionStatistics for this specific planet (matches game UI)
                stats.RefreshPowerGenerationCapacites(planet.id);
                stats.RefreshPowerConsumptionDemands(planet.id);

                // Use precomputed values from ProductionStatistics (already in Watts!)
                long generationCapacityW = stats.totalGenCapacity;
                long consumptionDemandW = stats.totalConDemand;

                // Still need to calculate some values from networks (not in ProductionStatistics)
                long totalEnergyServed = 0;
                long totalEnergyAccumulated = 0;
                long totalEnergyStored = 0;
                int networkCount = 0;

                for (int netId = 1; netId < powerSystem.netCursor; netId++)
                {
                    var network = powerSystem.netPool[netId];
                    if (network == null) continue;

                    // Only count networks that have actual consumers, generators, or accumulators
                    int consumerCount = network.consumers?.Count ?? 0;
                    int generatorCount = network.generators?.Count ?? 0;
                    int accumulatorCount = network.accumulators?.Count ?? 0;
                    if (consumerCount == 0 && generatorCount == 0 && accumulatorCount == 0) continue;

                    networkCount++;
                    totalEnergyServed += network.energyServed;
                    totalEnergyAccumulated += network.energyAccumulated;
                    totalEnergyStored += network.energyStored;
                }

                // Calculate satisfaction using UI values
                double satisfactionPercent = 0.0;
                if (consumptionDemandW > 0)
                {
                    satisfactionPercent = Math.Round((double)totalEnergyServed * 60 / consumptionDemandW * 100.0, 2);
                }
                else if (generationCapacityW > 0)
                {
                    // No demand but we have capacity
                    satisfactionPercent = 100.0;
                }

                json.StartObject()
                    .Prop("planetId", planet.id)
                    .Prop("planetName", planet.displayName ?? "Unknown")
                    .Prop("starName", planet.star?.displayName ?? "Unknown")
                    .Prop("networkCount", networkCount)
                    // Power values in Watts (matches game UI exactly)
                    .Prop("generationCapacityW", generationCapacityW)
                    .Prop("consumptionDemandW", consumptionDemandW)
                    .Prop("actualConsumptionW", totalEnergyServed * 60)
                    .Prop("energyStored", totalEnergyStored)
                    .Prop("satisfactionPercent", satisfactionPercent)
                    .EndObject();
            }

            json.EndArray();
            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        /// <summary>
        /// Get detailed power network information for a specific planet.
        /// Returns individual network stats including generators, consumers, and accumulators.
        /// </summary>
        private string GetPowerGridsByPlanet(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
            if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'planetId' parameter.");
            }

            var gameData = GameMain.data;
            PlanetFactory factory = FindFactoryByPlanetId(gameData, planetId);

            if (factory == null)
            {
                return JsonRpc.Error(id, -32602, $"Planet with ID {planetId} not found.");
            }

            var json = new JsonBuilder().StartObject();
            var powerSystem = factory.powerSystem;
            var stats = gameData.statistics.production;

            // Refresh ProductionStatistics for this planet (matches game UI)
            stats.RefreshPowerGenerationCapacites(planetId);
            stats.RefreshPowerConsumptionDemands(planetId);

            json.Prop("planetId", planetId);
            json.Prop("planetName", factory.planet.displayName ?? "Unknown");
            // Add summary from ProductionStatistics (matches UI exactly)
            json.Prop("totalGenerationCapacityW", stats.totalGenCapacity);
            json.Prop("totalConsumptionDemandW", stats.totalConDemand);
            json.Key("networks").StartArray();

            for (int netId = 1; netId < powerSystem.netCursor; netId++)
            {
                var network = powerSystem.netPool[netId];
                if (network == null) continue;

                // Only include networks that have actual consumers, generators, or accumulators
                int consumerCount = network.consumers?.Count ?? 0;
                int generatorCount = network.generators?.Count ?? 0;
                int accumulatorCount = network.accumulators?.Count ?? 0;
                if (consumerCount == 0 && generatorCount == 0 && accumulatorCount == 0) continue;

                // Calculate satisfaction percentage for this network
                double satisfactionPercent = 0.0;
                if (network.energyRequired > 0)
                {
                    satisfactionPercent = Math.Round((double)network.energyServed / network.energyRequired * 100.0, 2);
                }
                else if (network.energyCapacity > 0)
                {
                    satisfactionPercent = 100.0;
                }

                json.StartObject()
                    .Prop("networkId", network.id)
                    .Prop("energyRequired", network.energyRequired)
                    .Prop("energyServed", network.energyServed)
                    .Prop("energyCapacity", network.energyCapacity)
                    .Prop("energyDischarge", network.energyDischarge)
                    .Prop("energyCharge", network.energyCharge)
                    .Prop("energyAccumulated", network.energyAccumulated)
                    .Prop("energyStored", network.energyStored)
                    .Prop("satisfactionPercent", satisfactionPercent)
                    .Prop("consumerRatio", Math.Round(network.consumerRatio, 4))
                    .Prop("generatorRatio", Math.Round(network.generaterRatio, 4))
                    .Prop("consumerCount", consumerCount)
                    .Prop("generatorCount", generatorCount)
                    .Prop("accumulatorCount", accumulatorCount)
                    .Prop("exchangerCount", network.exchangers?.Count ?? 0);

                // Add consumer details
                json.Key("consumers").StartArray();
                if (network.consumers != null)
                {
                    foreach (var consumerId in network.consumers)
                    {
                        if (consumerId >= powerSystem.consumerCursor) continue;
                        var consumer = powerSystem.consumerPool[consumerId];
                        if (consumer.id == 0) continue;

                        json.StartObject()
                            .Prop("consumerId", consumer.id)
                            .Prop("entityId", consumer.entityId)
                            .Prop("workEnergyPerTick", consumer.workEnergyPerTick)
                            .Prop("idleEnergyPerTick", consumer.idleEnergyPerTick)
                            .EndObject();
                    }
                }
                json.EndArray();

                // Add generator details
                json.Key("generators").StartArray();
                if (network.generators != null)
                {
                    foreach (var generatorId in network.generators)
                    {
                        if (generatorId >= powerSystem.genCursor) continue;
                        var generator = powerSystem.genPool[generatorId];
                        if (generator.id == 0) continue;

                        json.StartObject()
                            .Prop("generatorId", generator.id)
                            .Prop("entityId", generator.entityId)
                            .Prop("genEnergyPerTick", generator.genEnergyPerTick)
                            .Prop("photovoltaic", generator.photovoltaic)
                            .Prop("wind", generator.wind)
                            .Prop("gamma", generator.gamma)
                            .Prop("geothermal", generator.geothermal)
                            .Prop("fuelId", generator.fuelId)
                            .Prop("fuelCount", generator.fuelCount)
                            .Prop("productId", generator.productId)
                            .Prop("warmup", Math.Round(generator.warmup, 4))
                            .EndObject();
                    }
                }
                json.EndArray();

                // Add accumulator details
                json.Key("accumulators").StartArray();
                if (network.accumulators != null)
                {
                    foreach (var accumulatorId in network.accumulators)
                    {
                        if (accumulatorId >= powerSystem.accCursor) continue;
                        var accumulator = powerSystem.accPool[accumulatorId];
                        if (accumulator.id == 0) continue;

                        json.StartObject()
                            .Prop("accumulatorId", accumulator.id)
                            .Prop("entityId", accumulator.entityId)
                            .Prop("curEnergy", accumulator.curEnergy)
                            .Prop("maxEnergy", accumulator.maxEnergy)
                            .Prop("inputEnergyPerTick", accumulator.inputEnergyPerTick)
                            .Prop("outputEnergyPerTick", accumulator.outputEnergyPerTick)
                            .EndObject();
                    }
                }
                json.EndArray();

                json.EndObject(); // Close network object
            }

            json.EndArray(); // Close networks array
            json.EndObject(); // Close root object
            return JsonRpc.Success(id, json.ToString());
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
    }
}
