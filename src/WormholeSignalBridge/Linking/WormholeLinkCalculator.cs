using System;
using System.Collections.Generic;
using RealAntennas;
using RealAntennas.Network;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class RelayCandidate
    {
        internal RACommNode Node;
        internal RACommNode MouthNode;
        internal Vessel Vessel;
        internal CelestialBody WormholeBody;
        internal double OrbitQuality = 1;
        internal double OrbitExtraLossDb;
        internal double OrbitMetricMultiplier = 1;
        internal readonly List<RealAntenna> Antennas = new List<RealAntenna>();
    }

    internal readonly struct TunnelDirectionBudget
    {
        internal readonly RealAntenna Tx;
        internal readonly RealAntenna Rx;
        internal readonly double DataRate;
        internal readonly double MaxDataRate;
        internal readonly double Metric;

        internal TunnelDirectionBudget(RealAntenna tx, RealAntenna rx, double dataRate, double maxDataRate, double metric)
        {
            Tx = tx;
            Rx = rx;
            DataRate = dataRate;
            MaxDataRate = maxDataRate;
            Metric = metric;
        }
    }

    internal readonly struct TunnelLinkBudget
    {
        internal readonly TunnelDirectionBudget Fwd;
        internal readonly TunnelDirectionBudget Rev;

        internal TunnelLinkBudget(TunnelDirectionBudget fwd, TunnelDirectionBudget rev)
        {
            Fwd = fwd;
            Rev = rev;
        }
    }

    internal static class WormholeLinkCalculator
    {
        internal static bool TryCreateCandidate(
            RACommNode node,
            RACommNode mouthNode,
            CelestialBody wormholeBody,
            WormholeLinkSettings settings,
            out RelayCandidate candidate,
            out string rejectReason)
        {
            candidate = null;
            rejectReason = null;

            if (node == null)
            {
                rejectReason = "comm node is null";
                return false;
            }

            if (mouthNode == null)
            {
                rejectReason = "mouth node is null";
                return false;
            }

            if (wormholeBody == null)
            {
                rejectReason = "wormhole body is null";
                return false;
            }

            if (node.isGroundStation)
            {
                rejectReason = "ground station nodes cannot relay through wormholes";
                return false;
            }

            if (!node.CanComm())
            {
                rejectReason = "vessel comm offline or unpowered";
                return false;
            }

            Vessel vessel = node.ParentVessel;
            if (vessel == null)
            {
                rejectReason = "no parent vessel";
                return false;
            }

            if (vessel.mainBody != wormholeBody)
            {
                rejectReason = $"orbiting {vessel.mainBody?.name ?? "?"} instead of {wormholeBody.name}";
                return false;
            }

            if (vessel.Landed || vessel.Splashed)
            {
                rejectReason = vessel.Landed ? "landed" : "splashed";
                return false;
            }

            if (!TryEvaluateOrbit(vessel, wormholeBody, settings, out double quality, out double extraLossDb, out double metricMultiplier, out rejectReason))
                return false;

            candidate = new RelayCandidate
            {
                Node = node,
                MouthNode = mouthNode,
                Vessel = vessel,
                WormholeBody = wormholeBody,
                OrbitQuality = quality,
                OrbitExtraLossDb = extraLossDb,
                OrbitMetricMultiplier = metricMultiplier
            };

            if (node.RAAntennaList != null)
            {
                foreach (RealAntenna antenna in node.RAAntennaList)
                {
                    if (antenna is RealAntennaDigital && antenna.Shape != AntennaShape.Omni)
                        candidate.Antennas.Add(antenna);
                }
            }

            if (candidate.Antennas.Count == 0)
            {
                rejectReason = "no online directional (non-omni) digital antennas";
                candidate = null;
                return false;
            }

            return true;
        }

        internal static TunnelLinkBudget? BestTunnelLink(RelayCandidate a, RelayCandidate b, WormholeLinkSettings settings, LinkBudgetLookup budgets)
        {
            TunnelDirectionBudget? bestFwd = null;
            TunnelDirectionBudget? bestRev = null;

            foreach (RealAntenna tx in a.Antennas)
            {
                foreach (RealAntenna rx in b.Antennas)
                {
                    TunnelDirectionBudget? budget = DirectionBudget(a, tx, b, rx, settings, budgets);
                    if (budget.HasValue && (!bestFwd.HasValue || budget.Value.DataRate > bestFwd.Value.DataRate))
                        bestFwd = budget;
                }
            }

            foreach (RealAntenna tx in b.Antennas)
            {
                foreach (RealAntenna rx in a.Antennas)
                {
                    TunnelDirectionBudget? budget = DirectionBudget(b, tx, a, rx, settings, budgets);
                    if (budget.HasValue && (!bestRev.HasValue || budget.Value.DataRate > bestRev.Value.DataRate))
                        bestRev = budget;
                }
            }

            if (!bestFwd.HasValue || !bestRev.HasValue)
                return null;

            return new TunnelLinkBudget(bestFwd.Value, bestRev.Value);
        }

        internal static bool TryEvaluateOrbit(
            Vessel vessel,
            CelestialBody wormholeBody,
            WormholeLinkSettings settings,
            out string rejectReason) =>
            TryEvaluateOrbit(vessel, wormholeBody, settings, out _, out _, out _, out rejectReason);

        internal static TunnelDirectionBudget? DirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            WormholeLinkSettings settings,
            LinkBudgetLookup budgets)
        {
            if (!WormholeMouthPointing.PointsAtMouth(sourceAntenna, source.Vessel, source.WormholeBody, settings) ||
                !WormholeMouthPointing.PointsAtMouth(targetAntenna, target.Vessel, target.WormholeBody, settings))
                return null;

            LinkDetails entryBudget = budgets.GetByTransmitter(source.Node, source.MouthNode, sourceAntenna);
            LinkDetails exitBudget = budgets.GetByReceiver(target.MouthNode, target.Node, targetAntenna);
            if (entryBudget.tx == null || exitBudget.rx == null)
                return BackgroundDirectionBudget(source, sourceAntenna, target, targetAntenna, settings);

            double dataRate = Math.Min(entryBudget.dataRate, exitBudget.dataRate);
            if (dataRate <= 0)
                return null;

            double maxDataRate = Math.Min(entryBudget.maxDataRate, exitBudget.maxDataRate);
            double metric = Math.Min(entryBudget.Metric, exitBudget.Metric);
            return CreateDirectionBudget(source, sourceAntenna, target, targetAntenna, dataRate, maxDataRate, metric, settings);
        }

        internal static bool HasSnapshotBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            LinkBudgetLookup budgets) =>
            budgets.GetByTransmitter(source.Node, source.MouthNode, sourceAntenna).tx != null &&
            budgets.GetByReceiver(target.MouthNode, target.Node, targetAntenna).rx != null;

        internal static double EstimateBackgroundDataRate(RealAntenna sourceAntenna, RealAntenna targetAntenna)
        {
            if (sourceAntenna == null || targetAntenna == null)
                return 0;

            if (!sourceAntenna.Compatible(targetAntenna))
                return 0;

            if (sourceAntenna is RealAntennaDigital sourceDigital &&
                targetAntenna is RealAntennaDigital targetDigital &&
                !sourceDigital.modulator.Compatible(targetDigital.modulator))
                return 0;

            return Math.Min(sourceAntenna.DataRate, targetAntenna.DataRate);
        }

        private static TunnelDirectionBudget? BackgroundDirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            WormholeLinkSettings settings)
        {
            double dataRate = EstimateBackgroundDataRate(sourceAntenna, targetAntenna);
            if (dataRate <= 0)
                return null;

            return CreateDirectionBudget(source, sourceAntenna, target, targetAntenna, dataRate, dataRate, 1.0, settings);
        }

        private static TunnelDirectionBudget CreateDirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            double dataRate,
            double maxDataRate,
            double metric,
            WormholeLinkSettings settings)
        {
            metric *= 1.0 / (1.0 + Math.Max(0, settings.InsertionLoss) / 20.0);
            metric *= source.OrbitMetricMultiplier * target.OrbitMetricMultiplier;
            metric = Math.Max(0.01, Math.Min(1.0, metric));

            return new TunnelDirectionBudget(sourceAntenna, targetAntenna, dataRate, maxDataRate, metric);
        }

        private static bool TryEvaluateOrbit(Vessel vessel, CelestialBody wormholeBody, WormholeLinkSettings settings, out double quality, out double extraLossDb, out double metricMultiplier, out string rejectReason)
        {
            quality = 1;
            extraLossDb = 0;
            metricMultiplier = 1;
            rejectReason = null;

            Orbit orbit = vessel.orbit;
            if (orbit == null)
            {
                rejectReason = "no orbit";
                return false;
            }

            double altitude = vessel.altitude;
            double influenceFloor = WormholeRegistry.GetMinimumRelayAltitude(wormholeBody);
            if (influenceFloor > 0 && altitude < influenceFloor)
            {
                rejectReason = $"altitude {altitude:F0} m below Hinf {influenceFloor:F0} m";
                return false;
            }

            if (!settings.AdvancedOrbitConstraints)
                return true;

            if (altitude > settings.MaxMouthAltitude)
            {
                rejectReason = $"altitude {altitude:F0} m above max {settings.MaxMouthAltitude:F0} m";
                return false;
            }

            double optimalMin = influenceFloor;
            double optimalMax = Math.Max(optimalMin, settings.OptimalMaxAltitude);
            if (optimalMax > settings.MaxMouthAltitude)
                optimalMax = settings.MaxMouthAltitude;

            if (settings.StrictPeApBounds && orbit.PeA < influenceFloor)
            {
                rejectReason = $"PeA {orbit.PeA:F0} m below Hinf {influenceFloor:F0} m";
                return false;
            }

            if (settings.StrictPeApBounds && orbit.ApA > settings.MaxApA)
            {
                rejectReason = $"ApA {orbit.ApA:F0} m above max ApA {settings.MaxApA:F0} m";
                return false;
            }

            if (settings.StrictInclinationBounds && !InRange(orbit.inclination, settings.MinInclination, settings.MaxInclination))
            {
                rejectReason = $"inclination {orbit.inclination:F1}° outside [{settings.MinInclination:F1}°, {settings.MaxInclination:F1}°]";
                return false;
            }

            if (settings.StrictEccentricityBounds && orbit.eccentricity > settings.MaxEccentricity)
            {
                rejectReason = $"eccentricity {orbit.eccentricity:F3} above max {settings.MaxEccentricity:F3}";
                return false;
            }

            double altitudeQuality = AltitudeQuality(altitude, optimalMin, optimalMax, settings.MaxMouthAltitude, settings.EdgeQuality);
            double eccentricityQuality = EccentricityQuality(orbit.eccentricity, settings);
            double coverageQuality = CoverageQuality(orbit, influenceFloor, settings.MaxMouthAltitude, settings.MaxApA, settings.EdgeQuality);
            double inclinationQuality = InclinationQuality(orbit.inclination, settings);
            quality = altitudeQuality * eccentricityQuality * coverageQuality * inclinationQuality;
            if (quality < settings.MinUsableOrbitQuality)
            {
                rejectReason = $"orbit quality {quality:F3} below minimum {settings.MinUsableOrbitQuality:F3} " +
                               $"(alt {altitudeQuality:F2}, ecc {eccentricityQuality:F2}, cov {coverageQuality:F2}, inc {inclinationQuality:F2})";
                return false;
            }

            double clamped = Math.Max(settings.MinUsableOrbitQuality, Math.Min(1.0, quality));
            extraLossDb = -10.0 * Math.Log10(clamped) * settings.OrbitLossScale;
            metricMultiplier = Math.Sqrt(clamped);
            return true;
        }

        private static double AltitudeQuality(double altitude, double optimalMin, double optimalMax, double maxAltitude, double edgeQuality)
        {
            if (altitude >= optimalMin && altitude <= optimalMax)
                return 1;
            if (altitude < optimalMin)
                return edgeQuality;
            return SmoothQuality(altitude, optimalMax, maxAltitude, 1, edgeQuality);
        }

        private static double EccentricityQuality(double eccentricity, WormholeLinkSettings settings)
        {
            if (eccentricity <= settings.IdealMaxEccentricity)
                return 1;
            if (settings.MaxEccentricity <= settings.IdealMaxEccentricity)
                return settings.EdgeQuality;
            return SmoothQuality(eccentricity, settings.IdealMaxEccentricity, settings.MaxEccentricity, 1, settings.EdgeQuality);
        }

        private static double CoverageQuality(Orbit orbit, double minAltitude, double maxAltitude, double maxApA, double edgeQuality)
        {
            double upperBound = Math.Min(maxAltitude, maxApA);
            if (orbit.PeA >= minAltitude && orbit.ApA <= upperBound)
                return 1;

            double span = Math.Max(1, orbit.ApA - orbit.PeA);
            double overlap = Math.Max(0, Math.Min(orbit.ApA, upperBound) - Math.Max(orbit.PeA, minAltitude));
            double fraction = Math.Max(0, Math.Min(1, overlap / span));
            return edgeQuality + ((1 - edgeQuality) * fraction);
        }

        private static double InclinationQuality(double inclination, WormholeLinkSettings settings)
        {
            if (!settings.UseInclinationQuality)
                return 1;
            if (InRange(inclination, settings.PreferredInclinationMin, settings.PreferredInclinationMax))
                return 1;
            if (settings.StrictInclinationBounds && !InRange(inclination, settings.MinInclination, settings.MaxInclination))
                return 0;
            return settings.EdgeQuality;
        }

        private static bool InRange(double value, double min, double max) => value >= min && value <= max;

        private static double SmoothQuality(double value, double min, double max, double low, double high)
        {
            if (max <= min)
                return high;
            double t = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            double smooth = t * t * (3 - (2 * t));
            return low + ((high - low) * smooth);
        }
    }

    internal sealed class LinkBudgetLookup
    {
        private static readonly LinkDetails Empty = default;

        private readonly Dictionary<(RACommNode txNode, RACommNode rxNode, RealAntenna tx), LinkDetails> byTransmitter;
        private readonly Dictionary<(RACommNode txNode, RACommNode rxNode, RealAntenna rx), LinkDetails> byReceiver;

        private LinkBudgetLookup(
            Dictionary<(RACommNode, RACommNode, RealAntenna), LinkDetails> byTransmitter,
            Dictionary<(RACommNode, RACommNode, RealAntenna), LinkDetails> byReceiver)
        {
            this.byTransmitter = byTransmitter;
            this.byReceiver = byReceiver;
        }

        internal static LinkBudgetLookup FromCollectors(IEnumerable<LinkMetricsCollector> collectors)
        {
            var byTx = new Dictionary<(RACommNode, RACommNode, RealAntenna), LinkDetails>();
            var byRx = new Dictionary<(RACommNode, RACommNode, RealAntenna), LinkDetails>();
            if (collectors != null)
            {
                foreach (LinkMetricsCollector collector in collectors)
                {
                    if (collector?.Items == null)
                        continue;

                    foreach (List<LinkDetails> detailsList in collector.Items.Values)
                    {
                        foreach (LinkDetails detail in detailsList)
                        {
                            if (detail.dataRate <= 0 || detail.txNode == null || detail.rxNode == null ||
                                detail.tx == null || detail.rx == null)
                                continue;

                            KeepBest(byTx, (detail.txNode, detail.rxNode, detail.tx), detail);
                            KeepBest(byRx, (detail.txNode, detail.rxNode, detail.rx), detail);
                        }
                    }
                }
            }

            return new LinkBudgetLookup(byTx, byRx);
        }

        internal LinkDetails GetByTransmitter(RACommNode txNode, RACommNode rxNode, RealAntenna tx)
        {
            if (txNode == null || rxNode == null || tx == null)
                return Empty;

            byTransmitter.TryGetValue((txNode, rxNode, tx), out LinkDetails budget);
            return budget;
        }

        internal LinkDetails GetByReceiver(RACommNode txNode, RACommNode rxNode, RealAntenna rx)
        {
            if (txNode == null || rxNode == null || rx == null)
                return Empty;

            byReceiver.TryGetValue((txNode, rxNode, rx), out LinkDetails budget);
            return budget;
        }

        private static void KeepBest(
            Dictionary<(RACommNode, RACommNode, RealAntenna), LinkDetails> map,
            (RACommNode, RACommNode, RealAntenna) key,
            LinkDetails budget)
        {
            if (!map.TryGetValue(key, out LinkDetails existing) || budget.dataRate > existing.dataRate)
                map[key] = budget;
        }
    }
}
