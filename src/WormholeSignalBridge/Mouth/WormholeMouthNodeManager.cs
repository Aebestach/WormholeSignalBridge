using System;
using System.Collections.Generic;
using RealAntennas;
using RealAntennas.Antenna;
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

        internal static void EnsureSynced(RACommNetwork network, WormholeLinkSettings settings)
        {
            if (network == null || !WormholeSettings.Current.Enabled)
            {
                ReleaseLocal(network);
                return;
            }

            var activeBodies = new HashSet<CelestialBody>();
            foreach (CelestialBody body in WormholeRegistry.AllWormholeBodies)
                activeBodies.Add(body);

            foreach (CelestialBody body in new List<CelestialBody>(Nodes.Keys))
            {
                if (!activeBodies.Contains(body))
                    ReleaseBody(network, body);
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
                        network.NotifyTopologyChanged();
                    }
                }

                UpdatePosition(mouth, settings);
            }
        }

        internal static void ReleaseLocal(RACommNetwork network)
        {
            foreach (CelestialBody body in new List<CelestialBody>(Nodes.Keys))
                ReleaseBody(network, body);
        }

        private static void ReleaseBody(RACommNetwork network, CelestialBody body)
        {
            if (!Nodes.TryGetValue(body, out WormholeMouthNode mouth))
                return;

            if (network != null && network.Nodes.Contains(mouth.Node))
                network.RemoveNode(mouth.Node);

            if (mouth.GameObject != null)
                UnityEngine.Object.Destroy(mouth.GameObject);

            Nodes.Remove(body);
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

            BuildMouthAntennas(node);
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

        private static void BuildMouthAntennas(RACommNode node)
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
                    TxPower = 60f,
                    antennaDiameter = 30f,
                    SymbolRate = 1e9
                };
                antenna.Gain = Physics.GainFromDishDiamater(antenna.antennaDiameter, band.Frequency, tech.ReflectorEfficiency);
                node.RAAntennaList.Add(antenna);
            }
        }
    }
}
