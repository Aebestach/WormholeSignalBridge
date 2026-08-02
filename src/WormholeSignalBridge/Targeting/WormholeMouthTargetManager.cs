using System.Collections.Generic;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthTargetManager
    {
        private static readonly Dictionary<RealAntenna, GameObject> GuiObjects = new Dictionary<RealAntenna, GameObject>();

        internal static WormholeMouthTargetGUI Acquire(ModuleRealAntenna module)
        {
            RealAntenna antenna = module.RAAntenna;
            if (!GuiObjects.TryGetValue(antenna, out GameObject go) || go == null)
            {
                go = new GameObject($"{antenna.ParentNode?.name}:{antenna.Name}:WSBMouthGUI");
                var gui = go.AddComponent<WormholeMouthTargetGUI>();
                gui.name = go.name;
                gui.Module = module;
                GuiObjects[antenna] = go;
            }

            var existing = go.GetComponent<WormholeMouthTargetGUI>();
            existing.Module = module;
            return existing;
        }

        internal static void Release(RealAntenna antenna, WormholeMouthTargetGUI _)
        {
            if (antenna == null || !GuiObjects.TryGetValue(antenna, out GameObject go))
                return;

            GuiObjects.Remove(antenna);
            if (go != null)
                Object.Destroy(go);
        }
    }
}
