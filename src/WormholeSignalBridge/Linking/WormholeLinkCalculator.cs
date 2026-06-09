using System;
using System.Collections.Generic;
using CommNet;
using RealAntennas;
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

            if (!TryEvaluateOrbit(vessel, wormholeBody, settings, out double quality, out double metricMultiplier, out rejectReason))
                return false;

            candidate = new RelayCandidate
            {
                Node = node,
                MouthNode = mouthNode,
                Vessel = vessel,
                WormholeBody = wormholeBody,
                OrbitQuality = quality,
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

        internal static TunnelLinkBudget? BestTunnelLink(RelayCandidate a, RelayCandidate b, WormholeLinkSettings settings)
        {
            TunnelDirectionBudget? bestFwd = null;
            TunnelDirectionBudget? bestRev = null;

            foreach (RealAntenna tx in a.Antennas)
            {
                foreach (RealAntenna rx in b.Antennas)
                {
                    TunnelDirectionBudget? budget = DirectionBudget(a, tx, b, rx, settings);
                    if (budget.HasValue && (!bestFwd.HasValue || budget.Value.DataRate > bestFwd.Value.DataRate))
                        bestFwd = budget;
                }
            }

            foreach (RealAntenna tx in b.Antennas)
            {
                foreach (RealAntenna rx in a.Antennas)
                {
                    TunnelDirectionBudget? budget = DirectionBudget(b, tx, a, rx, settings);
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
            TryEvaluateOrbit(vessel, wormholeBody, settings, out _, out _, out rejectReason);

        internal static TunnelDirectionBudget? DirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            WormholeLinkSettings settings)
        {
            if (!WormholeMouthPointing.PointsAtMouth(sourceAntenna, source.Vessel, source.WormholeBody, settings) ||
                !WormholeMouthPointing.PointsAtMouth(targetAntenna, target.Vessel, target.WormholeBody, settings))
                return null;

            if (!AntennasTunnelCompatible(sourceAntenna, targetAntenna))
                return null;

            return RaMouthDirectionBudget(source, sourceAntenna, target, targetAntenna, settings);
        }

        internal static bool HasRaMouthBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna) =>
            TryGetVesselToMouthBudget(source.Node, source.MouthNode, sourceAntenna, out _, out _, out _) &&
            TryGetMouthToVesselBudget(target.Node, target.MouthNode, targetAntenna, out _, out _, out _);

        internal static double EstimateMouthLinkDataRate(RACommNode vessel, RACommNode mouth, RealAntenna vesselAntenna)
        {
            if (TryGetVesselToMouthBudget(vessel, mouth, vesselAntenna, out double rate, out _, out _) && rate > 0)
                return rate;

            return 0;
        }

        private static TunnelDirectionBudget? RaMouthDirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            WormholeLinkSettings settings)
        {
            if (!TryGetVesselToMouthBudget(source.Node, source.MouthNode, sourceAntenna, out double srcRate, out double srcMax, out double srcMetric))
                return null;

            if (!TryGetMouthToVesselBudget(target.Node, target.MouthNode, targetAntenna, out double dstRate, out double dstMax, out double dstMetric))
                return null;

            double dataRate = Math.Min(srcRate, dstRate);
            if (dataRate <= 0)
                return null;

            double maxDataRate = Math.Min(srcMax, dstMax);
            double metric = Math.Min(srcMetric, dstMetric);
            return CreateDirectionBudget(source, sourceAntenna, target, targetAntenna, dataRate, maxDataRate, metric, settings);
        }

        private static bool TryGetVesselToMouthBudget(
            RACommNode vessel,
            RACommNode mouth,
            RealAntenna vesselAntenna,
            out double dataRate,
            out double maxDataRate,
            out double metric)
        {
            dataRate = 0;
            maxDataRate = 0;
            metric = 0;

            if (!TryGetRaCommLink(vessel, mouth, out RACommLink link))
                return false;

            if (ReferenceEquals(link.a, vessel))
            {
                if (!AntennaMatches(link.FwdAntennaTx, vesselAntenna))
                    return false;

                dataRate = link.FwdDataRate;
                maxDataRate = link.FwdDataRate;
                metric = link.FwdMetric;
                return dataRate > 0;
            }

            if (ReferenceEquals(link.b, vessel))
            {
                if (!AntennaMatches(link.RevAntennaTx, vesselAntenna))
                    return false;

                dataRate = link.RevDataRate;
                maxDataRate = link.RevDataRate;
                metric = link.RevMetric;
                return dataRate > 0;
            }

            return false;
        }

        private static bool TryGetMouthToVesselBudget(
            RACommNode vessel,
            RACommNode mouth,
            RealAntenna vesselAntenna,
            out double dataRate,
            out double maxDataRate,
            out double metric)
        {
            dataRate = 0;
            maxDataRate = 0;
            metric = 0;

            if (!TryGetRaCommLink(vessel, mouth, out RACommLink link))
                return false;

            if (ReferenceEquals(link.a, vessel))
            {
                if (!AntennaMatches(link.RevAntennaRx, vesselAntenna))
                    return false;

                dataRate = link.RevDataRate;
                maxDataRate = link.RevDataRate;
                metric = link.RevMetric;
                return dataRate > 0;
            }

            if (ReferenceEquals(link.b, vessel))
            {
                if (!AntennaMatches(link.FwdAntennaRx, vesselAntenna))
                    return false;

                dataRate = link.FwdDataRate;
                maxDataRate = link.FwdDataRate;
                metric = link.FwdMetric;
                return dataRate > 0;
            }

            return false;
        }

        private static bool TryGetRaCommLink(RACommNode a, RACommNode b, out RACommLink link)
        {
            link = null;
            if (a == null || b == null)
                return false;

            if (!a.TryGetValue(b, out CommLink commLink) || !(commLink is RACommLink raLink))
                return false;

            link = raLink;
            return true;
        }

        private static bool AntennasTunnelCompatible(RealAntenna sourceAntenna, RealAntenna targetAntenna)
        {
            if (sourceAntenna == null || targetAntenna == null)
                return false;

            if (!sourceAntenna.Compatible(targetAntenna))
                return false;

            if (sourceAntenna is RealAntennaDigital sourceDigital &&
                targetAntenna is RealAntennaDigital targetDigital &&
                !sourceDigital.modulator.Compatible(targetDigital.modulator))
                return false;

            return true;
        }

        private static bool AntennaMatches(RealAntenna linkAntenna, RealAntenna requested) =>
            linkAntenna != null && requested != null && ReferenceEquals(linkAntenna, requested);

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

        private static bool TryEvaluateOrbit(Vessel vessel, CelestialBody wormholeBody, WormholeLinkSettings settings, out double quality, out double metricMultiplier, out string rejectReason)
        {
            quality = 1;
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
            metricMultiplier = Math.Pow(clamped, Math.Max(0, settings.OrbitLossScale) * 0.5);
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

}
