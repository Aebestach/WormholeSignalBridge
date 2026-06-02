using System.IO;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class WormholeLinkSettings
    {
        internal bool Enabled = true;
        internal double EffectiveDistance = 1000;
        internal double InsertionLoss = 0;
    }

    internal static class WormholeSettings
    {
        private const string SettingsPath = "GameData/WormholeSignalBridge/PluginData/Settings.cfg";

        internal static WormholeLinkSettings Defaults { get; private set; } = new WormholeLinkSettings();
        internal static bool DebugLogging { get; private set; }

        internal static void Load()
        {
            Defaults = new WormholeLinkSettings();
            DebugLogging = false;

            string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, SettingsPath);
            if (!File.Exists(fullPath))
            {
                Log.Warning($"Settings file not found at {fullPath}; using built-in defaults.");
                LogLoadedSettings();
                return;
            }

            ConfigNode root = ConfigNode.Load(fullPath);
            if (root == null)
            {
                Log.Warning($"Failed to parse {fullPath}; using built-in defaults.");
                LogLoadedSettings();
                return;
            }

            ConfigNode settingsNode = root.GetNode("WormholeBridgeSettings") ?? root;
            ApplyNode(settingsNode, Defaults);
            if (settingsNode.HasValue("debugLogging"))
                DebugLogging = bool.Parse(settingsNode.GetValue("debugLogging"));

            LogLoadedSettings();
        }

        private static void ApplyNode(ConfigNode node, WormholeLinkSettings settings)
        {
            if (node.HasValue("enabled"))
                settings.Enabled = bool.Parse(node.GetValue("enabled"));
            if (node.HasValue("effectiveDistance"))
                settings.EffectiveDistance = double.Parse(node.GetValue("effectiveDistance"));
            if (node.HasValue("insertionLoss"))
                settings.InsertionLoss = double.Parse(node.GetValue("insertionLoss"));
        }

        private static void LogLoadedSettings()
        {
            Log.Info(
                $"Loaded settings: enabled={Defaults.Enabled}, " +
                $"effectiveDistance={Defaults.EffectiveDistance} m, insertionLoss={Defaults.InsertionLoss} dB, " +
                $"debugLogging={DebugLogging}");
        }
    }
}
