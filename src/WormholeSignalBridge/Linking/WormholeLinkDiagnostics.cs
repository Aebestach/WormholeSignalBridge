using System.Text;
using RealAntennas;

namespace WormholeSignalBridge
{
    internal static class WormholeLinkDiagnostics
    {
        internal static string DescribeTunnelFailure(RelayCandidate a, RelayCandidate b, WormholeLinkSettings settings)
        {
            var sb = new StringBuilder();
            sb.Append($"no viable tunnel between {a.Vessel.vesselName} and {b.Vessel.vesselName}");

            TunnelDirectionBudget? fwd = FindBestDirection(a, b, settings, out string fwdDetail);
            TunnelDirectionBudget? rev = FindBestDirection(b, a, settings, out string revDetail);

            sb.Append("; fwd (");
            sb.Append(fwdDetail);
            sb.Append("); rev (");
            sb.Append(revDetail);
            sb.Append(')');
            return sb.ToString();
        }

        private static TunnelDirectionBudget? FindBestDirection(
            RelayCandidate source,
            RelayCandidate target,
            WormholeLinkSettings settings,
            out string detail)
        {
            TunnelDirectionBudget? best = null;
            string bestDetail = "no directional antenna pairs";
            int pairsChecked = 0;

            foreach (RealAntenna tx in source.Antennas)
            {
                foreach (RealAntenna rx in target.Antennas)
                {
                    pairsChecked++;
                    string pairDetail;
                    TunnelDirectionBudget? budget = TryDirectionBudget(source, tx, target, rx, settings, out pairDetail);
                    if (budget.HasValue && (!best.HasValue || budget.Value.DataRate > best.Value.DataRate))
                    {
                        best = budget;
                        bestDetail = pairDetail;
                    }
                    else if (!best.HasValue && pairDetail != null)
                        bestDetail = pairDetail;
                }
            }

            detail = pairsChecked == 0
                ? "no directional antennas on one or both relays"
                : best.HasValue ? bestDetail : bestDetail;
            return best;
        }

        private static TunnelDirectionBudget? TryDirectionBudget(
            RelayCandidate source,
            RealAntenna sourceAntenna,
            RelayCandidate target,
            RealAntenna targetAntenna,
            WormholeLinkSettings settings,
            out string detail)
        {
            detail = null;
            string txLabel = AntennaLabel(sourceAntenna);
            string rxLabel = AntennaLabel(targetAntenna);

            if (!WormholeMouthPointing.PointsAtMouth(sourceAntenna, source.Vessel, source.WormholeBody, settings))
            {
                detail = $"{txLabel} not aimed at {source.WormholeBody.name} mouth ({WormholeMouthPointing.Describe(sourceAntenna, source.Vessel, source.WormholeBody, settings)})";
                return null;
            }

            if (!WormholeMouthPointing.PointsAtMouth(targetAntenna, target.Vessel, target.WormholeBody, settings))
            {
                detail = $"{rxLabel} not aimed at {target.WormholeBody.name} mouth ({WormholeMouthPointing.Describe(targetAntenna, target.Vessel, target.WormholeBody, settings)})";
                return null;
            }

            TunnelDirectionBudget? budget = WormholeLinkCalculator.DirectionBudget(
                source,
                sourceAntenna,
                target,
                targetAntenna,
                settings);

            if (!budget.HasValue)
            {
                detail = WormholeLinkCalculator.HasRaMouthBudget(source, sourceAntenna, target, targetAntenna)
                    ? $"{txLabel} <-> {rxLabel}: incompatible tunnel antennas or no positive RA data rate"
                    : $"{txLabel} <-> {rxLabel}: missing RA mouth link budget";
                return null;
            }

            detail = $"{txLabel} <-> {rxLabel} @ {RATools.PrettyPrintDataRate(budget.Value.DataRate)} (RA mouth link)";
            return budget;
        }

        private static string AntennaLabel(RealAntenna antenna) =>
            antenna == null ? "?" : $"{antenna.RFBand?.name ?? "?"} {antenna.Shape} {antenna.antennaDiameter:F0}m";
    }
}
