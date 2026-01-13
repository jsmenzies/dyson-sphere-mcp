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
                { "get_ils_details", "Get detailed ILS information for a specific planet including items, storage, and ship counts." }
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
    }
}