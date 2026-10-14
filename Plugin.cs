using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NoCheatItems
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class NoCheatItemsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ageous27.nocheatitems";
        public const string PluginName = "NoCheatItems";
        public const string PluginVersion = "1.0.4";

        internal static NoCheatItemsPlugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;
        private float _scanTimer;
        private float _debugTimer;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ModConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Server-side cheated-item cleanup is active. DebugLogging={ModConfig.DebugLogging.Value}.");
            CheatCleaner.LogDebugHeartbeat("startup");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            float interval = Mathf.Max(1f, ModConfig.ScanIntervalSeconds.Value);
            // Report liveness even if cleanup is disabled or this instance is a client.
            if (ModConfig.DebugLogging.Value)
            {
                _debugTimer += Time.unscaledDeltaTime;
                if (_debugTimer >= interval)
                {
                    _debugTimer = 0f;
                    CheatCleaner.LogDebugHeartbeat("update");
                }
            }
            else
            {
                _debugTimer = 0f;
            }

            if (!ModConfig.Enabled.Value || !CheatCleaner.IsServer())
            {
                return;
            }

            _scanTimer += Time.deltaTime;
            if (_scanTimer < interval)
            {
                return;
            }

            _scanTimer = 0f;
            CheatCleaner.SweepLoadedWorld("periodic");
        }
    }
}
