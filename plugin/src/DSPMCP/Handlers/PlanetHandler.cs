using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DSPMCP.Handlers
{
    public class PlanetHandler : IMethodHandler
    {
                public Dictionary<string, string> GetMethods()
                {
                    return new Dictionary<string, string>
                    {
                        { "list_planets", "List all planets with details (ID, name, star, type, position, radius, resources)." },
                        { "get_planet_resources", "Get vein/resource deposits on a specific planet." }
                    };
                }
        
                public string Handle(string method, string requestId, string paramsJson)
                {
                    if (GameMain.data == null || GameMain.data.galaxy == null)
                    {
                        return JsonRpc.GameNotLoaded(requestId);
                    }
        
                    switch (method)
                    {
                        case "list_planets":
                            return ListPlanets(requestId);
                        case "get_planet_resources":
                            return GetPlanetResources(requestId, paramsJson);
                        default:
                            return JsonRpc.Error(requestId, -32601, $"Method not found: {method}");
                    }
                }
        
                private string ListPlanets(string requestId)
                {
                    try
                    {
                        var jsonBuilder = new JsonBuilder();
                        jsonBuilder.StartArray();
        
                        foreach (var star in GameMain.data.galaxy.stars)
                        {
                            foreach (var planet in star.planets)
                            {
                                jsonBuilder.StartObject()
                                    .Prop("id", planet.id)
                                    .Prop("name", planet.displayName)
                                    .Prop("starId", star.id)
                                    .Prop("starName", star.displayName)
                                    .Prop("type", planet.type.ToString()) // EPlanetType
                                    .Prop("singularity", planet.singularity.ToString()) // EPlanetSingularity
                                    .Prop("theme", planet.theme)
                                    .Prop("radius", planet.radius)
                                    .Prop("orbitRadius", planet.orbitRadius)
                                    .Prop("rotationPeriod", planet.rotationPeriod)
                                    .Prop("obliquity", planet.obliquity)
                                    .Prop("orbitalPeriod", planet.orbitalPeriod) // Corrected from revolutionPeriod
                                    .Key("position").StartObject()
                                        .Prop("x", planet.runtimePosition.x)
                                        .Prop("y", planet.runtimePosition.y)
                                        .Prop("z", planet.runtimePosition.z)
                                        .EndObject()
                                    .EndObject(); // End planet object - resourceSpots removed
                            }
                        }
                        jsonBuilder.EndArray();
                        return JsonRpc.Success(requestId, jsonBuilder.ToString());
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"PlanetHandler.ListPlanets error: {ex.Message}");
                        return JsonRpc.Error(requestId, -32603, ex.Message);
                    }
                }
        
                private string GetPlanetResources(string requestId, string paramsJson)
                {
                    var planetIdMatch = System.Text.RegularExpressions.Regex.Match(paramsJson, @"""planetId""\s*:\s*(\d+)");
                    if (!planetIdMatch.Success || !int.TryParse(planetIdMatch.Groups[1].Value, out var planetId))
                    {
                        return JsonRpc.Error(requestId, -32602, "Missing or invalid 'planetId' parameter.");
                    }
        
                                var planet = GameMain.data.galaxy.PlanetById(planetId);
                                if (planet == null)
                                {
                                    return JsonRpc.Error(requestId, -32602, $"Planet with ID {planetId} not found.");
                                }
        
                                long[] veinAmounts = null;
                                int[] veinCounts = null;
                                HashSet<int> hashes = new HashSet<int>();
        
                                var factory = planet.factory;
                    try
                    {
                        var jsonBuilder = new JsonBuilder().StartObject()
                            .Prop("planetId", planet.id)
                            .Prop("planetName", planet.displayName)
                            .Key("resourceSpots").StartArray();
        
                        if (planet.factory != null && planet.runtimeVeinPool != null)
                        {
                                planet.SummarizeVeinAmountsByFilter(ref veinAmounts, hashes, 0); // 0 for all veins
                                planet.SummarizeVeinCountsByFilter(ref veinCounts, hashes, 0);   // 0 for all veins
        
                                // Create a mapping from EVeinType enum value to string name
                                var veinTypeNames = Enum.GetValues(typeof(EVeinType))
                                                        .Cast<EVeinType>()
                                                        .ToDictionary(type => (int)type, type => type.ToString());
        
                                for (int i = 1; i < veinAmounts.Length; i++) // Start from 1 to skip EVeinType.None
                                {
                                    if (veinAmounts[i] > 0 || veinCounts[i] > 0)
                                    {
                                        jsonBuilder.StartObject()
                                            .Prop("type", veinTypeNames[i])
                                            .Prop("amount", veinAmounts[i])
                                            .Prop("veinCount", veinCounts[i])
                                            .EndObject();
                                    }
                                }
                        }
                        jsonBuilder.EndArray();
                        jsonBuilder.EndObject(); // Add this line to close the main object
                        string jsonOutput = jsonBuilder.ToString();
                        return JsonRpc.Success(requestId, jsonOutput);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"PlanetHandler.GetPlanetResources error: {ex.Message}");
                        return JsonRpc.Error(requestId, -32603, ex.Message);
                    }
                }
            }
        }
        