using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace DSPMCP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private WsServer _server;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            _server = new WsServer();
            _server.Start();
        }

        private void OnDestroy()
        {
            _server?.Stop();
        }
    }
}
