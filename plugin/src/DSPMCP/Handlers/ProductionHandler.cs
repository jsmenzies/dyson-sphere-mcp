using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DSPMCP.Handlers;

namespace DSPMCP
{
    /// <summary>
    /// Handles production and consumption statistics queries.
    /// Exposes: production rates, consumption rates, and detailed assembler status.
    /// </summary>
    public class ProductionHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "get_production_stats", "Get production and consumption rates for a planet or the entire galaxy." },
                { "get_assembler_details", "Get detailed status of all assemblers on a specific planet, including proliferation and actual speed." }
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
                    case "get_production_stats":
                        return GetProductionStats(id, paramsJson);
                    case "get_assembler_details":
                        return GetAssemblerDetails(id, paramsJson);
                    default:
                        return JsonRpc.Error(id, -32601, $"Unknown method: {method}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"ProductionHandler.{method} error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        /// <summary>
        /// Get production stats. 
        /// Supports planetId (-1 for global) and timeLevel (0=1m, 1=10m, 2=1h, 3=10h, 4=100h, 5=Total).
        /// </summary>
        private string GetProductionStats(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(-?\d+)");
            int planetId = planetIdMatch.Success ? int.Parse(planetIdMatch.Groups[1].Value) : -1;

            var timeLevelMatch = Regex.Match(paramsJson, @"""timeLevel""\s*:\s*(\d+)");
            int timeLevel = timeLevelMatch.Success ? int.Parse(timeLevelMatch.Groups[1].Value) : 0;
            if (timeLevel < 0 || timeLevel > 5) timeLevel = 0;

            var gameData = GameMain.data;
            var prod = gameData.statistics.production;

            // Map timeLevel to multiplier to get "per minute"
            // Level 0 = 1m
            // Level 1 = 10m
            // Level 2 = 60m (1h)
            // ...
            double[] levelDivisors = { 1.0, 10.0, 60.0, 600.0, 6000.0, 1.0 }; 
            double divisor = levelDivisors[timeLevel];

            // Aggregator for items
            var itemStats = new Dictionary<int, ItemStatSum>();

            void AggregateFactory(int factoryIndex)
            {
                var factoryStat = prod.factoryStatPool[factoryIndex];
                if (factoryStat == null) return;

                for (int i = 1; i < factoryStat.productCursor; i++)
                {
                    var pStat = factoryStat.productPool[i];
                    if (pStat.itemId <= 0) continue;

                    if (!itemStats.TryGetValue(pStat.itemId, out var sum))
                    {
                        sum = new ItemStatSum { ItemId = pStat.itemId };
                        itemStats[pStat.itemId] = sum;
                    }

                    // Indices 1-6 are production levels, 8-13 are consumption levels (based on UIStatisticsWindow.DetermineProductEntryList)
                    sum.ProductionCount += pStat.total[timeLevel + 1];
                    sum.ConsumptionCount += pStat.total[timeLevel + 8];
                    sum.TheoreticalMaxProduction += pStat.refProductSpeed * 60.0; // convert items/sec to items/min
                    sum.TheoreticalMaxConsumption += pStat.refConsumeSpeed * 60.0;
                }
            }

            if (planetId == -1)
            {
                for (int i = 0; i < gameData.factoryCount; i++) AggregateFactory(i);
            }
            else
            {
                int factoryIndex = -1;
                for (int i = 0; i < gameData.factoryCount; i++)
                {
                    if (gameData.factories[i]?.planet?.id == planetId)
                    {
                        factoryIndex = i;
                        break;
                    }
                }
                if (factoryIndex != -1) AggregateFactory(factoryIndex);
                else return JsonRpc.Error(id, -32602, $"Planet {planetId} not found.");
            }

            var json = new JsonBuilder().StartObject();
            json.Prop("planetId", planetId);
            json.Prop("timeLevel", timeLevel);
            json.Key("stats").StartArray();

            foreach (var sum in itemStats.Values)
            {
                var proto = LDB.items.Select(sum.ItemId);
                json.StartObject()
                    .Prop("itemId", sum.ItemId)
                    .Prop("itemName", proto?.name?.Translate() ?? "Unknown")
                    .Prop("productionRate", Math.Round(sum.ProductionCount / divisor, 2))
                    .Prop("consumptionRate", Math.Round(sum.ConsumptionCount / divisor, 2))
                    .Prop("theoreticalMaxProduction", Math.Round(sum.TheoreticalMaxProduction, 2))
                    .Prop("theoreticalMaxConsumption", Math.Round(sum.TheoreticalMaxConsumption, 2))
                    .EndObject();
            }

            json.EndArray().EndObject();
            return JsonRpc.Success(id, json.ToString());
        }

        private class ItemStatSum
        {
            public int ItemId;
            public long ProductionCount;
            public long ConsumptionCount;
            public double TheoreticalMaxProduction;
            public double TheoreticalMaxConsumption;
        }

        /// <summary>
        /// Get detailed status of all assemblers on a planet.
        /// </summary>
        private string GetAssemblerDetails(string id, string paramsJson)
        {
            var planetIdMatch = Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
            if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
            {
                return JsonRpc.Error(id, -32602, "Missing or invalid 'planetId' parameter.");
            }

            var gameData = GameMain.data;
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
                return JsonRpc.Error(id, -32602, $"Planet with ID {planetId} not found.");

            var json = new JsonBuilder().StartObject();
            json.Prop("planetId", planetId);
            json.Prop("planetName", factory.planet.displayName ?? "Unknown");
            json.Key("assemblers").StartArray();

            var assemblerPool = factory.factorySystem?.assemblerPool;
            var assemblerCursor = factory.factorySystem?.assemblerCursor ?? 0;
            var powerSystem = factory.powerSystem;

            if (assemblerPool != null)
            {
                for (int i = 1; i < assemblerCursor; i++)
                {
                    var assembler = assemblerPool[i];
                    if (assembler.id != i || assembler.recipeId == 0) continue;

                    var recipe = LDB.recipes.Select(assembler.recipeId);
                    
                    float powerRatio = 0f;
                    if (powerSystem != null && powerSystem.consumerPool != null && assembler.pcId > 0 && assembler.pcId < powerSystem.consumerCursor)
                    {
                        var networkId = powerSystem.consumerPool[assembler.pcId].networkId;
                        if (networkId > 0 && networkId < powerSystem.netCursor)
                            powerRatio = powerSystem.networkServes[networkId];
                    }

                    json.StartObject()
                        .Prop("id", assembler.id)
                        .Prop("entityId", assembler.entityId)
                        .Prop("recipeId", assembler.recipeId)
                        .Prop("recipeName", recipe?.name?.Translate() ?? "Unknown")
                        .Prop("replicating", assembler.replicating)
                        .Prop("speed", assembler.speed) // Base speed multiplier (10000 = 100%)
                        .Prop("extraSpeed", assembler.extraSpeed) // Speed bonus from proliferation
                        .Prop("productive", assembler.productive) // Can use extra products?
                        .Prop("isExtraProductiveMode", assembler.forceAccMode) // User selected Extra Products mode
                        .Prop("powerRatio", Math.Round(powerRatio, 4))
                        .Prop("itemsPerMinute", recipe != null && recipe.TimeSpend > 0 ? (60.0 * 60.0 / recipe.TimeSpend * (assembler.speed / 10000.0) * (1.0 + assembler.extraSpeed / 100000.0)) : 0)
                        .EndObject();
                }
            }

            json.EndArray().EndObject();
            return JsonRpc.Success(id, json.ToString());
        }
    }
}
