using System;
using System.Reflection;
using HarmonyLib;
using RealAntennas;

namespace WormholeSignalBridge.HarmonyPatches
{
    [HarmonyPatch]
    internal static class PrecomputeCompletePatch
    {
        internal static MethodBase TargetMethod()
        {
            Type precomputeType = typeof(RACommNetwork).Assembly.GetType("RealAntennas.Precompute.Precompute");
            if (precomputeType == null)
                throw new InvalidOperationException("Could not find RealAntennas.Precompute.Precompute");

            return AccessTools.Method(precomputeType, "Complete", new[] { typeof(RACommNetwork) });
        }

        internal static void Postfix(RACommNetwork RACN)
        {
            WormholeLinkBuilder.InjectLinks(RACN);
        }
    }
}
