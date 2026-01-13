using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DSPMCP.Handlers;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace DSPMCP
{
    public class WsServer
    {
        private WebSocketServer _server;
        private readonly int _port;

        public WsServer(int port = 18181)
        {
            _port = port;
        }

        public void Start()
        {
            try
            {
                _server = new WebSocketServer(_port);
                _server.AddWebSocketService<GameService>("/");
                _server.Start();
                Plugin.Logger.LogInfo($"WebSocket Server started on ws://localhost:{_port}/");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to start WebSocket Server: {ex.Message}");
            }
        }

        public void Stop()
        {
            _server?.Stop();
        }
    }

    public class GameService : WebSocketBehavior
    {
                    // Registered method handlers
                    private static readonly Dictionary<string, IMethodHandler> Handlers = new Dictionary<string, IMethodHandler>();
                    private static readonly Dictionary<string, IMethodHandler> MethodToHandler = new Dictionary<string, IMethodHandler>();
        
                    static GameService()
                    {
                        // Register all handlers
                        RegisterHandler(new ResearchHandler());
                        RegisterHandler(new LogisticsHandler());
                        RegisterHandler(new PlanetHandler());
                        RegisterHandler(new GalaxyHandler());
                        RegisterHandler(new PowerGridHandler());
                        RegisterHandler(new ShipHandler());
                        RegisterHandler(new ProductionHandler());
                    }
        private static void RegisterHandler(IMethodHandler handler)
        {
            var methods = handler.GetMethods();
            foreach (var method in methods.Keys)
            {
                MethodToHandler[method] = handler;
            }
            Handlers[handler.GetType().Name] = handler;
        }

        protected override void OnOpen()
        {
            Plugin.Logger.LogInfo("Client connected.");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Plugin.Logger.LogInfo($"Client disconnected: {e.Reason}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Plugin.Logger.LogDebug($"Received: {e.Data}");

            try
            {
                var response = HandleRequest(e.Data);
                Send(response);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error handling message: {ex.Message}");
                Send(JsonRpc.Error("null", -32603, ex.Message));
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Plugin.Logger.LogError($"WebSocket error: {e.Message}");
        }

        private string HandleRequest(string json)
        {
            // Extract method from JSON-RPC request
            var methodMatch = Regex.Match(json, @"""method""\s*:\s*""([^""]+)""");
            var method = methodMatch.Success ? methodMatch.Groups[1].Value : "";

            // Extract id from JSON-RPC request
            var idMatch = Regex.Match(json, @"""id""\s*:\s*(\d+|""[^""]*"")");
            var id = idMatch.Success ? idMatch.Groups[1].Value : "1";

            // Extract params (raw JSON) for future use
            var paramsMatch = Regex.Match(json, @"""params""\s*:\s*(\{[^}]*\}|\[[^\]]*\])");
            var paramsJson = paramsMatch.Success ? paramsMatch.Groups[1].Value : "{}";

            // Built-in methods
            switch (method)
            {
                case "ping":
                    return JsonRpc.Success(id, "\"pong\"");

                case "get_game_info":
                    return GetGameInfo(id);

                case "list_methods":
                    return ListMethods(id);
            }

            // Check registered handlers
            if (MethodToHandler.TryGetValue(method, out var handler))
            {
                return handler.Handle(method, id, paramsJson);
            }

            // Unknown method
            return JsonRpc.Error(id, -32601, $"Method not found: {method}");
        }

        private string GetGameInfo(string id)
        {
            if (GameMain.data == null)
                return JsonRpc.GameNotLoaded(id);

            try
            {
                var gameData = GameMain.data;
                var player = GameMain.mainPlayer;
                var galaxy = GameMain.galaxy;

                var json = new JsonBuilder().StartObject()
                    .Prop("gameVersion", GameConfig.gameVersion.ToFullString())
                    .Prop("currentPlanet", player?.planetData?.displayName ?? "In Space")
                    .Prop("currentStar", player?.planetData?.star?.displayName ?? "Unknown")
                    .Prop("galaxyStarCount", galaxy?.starCount ?? 0)
                    .EndObject();

                return JsonRpc.Success(id, json.ToString());
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"GetGameInfo error: {ex.Message}");
                return JsonRpc.Error(id, -32603, ex.Message);
            }
        }

        private string ListMethods(string id)
        {
            var json = new JsonBuilder().StartObject();

            // Built-in methods
            json.Key("builtIn").StartObject()
                .Prop("ping", "Test connectivity")
                .Prop("get_game_info", "Get basic game info: version, current planet, and star count")
                .Prop("list_methods", "List all available methods")
                .EndObject();

            // Handler methods
            json.Key("handlers").StartObject();
            foreach (var kvp in Handlers)
            {
                json.Key(kvp.Key).StartObject();
                foreach (var method in kvp.Value.GetMethods())
                {
                    json.Prop(method.Key, method.Value);
                }
                json.EndObject();
            }
            json.EndObject();

            json.EndObject();
            return JsonRpc.Success(id, json.ToString());
        }
    }
}
