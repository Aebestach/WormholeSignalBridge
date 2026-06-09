using System.Collections;
using System.Linq;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    internal sealed class WormholeBridgeBootstrap : MonoBehaviour
    {
        private RACommNetwork network;
        private bool subscribed;

        private IEnumerator Start()
        {
            if (HighLogic.CurrentGame == null)
                yield break;

            if (!DependencyChecker.Verify())
                yield break;

            WormholeRegistry.Refresh();
            WormholeMouthScienceResults.RegisterAll();

            while (network == null)
            {
                network = RACommNetScenario.RACN;
                if (network == null)
                    yield return new WaitForSeconds(1f);
            }

            network.BeforePrecompute += OnBeforePrecompute;
            network.AfterPrecomputeLinkages += OnAfterPrecomputeLinkages;
            subscribed = true;
            WormholeMouthNodeManager.EnsureSynced(network, WormholeSettings.Current);
            Log.Info("Initialized and subscribed to RealAntennas precompute link hooks.");
        }

        private void OnDestroy()
        {
            if (subscribed && network != null)
            {
                network.BeforePrecompute -= OnBeforePrecompute;
                network.AfterPrecomputeLinkages -= OnAfterPrecomputeLinkages;
                subscribed = false;
            }

            WormholeMouthNodeManager.ReleaseLocal(network);
        }

        private static void OnBeforePrecompute(RACommNetwork net)
        {
            if (net == null)
                return;

            WormholeRegistry.Refresh();
            WormholeMouthScienceResults.RegisterAll();
            WormholeMouthNodeManager.EnsureSynced(net, WormholeSettings.Current);
            WormholeLinkMetrics.PrepareCollectors(net);
        }

        private static void OnAfterPrecomputeLinkages(RACommNetwork net)
        {
            if (net == null)
                return;

            WormholeLinkBuilder.InjectLinks(net);
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
