using System.Collections.Generic;
using CommNet;
using RealAntennas;
using RealAntennas.Antenna;
using RealAntennas.Network;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class WormholeMouthNode
    {
        internal CelestialBody Body;
        internal GameObject GameObject;
        internal RACommNode Node;
    }

    /// <summary>
    /// Invisible RA ground-station-style nodes at each wormhole mouth so RA Precompute/Jobs
    /// produce vessel ↔ mouth link budgets. Players aim via RA BodyLatLonAlt coordinates.
    /// </summary>
    internal static class WormholeMouthNodeManager
    {
        private static readonly Dictionary<CelestialBody, WormholeMouthNode> Nodes = new Dictionary<CelestialBody, WormholeMouthNode>();

        internal static IReadOnlyDictionary<CelestialBody, WormholeMouthNode> ActiveNodes => Nodes;

        internal static bool EnsureSynced(RACommNetwork network, WormholeLinkSettings settings)
        {
            if (network == null || !WormholeSettings.Current.Enabled)
            {
                return ReleaseLocal(network);
            }

            bool topologyChanged = false;
            var activeBodies = new HashSet<CelestialBody>();
            foreach (CelestialBody body in WormholeRegistry.AllWormholeBodies)
                activeBodies.Add(body);

            foreach (CelestialBody body in new List<CelestialBody>(Nodes.Keys))
            {
                if (!activeBodies.Contains(body))
                    topologyChanged |= ReleaseBody(network, body);
            }

            foreach (CelestialBody body in activeBodies)
            {
                if (!Nodes.TryGetValue(body, out WormholeMouthNode mouth))
                {
                    mouth = Create(body, settings);
                    Nodes.Add(body, mouth);
                    if (!network.Nodes.Contains(mouth.Node))
                    {
                        network.Add(mouth.Node);
                        topologyChanged = true;
                        InvalidateCommCache();
                    }
                }

                UpdatePosition(mouth, settings);
            }

            return topologyChanged;
        }

        internal static bool ReleaseLocal(RACommNetwork network, bool suppressTopologyRefresh = false)
        {
            bool topologyChanged = false;
            foreach (CelestialBody body in new List<CelestialBody>(Nodes.Keys))
                topologyChanged |= ReleaseBody(network, body, suppressTopologyRefresh);

            return topologyChanged;
        }

        private static bool ReleaseBody(RACommNetwork network, CelestialBody body, bool suppressTopologyRefresh = false)
        {
            if (!Nodes.TryGetValue(body, out WormholeMouthNode mouth))
                return false;

            bool topologyChanged = false;
            if (network != null && network.Nodes.Contains(mouth.Node))
            {
                var links = new List<CommLink>();
                foreach (CommLink link in mouth.Node.Values)
                    links.Add(link);
                foreach (CommLink link in links)
                    network.DoDisconnect(link.start, link.end);
                network.Nodes.Remove(mouth.Node);
                topologyChanged = true;
                if (!suppressTopologyRefresh)
                    InvalidateCommCache();
            }

            if (mouth.GameObject != null)
                UnityEngine.Object.Destroy(mouth.GameObject);

            Nodes.Remove(body);
            return topologyChanged;
        }

        private static void InvalidateCommCache()
        {
            (CommNetScenario.Instance as RACommNetScenario)?.Network?.InvalidateCache();
        }

        private static WormholeMouthNode Create(CelestialBody body, WormholeLinkSettings settings)
        {
            var go = new GameObject($"WSB {body.name} Mouth");
            var node = new RACommNode(go.transform)
            {
                name = $"{body.name}Mouth",
                displayName = $"{body.name} Mouth",
                ParentBody = body,
                ParentVessel = null,
                RAAntennaList = new List<RealAntenna>()
            };

            BuildMouthAntennas(node, settings);
            UpdatePosition(new WormholeMouthNode { Body = body, GameObject = go, Node = node }, settings);

            return new WormholeMouthNode
            {
                Body = body,
                GameObject = go,
                Node = node
            };
        }

        private static void UpdatePosition(WormholeMouthNode mouth, WormholeLinkSettings settings)
        {
            if (mouth?.Body == null || mouth.Node == null || mouth.GameObject == null)
                return;

            Vector3d position = WormholeMouthPlacement.GetMouthWorldPosition(mouth.Body, settings);
            mouth.GameObject.transform.position = position;
            mouth.Node.precisePosition = position;
        }

        private static void BuildMouthAntennas(RACommNode node, WormholeLinkSettings settings)
        {
            BandInfo.Get(BandInfo.DefaultBand);
            TechLevelInfo.GetTechLevel(0);
            TechLevelInfo tech = TechLevelInfo.GetTechLevel(TechLevelInfo.MaxTL);
            node.RAAntennaList.Clear();

            foreach (BandInfo band in BandInfo.All.Values)
            {
                var antenna = new RealAntennaDigital($"{node.displayName} {band.name}")
                {
                    ParentNode = node,
                    RFBand = band,
                    TechLevelInfo = tech,
                    TxPower = (float)settings.MouthProxyTxPower,
                    antennaDiameter = (float)settings.MouthProxyDishDiameter,
                    SymbolRate = settings.MouthProxySymbolRate
                };
                antenna.Gain = Physics.GainFromDishDiamater(antenna.antennaDiameter, band.Frequency, tech.ReflectorEfficiency);
                node.RAAntennaList.Add(antenna);
            }
        }
    }
}
