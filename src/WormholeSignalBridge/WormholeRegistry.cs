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

    internal static class WormholeRegistry
    {
        private static readonly List<WormholePair> Pairs = new List<WormholePair>();

        internal static IReadOnlyList<WormholePair> ActivePairs => Pairs;

        internal static void Refresh()
        {
            Pairs.Clear();
            var bodies = PSystemManager.Instance?.localBodies;
            if (bodies == null || bodies.Count == 0)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CelestialBody body in bodies)
            {
                WormholeComponent wormhole = body.GetComponent<WormholeComponent>();
                if (wormhole == null || string.IsNullOrEmpty(wormhole.partnerBody))
                    continue;

                CelestialBody partner = bodies.Find(b => b.name == wormhole.partnerBody);
                if (partner == null)
                {
                    Log.Warning($"Wormhole body {body.name} references missing partner {wormhole.partnerBody}.");
                    continue;
                }

                string key = PairKey(body.name, partner.name);
                if (!seen.Add(key))
                    continue;

                if (!WormholeSettings.Defaults.Enabled)
                    continue;

                Pairs.Add(new WormholePair
                {
                    BodyA = body,
                    BodyB = partner
                });
            }

            if (WormholeSettings.DebugLogging)
                Log.Info($"Registered {Pairs.Count} wormhole comm pair(s).");
        }

        private static string PairKey(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
    }
}
