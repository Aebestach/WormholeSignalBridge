using System;
using System.Collections.Generic;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class MouthTargetEntry
    {
        internal CelestialBody Body;
        internal string Label = string.Empty;
        internal bool Selectable;
        internal string StatusHint = string.Empty;
        internal double LinkDataRate;
        internal bool CurrentlyAimed;
    }

    internal static class WormholeMouthTargetEvaluator
    {
        internal static bool AntennaCanOpenSelector(ModuleRealAntenna module)
        {
            if (module?.RAAntenna == null || !module.RAAntenna.CanTarget)
                return false;

            if (!module._enabled || module.Condition != AntennaCondition.Enabled)
                return false;

            return HasAnyDiscoveredMouth();
        }

        internal static bool HasAnyDiscoveredMouth()
        {
            foreach (CelestialBody _ in DiscoveredMouthRegistry.DiscoveredBodies())
                return true;

            return false;
        }

        internal static List<MouthTargetEntry> BuildEntries(ModuleRealAntenna module)
        {
            var entries = new List<MouthTargetEntry>();
            if (module?.RAAntenna == null)
                return entries;

            WormholeLinkSettings settings = WormholeSettings.Current;
            Vessel vessel = module.vessel;
            RACommNode vesselNode = GetVesselNode(vessel);

            foreach (CelestialBody body in DiscoveredMouthRegistry.DiscoveredBodies())
            {
                var entry = new MouthTargetEntry
                {
                    Body = body,
                    Label = FormatMouthLabel(body),
                    CurrentlyAimed = WormholeMouthPointing.PointsAtMouth(module.RAAntenna, vessel, body, settings)
                };

                EvaluateEntry(vessel, vesselNode, body, settings, module.RAAntenna, entry);
                entries.Add(entry);
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
            return entries;
        }

        private static void EvaluateEntry(
            Vessel vessel,
            RACommNode vesselNode,
            CelestialBody body,
            WormholeLinkSettings settings,
            RealAntenna antenna,
            MouthTargetEntry entry)
        {
            if (!DiscoveredMouthRegistry.IsDiscovered(body))
            {
                entry.Selectable = false;
                entry.StatusHint = Local.MouthEntryNotDiscovered;
                return;
            }

            if (vessel == null)
            {
                entry.Selectable = false;
                entry.StatusHint = Local.MouthEntryNoVessel;
                return;
            }

            entry.Selectable = true;

            if (vessel.mainBody != body)
            {
                entry.StatusHint = Local.MouthEntryAimOffBody(CelestialBodyDisplay.ForMessage(body));
                return;
            }

            if (vesselNode == null || !vesselNode.CanComm())
            {
                entry.StatusHint = Local.MouthEntryNoComm;
                return;
            }

            if (!WormholeLinkCalculator.TryEvaluateOrbit(vessel, body, settings, out string orbitReason))
            {
                entry.StatusHint = Local.MouthEntryOrbitBlocked(orbitReason);
                return;
            }

            if (WormholeMouthNodeManager.ActiveNodes.TryGetValue(body, out WormholeMouthNode mouthNode))
                entry.LinkDataRate = WormholeLinkCalculator.EstimateMouthLinkDataRate(vesselNode, mouthNode.Node, antenna);

            entry.Selectable = true;
            if (entry.LinkDataRate > 0)
                entry.StatusHint = Local.MouthEntryLinkRate(entry.LinkDataRate);
            else if (entry.CurrentlyAimed)
                entry.StatusHint = Local.MouthEntryNoLink;
            else
                entry.StatusHint = Local.MouthEntryReady;
        }

        internal static RACommNode GetVesselNode(Vessel vessel) =>
            vessel?.Connection is RACommNetVessel commNet && commNet.Comm is RACommNode node ? node : null;

        internal static string FormatMouthLabel(CelestialBody body) =>
            body == null ? "?" : Local.MouthEntryLabel(CelestialBodyDisplay.ForMessage(body));
    }
}
