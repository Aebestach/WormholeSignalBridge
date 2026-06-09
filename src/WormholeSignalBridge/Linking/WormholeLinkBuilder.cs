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
            if (network == null)
                return;

            WormholeRegistry.Refresh();

            WormholeLinkSettings settings = WormholeSettings.Current;
            if (!settings.Enabled)
            {
                Log.DebugLog("Wormhole comm bridge disabled in difficulty settings; skipping link injection.");
                return;
            }

            bool debug = settings.DebugLogging;
            int created = 0;
            int failedPairs = 0;
            LinkBudgetLookup budgets = LinkBudgetLookup.FromCollectors(network.linkMetricsCollectors);
            Dictionary<CelestialBody, List<RelayCandidate>> candidatesByBody = CollectCandidates(network, settings, debug, budgets);

            if (candidatesByBody.Count == 0)
            {
                if (debug)
                {
                    Log.DebugLog(
                        $"No wormhole relay candidates this rebuild (pairs={WormholeRegistry.ActivePairs.Count}, " +
                        $"mouth nodes={WormholeMouthNodeManager.ActiveNodes.Count}, " +
                        $"link metric collectors={network.linkMetricsCollectors?.Count ?? 0}).");
                }
                return;
            }

            if (debug)
            {
                foreach (KeyValuePair<CelestialBody, List<RelayCandidate>> entry in candidatesByBody)
                {
                    foreach (RelayCandidate candidate in entry.Value)
                    {
                        Log.DebugLog(
                            $"Candidate {candidate.Vessel.vesselName} @ {entry.Key.name}: " +
                            $"{candidate.Antennas.Count} directional antenna(s), orbit quality {candidate.OrbitQuality:F2}");
                    }
                }
            }

            foreach (WormholePair pair in WormholeRegistry.ActivePairs)
            {
                bool hasA = candidatesByBody.TryGetValue(pair.BodyA, out List<RelayCandidate> nodesA);
                bool hasB = candidatesByBody.TryGetValue(pair.BodyB, out List<RelayCandidate> nodesB);

                if (!hasA || !hasB)
                {
                    if (debug)
                    {
                        if (!hasA)
                            Log.DebugLog($"Pair {pair.BodyA.name}<->{pair.BodyB.name}: no candidate on {pair.BodyA.name}.");
                        if (!hasB)
                            Log.DebugLog($"Pair {pair.BodyA.name}<->{pair.BodyB.name}: no candidate on {pair.BodyB.name}.");
                    }
                    continue;
                }

                foreach (RelayCandidate nodeA in nodesA)
                {
                    foreach (RelayCandidate nodeB in nodesB)
                    {
                        if (TryCreateTunnelLink(network, nodeA, nodeB, settings, budgets))
                        {
                            created++;
                        }
                        else if (debug)
                        {
                            failedPairs++;
                            Log.DebugLog(WormholeLinkDiagnostics.DescribeTunnelFailure(nodeA, nodeB, settings, budgets));
                        }
                    }
                }
            }

            if (debug)
            {
                Log.DebugLog(
                    $"Rebuild summary: injected/updated {created} tunnel link(s), " +
                    $"{failedPairs} vessel pair(s) failed, link metric collectors={network.linkMetricsCollectors?.Count ?? 0}.");
            }
        }

        private static bool TryCreateTunnelLink(RACommNetwork network, RelayCandidate nodeA, RelayCandidate nodeB, WormholeLinkSettings settings, LinkBudgetLookup budgets)
        {
            TunnelLinkBudget? budget = WormholeLinkCalculator.BestTunnelLink(nodeA, nodeB, settings, budgets);
            if (!budget.HasValue || budget.Value.Fwd.DataRate <= 0 || budget.Value.Rev.DataRate <= 0)
                return false;

            double tunnelDistance = settings.EffectiveDistance;
            network.MakeLink(
                budget.Value.Fwd.Tx,
                budget.Value.Fwd.Rx,
                budget.Value.Rev.Tx,
                budget.Value.Rev.Rx,
                nodeA.Node,
                nodeB.Node,
                tunnelDistance,
                budget.Value.Fwd.DataRate,
                budget.Value.Rev.DataRate,
                budget.Value.Fwd.MaxDataRate,
                budget.Value.Fwd.Metric,
                budget.Value.Rev.Metric);

            if (settings.DebugLogging)
            {
                Log.DebugLog(
                    $"Injected tunnel {nodeA.Node.displayName} <-> {nodeB.Node.displayName}: " +
                    $"{RATools.PrettyPrintDataRate(budget.Value.Fwd.DataRate)}/{RATools.PrettyPrintDataRate(budget.Value.Rev.DataRate)} " +
                    $"(insertion loss {settings.InsertionLoss} dB, orbit quality {nodeA.OrbitQuality:F2}/{nodeB.OrbitQuality:F2})");
            }

            return true;
        }

        private static Dictionary<CelestialBody, List<RelayCandidate>> CollectCandidates(RACommNetwork network, WormholeLinkSettings settings, bool debug, LinkBudgetLookup budgets)
        {
            var result = new Dictionary<CelestialBody, List<RelayCandidate>>();
            var bodies = new HashSet<CelestialBody>();
            foreach (WormholePair pair in WormholeRegistry.ActivePairs)
            {
                if (pair.BodyA != null)
                    bodies.Add(pair.BodyA);
                if (pair.BodyB != null)
                    bodies.Add(pair.BodyB);
            }

            if (debug && bodies.Count == 0)
                Log.DebugLog("No active wormhole pairs registered.");

            IReadOnlyDictionary<CelestialBody, WormholeMouthNode> mouthNodes = WormholeMouthNodeManager.ActiveNodes;
            foreach (CommNode node in network.Nodes)
            {
                if (!(node is RACommNode raNode))
                    continue;

                Vessel vessel = raNode.ParentVessel;
                if (vessel == null || !bodies.Contains(vessel.mainBody))
                    continue;

                if (!mouthNodes.TryGetValue(vessel.mainBody, out WormholeMouthNode mouth))
                {
                    if (debug)
                        Log.DebugLog($"Vessel {vessel.vesselName} @ {vessel.mainBody.name}: no WSB mouth node registered.");
                    continue;
                }

                if (!WormholeLinkCalculator.TryCreateCandidate(raNode, mouth.Node, vessel.mainBody, settings, out RelayCandidate candidate, out string rejectReason))
                {
                    if (debug)
                        Log.DebugLog($"Vessel {vessel.vesselName} @ {vessel.mainBody.name}: {rejectReason}");
                    continue;
                }

                if (debug)
                {
                    foreach (RealAntenna antenna in candidate.Antennas)
                    {
                        Log.DebugLog(
                            $"  {vessel.vesselName} antenna {antenna.RFBand?.name} {antenna.Shape} {antenna.antennaDiameter:F0}m -> {vessel.mainBody.name} mouth: " +
                            WormholeMouthPointing.Describe(antenna, vessel, vessel.mainBody, settings));
                    }
                }

                if (!result.TryGetValue(vessel.mainBody, out List<RelayCandidate> candidates))
                {
                    candidates = new List<RelayCandidate>();
                    result.Add(vessel.mainBody, candidates);
                }
                candidates.Add(candidate);
            }

            return result;
        }
    }
}
