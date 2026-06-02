using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WormholeSignalBridge
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    internal sealed class WormholeBridgeBootstrap : MonoBehaviour
    {
        private static bool initialized;

        private void Start()
        {
            if (initialized)
                return;

            if (!DependencyChecker.Verify())
                return;

            WormholeSettings.Load();
            WormholeRegistry.Refresh();

            if (!RealAntennasReflection.IsReady)
            {
                Log.Error("Failed to bind RealAntennas internals; wormhole comm links are disabled.");
                return;
            }

            var harmony = new Harmony("com.aebestach.WormholeSignalBridge");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            initialized = true;
            Log.Info("Initialized.");
        }
    }

    internal static class DependencyChecker
    {
        internal static bool Verify()
        {
            bool hasRealAntennas = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "RealAntennas");
            bool hasKex = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "KEX-Wormholes");

            if (!hasRealAntennas)
            {
                Log.Error("RealAntennas is required but was not found.");
                return false;
            }

            if (!hasKex)
            {
                Log.Error("KEX-Wormholes (Kopernicus Expansion Continued) is required but was not found.");
                return false;
            }

            return true;
        }
    }
}
