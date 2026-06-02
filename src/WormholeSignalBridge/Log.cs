using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class Log
    {
        private const string Tag = "[WormholeSignalBridge]";

        internal static void Info(string message) => Debug.Log($"{Tag} {message}");
        internal static void Warning(string message) => Debug.LogWarning($"{Tag} {message}");
        internal static void Error(string message) => Debug.LogError($"{Tag} {message}");
    }
}
