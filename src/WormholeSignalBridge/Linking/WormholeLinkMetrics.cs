using System.Collections.Generic;
using CommNet;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeLinkMetrics
    {
        internal static void PrepareCollectors(RACommNetwork network)
        {
            if (network == null)
                return;

            network.ClearLinkMetricsCollectors();

            if (!WormholeSettings.Current.Enabled)
                return;

            var bodies = new HashSet<CelestialBody>();
            foreach (WormholePair pair in WormholeRegistry.ActivePairs)
            {
                if (pair.BodyA != null)
                    bodies.Add(pair.BodyA);
                if (pair.BodyB != null)
                    bodies.Add(pair.BodyB);
            }

            foreach (CommNode node in network.Nodes)
            {
                if (!(node is RACommNode raNode))
                    continue;

                Vessel vessel = raNode.ParentVessel;
                if (vessel == null || !bodies.Contains(vessel.mainBody))
                    continue;

                if (raNode.RAAntennaList == null)
                    continue;

                foreach (RealAntenna antenna in raNode.RAAntennaList)
                {
                    if (antenna is RealAntennaDigital && antenna.Shape != AntennaShape.Omni)
                        network.RegisterLinkMetricsCollector(antenna);
                }
            }
        }
    }
}
