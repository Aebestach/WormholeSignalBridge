using System.Collections.Generic;
using CommNet;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeLinkBuilder
    {
        internal static void InjectLinks(RACommNetwork network)
        {
            if (network == null || !RealAntennasReflection.IsReady)
                return;

            WormholeRegistry.Refresh();

            if (!WormholeSettings.Defaults.Enabled)
                return;

            int created = 0;
            WormholeLinkSettings settings = WormholeSettings.Defaults;
            foreach (WormholePair pair in WormholeRegistry.ActivePairs)
            {
                List<RACommNode> nodesA = CollectCommNodes(network, pair.BodyA);
                List<RACommNode> nodesB = CollectCommNodes(network, pair.BodyB);

                foreach (RACommNode nodeA in nodesA)
                {
                    foreach (RACommNode nodeB in nodesB)
                    {
                        if (TryCreateTunnelLink(network, nodeA, nodeB, settings))
                            created++;
                    }
                }
            }

            if (WormholeSettings.DebugLogging && created > 0)
                Log.Info($"Injected or updated {created} wormhole tunnel link(s).");
        }

        private static bool TryCreateTunnelLink(RACommNetwork network, RACommNode nodeA, RACommNode nodeB, WormholeLinkSettings settings)
        {
            DirectionalLink? bestFwd = null;
            DirectionalLink? bestRev = null;

            foreach (RealAntenna tx in nodeA.RAAntennaList)
            {
                foreach (RealAntenna rx in nodeB.RAAntennaList)
                {
                    DirectionalLink? candidate = WormholeLinkCalculator.BestDirectionalLink(tx, rx, settings);
                    if (!candidate.HasValue)
                        continue;

                    if (!bestFwd.HasValue || candidate.Value.DataRate > bestFwd.Value.DataRate)
                        bestFwd = candidate;
                }
            }

            foreach (RealAntenna tx in nodeB.RAAntennaList)
            {
                foreach (RealAntenna rx in nodeA.RAAntennaList)
                {
                    DirectionalLink? candidate = WormholeLinkCalculator.BestDirectionalLink(tx, rx, settings);
                    if (!candidate.HasValue)
                        continue;

                    if (!bestRev.HasValue || candidate.Value.DataRate > bestRev.Value.DataRate)
                        bestRev = candidate;
                }
            }

            if (!bestFwd.HasValue || !bestRev.HasValue)
                return false;

            if (bestFwd.Value.DataRate <= 0 || bestRev.Value.DataRate <= 0)
                return false;

            double tunnelDistance = settings.EffectiveDistance;
            RealAntennasReflection.MakeLink(
                network,
                bestFwd.Value.Tx,
                bestFwd.Value.Rx,
                bestRev.Value.Tx,
                bestRev.Value.Rx,
                nodeA,
                nodeB,
                tunnelDistance,
                bestFwd.Value.DataRate,
                bestRev.Value.DataRate,
                bestFwd.Value.MaxDataRate,
                bestFwd.Value.Metric,
                bestRev.Value.Metric);

            if (WormholeSettings.DebugLogging)
            {
                Log.Info(
                    $"Wormhole link {nodeA.displayName} <-> {nodeB.displayName}: " +
                    $"{RATools.PrettyPrintDataRate(bestFwd.Value.DataRate)}/{RATools.PrettyPrintDataRate(bestRev.Value.DataRate)} " +
                    $"(loss {settings.InsertionLoss} dB, distance {settings.EffectiveDistance} m)");
            }

            return true;
        }

        private static List<RACommNode> CollectCommNodes(RACommNetwork network, CelestialBody wormholeBody)
        {
            var result = new List<RACommNode>();
            if (wormholeBody == null)
                return result;

            foreach (CommNode node in network.Nodes)
            {
                if (!(node is RACommNode raNode))
                    continue;

                if (!IsWormholeMouthNode(raNode, wormholeBody))
                    continue;

                if (!raNode.CanComm() || raNode.RAAntennaList == null || raNode.RAAntennaList.Count == 0)
                    continue;

                result.Add(raNode);
            }

            return result;
        }

        internal static bool IsWormholeMouthNode(RACommNode node, CelestialBody wormholeBody)
        {
            if (node.isGroundStation)
                return false;

            Vessel vessel = node.ParentVessel;
            return vessel != null && vessel.mainBody == wormholeBody;
        }
    }
}
