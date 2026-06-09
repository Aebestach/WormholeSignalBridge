using System;
using System.Collections.Generic;
using KopernicusExpansion.Wormholes;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class WormholePair
    {
        internal CelestialBody BodyA;
        internal CelestialBody BodyB;
    }

    internal sealed class WormholeBodyInfo
    {
        internal CelestialBody Body;
        internal double InfluenceAltitude;
        internal double JumpMinAltitude;
        internal double JumpMaxAltitude;
    }

    internal static class WormholeRegistry
    {
        private static readonly List<WormholePair> Pairs = new List<WormholePair>();
        private static readonly Dictionary<CelestialBody, WormholeBodyInfo> BodyInfoByBody = new Dictionary<CelestialBody, WormholeBodyInfo>();

        internal static IReadOnlyList<WormholePair> ActivePairs => Pairs;

        internal static IEnumerable<CelestialBody> AllWormholeBodies => BodyInfoByBody.Keys;

        internal static bool TryGetBodyInfo(CelestialBody body, out WormholeBodyInfo info) =>
            BodyInfoByBody.TryGetValue(body, out info);

        internal static bool TryGetPartner(CelestialBody body, out CelestialBody partner)
        {
            partner = null;
            if (body == null)
                return false;

            foreach (WormholePair pair in Pairs)
            {
                if (pair.BodyA == body)
                {
                    partner = pair.BodyB;
                    return partner != null;
                }

                if (pair.BodyB == body)
                {
                    partner = pair.BodyA;
                    return partner != null;
                }
            }

            return false;
        }

        /// <summary>
        /// Minimum relay altitude from KEX. Uses influenceAltitude; falls back to jumpMaxAltitude when unset.
        /// Returns 0 when no KEX floor is configured.
        /// </summary>
        internal static double GetMinimumRelayAltitude(CelestialBody body)
        {
            if (!TryGetBodyInfo(body, out WormholeBodyInfo info))
                return 0;

            if (info.InfluenceAltitude > 0)
                return info.InfluenceAltitude;

            if (info.JumpMaxAltitude > 0)
                return info.JumpMaxAltitude;

            return 0;
        }

        /// <summary>
        /// RF proxy altitude for the mouth target: midpoint between the KEX jump zone ceiling and Hinf.
        /// </summary>
        internal static double GetMouthProxyAltitude(CelestialBody body, WormholeLinkSettings settings)
        {
            double hInf = GetMinimumRelayAltitude(body);
            double jumpCeiling = GetJumpZoneCeiling(body);

            if (hInf > 0 && jumpCeiling > 0)
            {
                if (jumpCeiling >= hInf)
                    return hInf * 0.95;
                return (jumpCeiling + hInf) * 0.5;
            }

            if (hInf > 0)
                return hInf * 0.95;

            if (jumpCeiling > 0)
                return jumpCeiling;

            return Math.Max(1000, (settings?.OptimalMaxAltitude ?? 200000) * 0.5);
        }

        private static double GetJumpZoneCeiling(CelestialBody body)
        {
            if (!TryGetBodyInfo(body, out WormholeBodyInfo info))
                return 0;

            if (info.JumpMaxAltitude > 0)
                return info.JumpMaxAltitude;

            if (info.JumpMinAltitude > 0)
                return info.JumpMinAltitude;

            return 0;
        }

        internal static void Refresh()
        {
            Pairs.Clear();
            BodyInfoByBody.Clear();

            var bodies = PSystemManager.Instance?.localBodies;
            if (bodies == null || bodies.Count == 0)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CelestialBody body in bodies)
            {
                WormholeComponent wormhole = body.GetComponent<WormholeComponent>();
                if (wormhole == null || string.IsNullOrEmpty(wormhole.partnerBody))
                    continue;

                RegisterBodyInfo(body, wormhole);

                CelestialBody partner = bodies.Find(b => b.name == wormhole.partnerBody);
                if (partner == null)
                {
                    Log.Warning($"Wormhole body {body.name} references missing partner {wormhole.partnerBody}.");
                    continue;
                }

                WormholeComponent partnerWormhole = partner.GetComponent<WormholeComponent>();
                if (partnerWormhole != null)
                    RegisterBodyInfo(partner, partnerWormhole);

                string key = PairKey(body.name, partner.name);
                if (!seen.Add(key))
                    continue;

                if (!WormholeSettings.Current.Enabled)
                    continue;

                Pairs.Add(new WormholePair
                {
                    BodyA = body,
                    BodyB = partner
                });
            }

            if (WormholeSettings.DebugLogging)
                Log.DebugLog($"Registered {Pairs.Count} wormhole comm pair(s).");
        }

        private static void RegisterBodyInfo(CelestialBody body, WormholeComponent wormhole)
        {
            if (body == null || wormhole == null || BodyInfoByBody.ContainsKey(body))
                return;

            BodyInfoByBody[body] = new WormholeBodyInfo
            {
                Body = body,
                InfluenceAltitude = wormhole.influenceAltitude,
                JumpMinAltitude = wormhole.jumpMinAltitude,
                JumpMaxAltitude = wormhole.jumpMaxAltitude
            };
        }

        private static string PairKey(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
    }
}
