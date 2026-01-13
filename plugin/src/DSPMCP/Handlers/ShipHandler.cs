using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace DSPMCP.Handlers
{
    public class ShipHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "get_ships", "Get details of all ships in the game." }
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
                    case "get_ships":
                        return GetShips(id);
                    default:
                        return JsonRpc.Error(id, -32601, $"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ShipHandler.{method} error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        private string GetShips(string id)
        {
            var json = new JsonBuilder().StartObject();
            var gameData = GameMain.data;

            json.Key("ships").StartArray();

            if (gameData?.factories != null)
            {
                for (int i = 0; i < gameData.factoryCount; i++)
                {
                    var factory = gameData.factories[i];
                    if (factory == null) continue;

                    var planet = factory.planet;
                    var transport = factory.transport;
                    if (transport?.stationPool == null) continue;

                    var stationPool = transport.stationPool;
                    var stationCursor = transport.stationCursor;

                    for (int j = 1; j < stationCursor; j++)
                    {
                        var station = stationPool[j];
                        if (station == null || station.id == 0 || !station.isStellar) continue;

                        // Output ship counts for this ILS station
                        json.StartObject()
                            .Prop("planetId", planet.id)
                            .Prop("planetName", planet.displayName ?? "Unknown")
                            .Prop("starName", planet.star?.displayName ?? "Unknown")
                            .Prop("stationId", station.id)
                            .Prop("stationEntityId", station.entityId)
                            .Prop("deliveryShips", station.deliveryShips)
                            .Prop("idleShipCount", station.idleShipCount)
                            .Prop("workShipCount", station.workShipCount)
                            .EndObject();
                    }
                }
            }

            json.EndArray();
            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }
    }
}