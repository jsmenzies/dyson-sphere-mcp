using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DSPMCP.Handlers
{
    public class LogisticsHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "list_ils_per_planet", "List Interstellar Logistics Stations (ILS) for each planet in the system." },
                { "get_ils_details", "Get detailed ILS information for a specific planet including items, storage, and ship counts." },
                { "get_shipping_routes_for_ils", "Get shipping routes for a given ILS (by gid), showing outgoing and incoming ships." },
                { "get_planet_routes", "Get all incoming and outgoing shipping routes for a specific planet." },
                { "find_item_transport", "Find all ships currently transporting a specific item (by ID)." }
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
                    case "list_ils_per_planet":
                        return ListIlsPerPlanet(id);
                    case "get_ils_details":
                        return GetIlsDetails(id, paramsJson);
                    case "get_shipping_routes_for_ils":
                        return GetShippingRoutes(id, paramsJson);
                    case "get_planet_routes":
                        return GetPlanetRoutes(id, paramsJson);
                    case "find_item_transport":
                        return FindItemTransport(id, paramsJson);
                    default:
                        return JsonRpc.Error(id, -32601, $"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"LogisticsHandler.{method} error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        private string ListIlsPerPlanet(string id)
        {
            var json = new JsonBuilder().StartObject();
            var gameData = GameMain.data;

            json.Key("planets").StartArray();

            for (int i = 0; i < gameData.factoryCount; i++)
            {
                var factory = gameData.factories[i];
                if (factory == null) continue;

                var planet = factory.planet;
                var transport = factory.transport;
                if (transport?.stationPool == null) continue;

                var stationPool = transport.stationPool;
                var stationCursor = transport.stationCursor;

                json.StartObject()
                    .Prop("planetId", planet.id)
                    .Prop("planetName", planet.displayName ?? "Unknown")
                    .Prop("starName", planet.star?.displayName ?? "Unknown")
                    .Key("ilsStations").StartArray();

                for (int j = 1; j < stationCursor; j++)
                {
                    var station = stationPool[j];
                    if (station == null || station.id == 0 || !station.isStellar) continue; // Only process active ILS

                    json.StartObject()
                        .Prop("id", station.id)
                        .Prop("gid", station.gid)
                        .Prop("entityId", station.entityId)
                        .Prop("isStellar", station.isStellar)
                        .Prop("deliveryDrones", station.deliveryDrones)
                        .Prop("deliveryShips", station.deliveryShips)
                        .Prop("idleDroneCount", station.idleDroneCount)
                        .Prop("workDroneCount", station.workDroneCount)
                        .Prop("idleShipCount", station.idleShipCount)
                        .Prop("workShipCount", station.workShipCount)
                        .Prop("warperCount", station.warperCount)
                        .Prop("warperMaxCount", station.warperMaxCount)
                        .Prop("energy", station.energy)
                        .Prop("energyMax", station.energyMax)
                        .Prop("energyPerTick", station.energyPerTick)
                        .Prop("remoteGroupMask", station.remoteGroupMask)
                        .Prop("routePriority", station.routePriority.ToString());

                    // Item Storage Details
                    json.Key("storage").StartArray();
                    if (station.storage != null)
                    {
                        for (int k = 0; k < station.storage.Length; k++)
                        {
                            var store = station.storage[k];
                            if (store.itemId > 0) // Only include occupied slots
                            {
                                string itemName = "Unknown";
                                try
                                {
                                    var itemProto = LDB.items.Select(store.itemId);
                                    if (itemProto != null && itemProto.name != null)
                                    {
                                        itemName = itemProto.name.Translate() ?? "Unknown";
                                    }
                                }
                                catch
                                {
                                    // Fallback to item ID if name lookup fails
                                    itemName = $"Item {store.itemId}";
                                }

                                json.StartObject()
                                    .Prop("itemId", store.itemId)
                                    .Prop("itemName", itemName)
                                    .Prop("count", store.count)
                                    .Prop("inc", store.inc)
                                    .Prop("max", store.max)
                                    .Prop("localLogic", store.localLogic.ToString())
                                    .Prop("remoteLogic", store.remoteLogic.ToString())
                                    .EndObject();
                            }
                        }
                    }

                    json.EndArray(); // End storage array

                    json.EndObject(); // End station object
                }

                json.EndArray(); // End ilsStations array
                json.EndObject(); // End planet object
            }

            json.EndArray(); // End planets array
            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private string GetIlsDetails(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
            if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'planetId' parameter.");
            }

            var json = new JsonBuilder().StartObject();
            var gameData = GameMain.data;

            // Find the factory for the given planetId
            PlanetFactory factory = null;
            for (int i = 0; i < gameData.factoryCount; i++)
            {
                if (gameData.factories[i]?.planet?.id == planetId)
                {
                    factory = gameData.factories[i];
                    break;
                }
            }

            if (factory == null)
            {
                return JsonRpc.Error(id, -32602, $"Planet with ID {planetId} not found.");
            }

            var planet = factory.planet;
            var transport = factory.transport;

            json.Prop("planetId", planet.id)
                .Prop("planetName", planet.displayName ?? "Unknown")
                .Prop("starName", planet.star?.displayName ?? "Unknown");

            if (transport?.stationPool == null)
            {
                json.Key("ilsStations").StartArray().EndArray();
                json.Prop("ilsCount", 0);
                json.EndObject();
                return JsonRpc.Success(id, json.ToString());
            }

            var stationPool = transport.stationPool;
            var stationCursor = transport.stationCursor;

            json.Key("ilsStations").StartArray();

            int ilsCount = 0;
            for (int j = 1; j < stationCursor; j++)
            {
                var station = stationPool[j];
                if (station == null || station.id == 0 || !station.isStellar) continue;

                ilsCount++;

                json.StartObject()
                    .Prop("id", station.id)
                    .Prop("gid", station.gid)
                    .Prop("entityId", station.entityId)
                    .Prop("isStellar", station.isStellar)
                    .Prop("deliveryDrones", station.deliveryDrones)
                    .Prop("deliveryShips", station.deliveryShips)
                    .Prop("idleDroneCount", station.idleDroneCount)
                    .Prop("workDroneCount", station.workDroneCount)
                    .Prop("idleShipCount", station.idleShipCount)
                    .Prop("workShipCount", station.workShipCount)
                    .Prop("warperCount", station.warperCount)
                    .Prop("warperMaxCount", station.warperMaxCount)
                    .Prop("energy", station.energy)
                    .Prop("energyMax", station.energyMax)
                    .Prop("energyPerTick", station.energyPerTick)
                    .Prop("remoteGroupMask", station.remoteGroupMask)
                    .Prop("routePriority", station.routePriority.ToString());

                // Item Storage Details
                json.Key("storage").StartArray();
                if (station.storage != null)
                {
                    for (int k = 0; k < station.storage.Length; k++)
                    {
                        var store = station.storage[k];
                        if (store.itemId > 0)
                        {
                            string itemName = "Unknown";
                            try
                            {
                                var itemProto = LDB.items.Select(store.itemId);
                                if (itemProto != null && itemProto.name != null)
                                {
                                    itemName = itemProto.name.Translate() ?? "Unknown";
                                }
                            }
                            catch
                            {
                                itemName = $"Item {store.itemId}";
                            }

                            json.StartObject()
                                .Prop("itemId", store.itemId)
                                .Prop("itemName", itemName)
                                .Prop("count", store.count)
                                .Prop("inc", store.inc)
                                .Prop("max", store.max)
                                .Prop("localLogic", store.localLogic.ToString())
                                .Prop("remoteLogic", store.remoteLogic.ToString())
                                .EndObject();
                        }
                    }
                }

                json.EndArray(); // End storage array
                json.EndObject(); // End station object
            }

            json.EndArray(); // End ilsStations array
            json.Prop("ilsCount", ilsCount);
            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private string GetShippingRoutes(string id, string paramsJson)
        {
            var stationIdMatch = Regex.Match(paramsJson, @"""stationId""\s*:\s*(\d+)");
            if (!stationIdMatch.Success || !int.TryParse(stationIdMatch.Groups[1].Value, out var stationGid))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'stationId' (gid) parameter.");
            }

            var gameData = GameMain.data;
            var galacticTransport = gameData.galacticTransport;
            if (galacticTransport == null || galacticTransport.stationPool == null)
            {
                return JsonRpc.Error(id, -32602, "Galactic Transport not initialized.");
            }

            var stationPool = galacticTransport.stationPool;
            var stationCursor = galacticTransport.stationCursor;

            // Find the target station
            StationComponent targetStation = null;
            if (stationGid >= 0 && stationGid < stationPool.Length)
            {
                targetStation = stationPool[stationGid];
            }

            if (targetStation == null || targetStation.id == 0 || targetStation.gid != stationGid)
            {
                 // Fallback search if index mismatch (though gid is usually index)
                 bool found = false;
                 for(int i=0; i<stationCursor; i++) {
                    if (stationPool[i] != null && stationPool[i].gid == stationGid) {
                        targetStation = stationPool[i];
                        found = true;
                        break;
                    }
                 }
                 if (!found)
                    return JsonRpc.Error(id, -32602, $"Station with GID {stationGid} not found.");
            }

            var json = new JsonBuilder().StartObject();
            var targetPlanet = gameData.galaxy.PlanetById(targetStation.planetId);
            
            json.Prop("stationId", targetStation.gid)
                .Prop("planetId", targetStation.planetId)
                .Prop("planetName", targetPlanet?.displayName ?? "Unknown")
                .Prop("starName", targetPlanet?.star?.displayName ?? "Unknown");

            // Outgoing Ships (Owned by this station)
            json.Key("outgoing").StartArray();
            if (targetStation.workShipDatas != null)
            {
                for (int i = 0; i < targetStation.workShipCount; i++)
                {
                    var ship = targetStation.workShipDatas[i];
                    if (ship.otherGId <= 0) continue; 

                    var otherStation = (ship.otherGId < stationPool.Length) ? stationPool[ship.otherGId] : null;
                    string otherPlanetName = "Unknown";
                    string otherStarName = "Unknown";
                    
                    if (otherStation != null) {
                        var p = gameData.galaxy.PlanetById(otherStation.planetId);
                        if (p != null) {
                            otherPlanetName = p.displayName;
                            otherStarName = p.star?.displayName;
                        }
                    }

                    string itemName = GetItemName(ship.itemId);
                    string stage = GetShipStageName(ship.stage);

                    var astroPoses = GameMain.data.galaxy.astrosData;
                    var history = GameMain.history;
                    int remainingTicks = targetStation.CalcArrivalRemainingTime(astroPoses, otherStation, history.logisticShipSailSpeedModified, history.logisticShipWarpSpeedModified, ship.shipIndex);

                    // Calculate distance to target station
                    double distance = 0;
                    if (otherStation != null)
                    {
                        var targetPos = astroPoses[otherStation.planetId].uPos;
                        distance = (ship.uPos - targetPos).magnitude;
                    }

                    json.StartObject()
                        .Prop("shipIndex", ship.shipIndex)
                        .Prop("otherStationGId", ship.otherGId)
                        .Prop("otherPlanetName", otherPlanetName)
                        .Prop("otherStarName", otherStarName)
                        .Prop("itemId", ship.itemId)
                        .Prop("itemName", itemName)
                        .Prop("itemCount", ship.itemCount)
                        .Prop("stage", stage)
                        .Prop("direction", ship.direction)
                        .Prop("t", ship.t) // progress 0-1
                        .Prop("uSpeed", ship.uSpeed)
                        .Prop("warpState", ship.warpState)
                        .Prop("distance", distance)
                        .Prop("remainingTicks", remainingTicks)
                        .Prop("remainingSeconds", remainingTicks / 60.0)
                        .EndObject();
                }
            }
            json.EndArray();

            // Incoming Ships (Owned by OTHER stations, targeting this one)
            json.Key("incoming").StartArray();
            for (int i = 0; i < stationCursor; i++)
            {
                var otherStation = stationPool[i];
                if (otherStation == null || otherStation.gid == targetStation.gid || otherStation.id == 0) continue;

                if (otherStation.workShipDatas != null)
                {
                    for (int s = 0; s < otherStation.workShipCount; s++)
                    {
                        var ship = otherStation.workShipDatas[s];
                        if (ship.otherGId == targetStation.gid)
                        {
                            // This ship is coming to (or returning from) our target station
                            string sourcePlanetName = "Unknown";
                            string sourceStarName = "Unknown";
                            
                            var p = gameData.galaxy.PlanetById(otherStation.planetId);
                            if (p != null) {
                                sourcePlanetName = p.displayName;
                                sourceStarName = p.star?.displayName;
                            }

                            string itemName = GetItemName(ship.itemId);
                            string stage = GetShipStageName(ship.stage);

                            var astroPoses = GameMain.data.galaxy.astrosData;
                            var history = GameMain.history;
                            // Note: CalcArrivalRemainingTime must be called on the station that OWNS the ship
                            int remainingTicks = otherStation.CalcArrivalRemainingTime(astroPoses, targetStation, history.logisticShipSailSpeedModified, history.logisticShipWarpSpeedModified, ship.shipIndex);

                            // Calculate distance to this station
                            var targetPos = astroPoses[targetStation.planetId].uPos;
                            double distance = (ship.uPos - targetPos).magnitude;

                            json.StartObject()
                                .Prop("shipIndex", ship.shipIndex)
                                .Prop("sourceStationGId", otherStation.gid)
                                .Prop("sourcePlanetName", sourcePlanetName)
                                .Prop("sourceStarName", sourceStarName)
                                .Prop("itemId", ship.itemId)
                                .Prop("itemName", itemName)
                                .Prop("itemCount", ship.itemCount)
                                .Prop("stage", stage)
                                .Prop("direction", ship.direction) 
                                .Prop("t", ship.t)
                                .Prop("uSpeed", ship.uSpeed)
                                .Prop("warpState", ship.warpState)
                                .Prop("distance", distance)
                                .Prop("remainingTicks", remainingTicks)
                                .Prop("remainingSeconds", remainingTicks / 60.0)
                                .EndObject();
                        }
                    }
                }
            }
            json.EndArray();

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private string GetPlanetRoutes(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
            if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'planetId' parameter.");
            }

            var gameData = GameMain.data;
            var galacticTransport = gameData.galacticTransport;
            if (galacticTransport == null || galacticTransport.stationPool == null)
                return JsonRpc.Error(id, -32602, "Galactic Transport not initialized.");

            // Identify all station GIDs belonging to the target planet
            var targetStationGids = new HashSet<int>();
            var stationPool = galacticTransport.stationPool;
            var stationCursor = galacticTransport.stationCursor;

            for (int i = 0; i < stationCursor; i++)
            {
                var s = stationPool[i];
                if (s != null && s.id > 0 && s.planetId == planetId)
                {
                    targetStationGids.Add(s.gid);
                }
            }

            var json = new JsonBuilder().StartObject();
            var targetPlanet = gameData.galaxy.PlanetById(planetId);
            
            json.Prop("planetId", planetId)
                .Prop("planetName", targetPlanet?.displayName ?? "Unknown")
                .Prop("starName", targetPlanet?.star?.displayName ?? "Unknown")
                .Prop("stationCount", targetStationGids.Count);

            var outgoing = new JsonBuilder().StartArray();
            var incoming = new JsonBuilder().StartArray();

            var astroPoses = GameMain.data.galaxy.astrosData;
            var history = GameMain.history;

            // Single pass over all global stations to find relevant ships
            for (int i = 0; i < stationCursor; i++)
            {
                var station = stationPool[i];
                if (station == null || station.id == 0 || station.workShipDatas == null) continue;

                bool stationIsOnTargetPlanet = targetStationGids.Contains(station.gid);

                for (int s = 0; s < station.workShipCount; s++)
                {
                    var ship = station.workShipDatas[s];
                    if (ship.otherGId <= 0) continue;

                    // Outgoing: Origin is on target planet
                    if (stationIsOnTargetPlanet)
                    {
                        var destStation = (ship.otherGId < stationPool.Length) ? stationPool[ship.otherGId] : null;
                        string destPlanetName = "Unknown";
                        double distance = 0;
                        int remainingTicks = 0;

                        if (destStation != null) 
                        {
                            var p = gameData.galaxy.PlanetById(destStation.planetId);
                            destPlanetName = p?.displayName ?? "Unknown";
                            
                            var targetPos = astroPoses[destStation.planetId].uPos;
                            distance = (ship.uPos - targetPos).magnitude;
                            remainingTicks = station.CalcArrivalRemainingTime(astroPoses, destStation, history.logisticShipSailSpeedModified, history.logisticShipWarpSpeedModified, ship.shipIndex);
                        }
                        
                        outgoing.StartObject()
                            .Prop("shipIndex", ship.shipIndex)
                            .Prop("originStationGId", station.gid)
                            .Prop("destStationGId", ship.otherGId)
                            .Prop("destPlanetName", destPlanetName)
                            .Prop("itemId", ship.itemId)
                            .Prop("itemName", GetItemName(ship.itemId))
                            .Prop("itemCount", ship.itemCount)
                            .Prop("stage", GetShipStageName(ship.stage))
                            .Prop("t", ship.t)
                            .Prop("uSpeed", ship.uSpeed)
                            .Prop("distance", distance)
                            .Prop("remainingTicks", remainingTicks)
                            .Prop("remainingSeconds", remainingTicks / 60.0)
                            .EndObject();
                    }
                    // Incoming: Destination is on target planet
                    else if (targetStationGids.Contains(ship.otherGId))
                    {
                        var originStation = station;
                        string originPlanetName = "Unknown";
                        var p = gameData.galaxy.PlanetById(originStation.planetId);
                        originPlanetName = p?.displayName ?? "Unknown";

                        var destStation = stationPool[ship.otherGId];
                        double distance = 0;
                        int remainingTicks = 0;
                        if (destStation != null)
                        {
                            var targetPos = astroPoses[destStation.planetId].uPos;
                            distance = (ship.uPos - targetPos).magnitude;
                            remainingTicks = originStation.CalcArrivalRemainingTime(astroPoses, destStation, history.logisticShipSailSpeedModified, history.logisticShipWarpSpeedModified, ship.shipIndex);
                        }

                        incoming.StartObject()
                            .Prop("shipIndex", ship.shipIndex)
                            .Prop("originStationGId", originStation.gid)
                            .Prop("originPlanetName", originPlanetName)
                            .Prop("destStationGId", ship.otherGId)
                            .Prop("itemId", ship.itemId)
                            .Prop("itemName", GetItemName(ship.itemId))
                            .Prop("itemCount", ship.itemCount)
                            .Prop("stage", GetShipStageName(ship.stage))
                            .Prop("t", ship.t)
                            .Prop("uSpeed", ship.uSpeed)
                            .Prop("distance", distance)
                            .Prop("remainingTicks", remainingTicks)
                            .Prop("remainingSeconds", remainingTicks / 60.0)
                            .EndObject();
                    }
                }
            }

            outgoing.EndArray();
            incoming.EndArray();

            json.Key("outgoing").AppendRaw(outgoing.ToString());
            json.Key("incoming").AppendRaw(incoming.ToString());

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private string FindItemTransport(string id, string paramsJson)
        {
            var itemIdMatch = Regex.Match(paramsJson, @"""itemId""\s*:\s*(\d+)");
            if (!itemIdMatch.Success || !int.TryParse(itemIdMatch.Groups[1].Value, out var itemId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'itemId' parameter.");
            }

            var gameData = GameMain.data;
            var galacticTransport = gameData.galacticTransport;
            if (galacticTransport == null || galacticTransport.stationPool == null)
                return JsonRpc.Error(id, -32602, "Galactic Transport not initialized.");

            var json = new JsonBuilder().StartObject();
            json.Prop("itemId", itemId)
                .Prop("itemName", GetItemName(itemId));

            json.Key("ships").StartArray();

            var stationPool = galacticTransport.stationPool;
            var stationCursor = galacticTransport.stationCursor;
            var astroPoses = GameMain.data.galaxy.astrosData;
            var history = GameMain.history;

            for (int i = 0; i < stationCursor; i++)
            {
                var station = stationPool[i];
                if (station == null || station.id == 0 || station.workShipDatas == null) continue;

                for (int s = 0; s < station.workShipCount; s++)
                {
                    var ship = station.workShipDatas[s];
                    if (ship.itemId == itemId)
                    {
                        var destStation = (ship.otherGId > 0 && ship.otherGId < stationPool.Length) ? stationPool[ship.otherGId] : null;
                        
                        string originPlanet = gameData.galaxy.PlanetById(station.planetId)?.displayName ?? "Unknown";
                        string destPlanet = destStation != null ? (gameData.galaxy.PlanetById(destStation.planetId)?.displayName ?? "Unknown") : "Unknown";

                        double distance = 0;
                        int remainingTicks = 0;
                        if (destStation != null)
                        {
                            var targetPos = astroPoses[destStation.planetId].uPos;
                            distance = (ship.uPos - targetPos).magnitude;
                            remainingTicks = station.CalcArrivalRemainingTime(astroPoses, destStation, history.logisticShipSailSpeedModified, history.logisticShipWarpSpeedModified, ship.shipIndex);
                        }

                        json.StartObject()
                            .Prop("shipIndex", ship.shipIndex)
                            .Prop("originStationGId", station.gid)
                            .Prop("originPlanet", originPlanet)
                            .Prop("destStationGId", ship.otherGId)
                            .Prop("destPlanet", destPlanet)
                            .Prop("itemCount", ship.itemCount)
                            .Prop("stage", GetShipStageName(ship.stage))
                            .Prop("t", ship.t)
                            .Prop("uSpeed", ship.uSpeed)
                            .Prop("distance", distance)
                            .Prop("remainingTicks", remainingTicks)
                            .Prop("remainingSeconds", remainingTicks / 60.0)
                            .EndObject();
                    }
                }
            }

            json.EndArray().EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private string GetItemName(int itemId) {
             if (itemId <= 0) return "None";
             try {
                var itemProto = LDB.items.Select(itemId);
                return itemProto?.name?.Translate() ?? $"Item {itemId}";
             } catch { return $"Item {itemId}"; }
        }

        private string GetShipStageName(int stage) {
            switch (stage)
            {
                case -2: return "Takeoff";
                case -1: return "Departure";
                case 0: return "Flight";
                case 1: return "Approach";
                case 2: return "Landing";
                default: return stage.ToString();
            }
        }
    }
}