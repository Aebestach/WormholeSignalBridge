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

            network.OnNetworkPreUpdate += OnNetworkPreUpdate;
            network.NetworkUpdateComplete.Add(OnNetworkUpdateComplete);
            subscribed = true;
            WormholeMouthNodeManager.EnsureSynced(network, WormholeSettings.Current);
            Log.Info("Initialized and subscribed to RealAntennas network rebuild events.");
        }

        private void OnDestroy()
        {
            if (subscribed && network != null)
            {
                network.OnNetworkPreUpdate -= OnNetworkPreUpdate;
                network.NetworkUpdateComplete.Remove(OnNetworkUpdateComplete);
                subscribed = false;
            }

            WormholeMouthNodeManager.ReleaseLocal(network, suppressTopologyRefresh: true);
        }

        private void OnNetworkPreUpdate()
        {
            if (network == null)
                return;

            WormholeRegistry.Refresh();
            WormholeMouthNodeManager.EnsureSynced(network, WormholeSettings.Current);
        }

        private void OnNetworkUpdateComplete()
        {
            if (network == null)
                return;

            WormholeRegistry.Refresh();
            WormholeMouthScienceResults.RegisterAll();
            WormholeLinkBuilder.InjectLinks(network);
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
