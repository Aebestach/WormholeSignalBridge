using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class Log
    {
        private const string Tag = "[WormholeSignalBridge]";

        internal static void Info(string message) => UnityEngine.Debug.Log($"{Tag} {message}");

        /// <summary>Emitted only when Wormhole Bridge debug logging is enabled in difficulty settings.</summary>
        internal static void DebugLog(string message)
        {
            if (WormholeSettings.DebugLogging)
                UnityEngine.Debug.Log($"{Tag} [debug] {message}");
        }

        internal static void Warning(string message) => UnityEngine.Debug.LogWarning($"{Tag} {message}");
        internal static void Error(string message) => UnityEngine.Debug.LogError($"{Tag} {message}");
    }
}
