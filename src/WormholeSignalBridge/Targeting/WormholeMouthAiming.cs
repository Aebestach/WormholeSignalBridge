using RealAntennas;
using RealAntennas.Targeting;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthAiming
    {
        internal static void ApplyMouthTarget(RealAntenna antenna, CelestialBody body)
        {
            Vector3 latLonAlt = WormholeMouthPlacement.GetMouthLatLonAlt(body, WormholeSettings.Current);
            var node = new ConfigNode(AntennaTarget.nodeName);
            node.AddValue("name", AntennaTarget.TargetMode.BodyLatLonAlt.ToString());
            node.AddValue("bodyName", body.name);
            node.AddValue("latLonAlt", latLonAlt);
            antenna.Target = AntennaTarget.LoadFromConfig(node, antenna);
            ModuleWormholeMouthAiming.RememberMouthTarget(antenna, body);
        }

        internal static string FormatMouthCoordinates(CelestialBody body)
        {
            Vector3 latLonAlt = WormholeMouthPlacement.GetMouthLatLonAlt(body, WormholeSettings.Current);
            return $"{CelestialBodyDisplay.ForMessage(body)}: {latLonAlt.x:F2}°, {latLonAlt.y:F2}°, {latLonAlt.z:F0} m";
        }
    }
}
