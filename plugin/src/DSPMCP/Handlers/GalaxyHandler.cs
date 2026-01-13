using System;
using System.Collections.Generic;

namespace DSPMCP.Handlers
{
    public class GalaxyHandler : IMethodHandler
    {
        public Dictionary<string, string> GetMethods()
        {
            return new Dictionary<string, string>
            {
                { "get_galaxy_details", "Get detailed information about the current galaxy, including seed, star count, and birth planet/star IDs." },
                { "get_stars", "List all stars with details (ID, name, type, position, luminosity, planet count)." }
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
                case "get_galaxy_details":
                    return GetGalaxyDetails(requestId);
                case "get_stars":
                    return GetStars(requestId);
                default:
                    return JsonRpc.Error(requestId, -32601, $"Method not found: {method}");
            }
        }

        private string GetGalaxyDetails(string requestId)
        {
            try
            {
                var galaxy = GameMain.data.galaxy;
                var jsonBuilder = new JsonBuilder().StartObject()
                    .Prop("seed", galaxy.seed)
                    .Prop("starCount", galaxy.starCount)
                    .Prop("birthPlanetId", galaxy.birthPlanetId)
                    .Prop("birthStarId", galaxy.birthStarId)
                    .Prop("habitableCount", galaxy.habitableCount)
                    .EndObject();

                return JsonRpc.Success(requestId, jsonBuilder.ToString());
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GalaxyHandler.GetGalaxyDetails error: {ex.Message}");
                return JsonRpc.Error(requestId, -32603, ex.Message);
            }
        }

        private string GetStars(string requestId)
        {
            try
            {
                var galaxy = GameMain.data.galaxy;
                var jsonBuilder = new JsonBuilder().StartArray();

                foreach (var star in galaxy.stars)
                {
                    jsonBuilder.StartObject()
                        .Prop("id", star.id)
                        .Prop("name", star.name)
                        .Prop("displayName", star.displayName)
                        .Key("position").StartObject()
                            .Prop("x", star.position.x)
                            .Prop("y", star.position.y)
                            .Prop("z", star.position.z)
                            .EndObject()
                        .Prop("type", star.type.ToString()) // EStarType
                        .Prop("spectr", star.spectr.ToString()) // ESpectrType
                        .Prop("temperature", star.temperature)
                        .Prop("luminosity", star.luminosity)
                        .Prop("radius", star.radius)
                        .Prop("dysonRadius", star.dysonRadius)
                        .Prop("planetCount", star.planetCount)
                        .EndObject();
                }
                jsonBuilder.EndArray();
                return JsonRpc.Success(requestId, jsonBuilder.ToString());
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GalaxyHandler.GetStars error: {ex.Message}");
                return JsonRpc.Error(requestId, -32603, ex.Message);
            }
        }
    }
}
