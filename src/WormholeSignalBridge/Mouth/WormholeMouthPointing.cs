using System;
using RealAntennas;
using RealAntennas.Targeting;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthPointing
    {
        private const float CoordinateToleranceDeg = 0.001f;
        private const float AltitudeToleranceMeters = 1f;

        internal static bool PointsAtMouth(RealAntenna antenna, CelestialBody body, WormholeLinkSettings settings)
        {
            return PointsAtMouth(antenna, null, body, settings);
        }

        internal static bool PointsAtMouth(RealAntenna antenna, Vessel vessel, CelestialBody body, WormholeLinkSettings settings)
        {
            if (antenna == null || body == null || antenna.Shape == AntennaShape.Omni)
                return false;

            if (TargetsMouthLatLonAlt(antenna, body, settings))
                return PhysicalPointsAtMouth(antenna, body, settings);

            return antenna.Target != null && PhysicalPointsAtMouth(antenna, body, settings);
        }

        internal static string Describe(RealAntenna antenna, CelestialBody body, WormholeLinkSettings settings)
        {
            return Describe(antenna, null, body, settings);
        }

        internal static string Describe(RealAntenna antenna, Vessel vessel, CelestialBody body, WormholeLinkSettings settings)
        {
            if (antenna == null)
                return "null antenna";

            if (body == null)
                return "null body";

            if (antenna.Shape == AntennaShape.Omni)
                return "omni excluded from wormhole links";

            Vector3 mouth = MouthWorldPosition(body, settings);
            if (TargetsMouthLatLonAlt(antenna, body, settings))
            {
                float loss = Physics.PointingLoss(antenna, mouth);
                if (loss < Physics.MaxPointingLoss)
                    return $"configured BodyLatLonAlt target on {body.name}, loss {loss:F1} dB";

                return $"configured BodyLatLonAlt target on {body.name}, but pointing loss {loss:F1} dB >= max {Physics.MaxPointingLoss:F1} dB";
            }

            if (ModuleWormholeMouthAiming.IsRememberedMouthTarget(antenna, vessel, body))
                return $"remembered WSB Mouth target on {body.name}, but current RA target is {DescribeTarget(antenna)}";

            if (antenna.Target == null)
                return $"no current RA target; expected {FormatCoordinates(WormholeMouthPlacement.GetMouthLatLonAlt(body, settings))}";

            if (PhysicalPointsAtMouth(antenna, body, settings))
            {
                float loss = Physics.PointingLoss(antenna, mouth);
                return $"pointing at {body.name} mouth ok, loss {loss:F1} dB";
            }

            float failLoss = Physics.PointingLoss(antenna, mouth);
            return $"target {DescribeTarget(antenna)}, expected {FormatCoordinates(WormholeMouthPlacement.GetMouthLatLonAlt(body, settings))}; " +
                   $"pointing loss {failLoss:F1} dB >= max {Physics.MaxPointingLoss:F1} dB, not aimed at {body.name} mouth";
        }

        private static bool PhysicalPointsAtMouth(RealAntenna antenna, CelestialBody body, WormholeLinkSettings settings) =>
            Physics.PointingLoss(antenna, MouthWorldPosition(body, settings)) < Physics.MaxPointingLoss;

        private static Vector3 MouthWorldPosition(CelestialBody body, WormholeLinkSettings settings) =>
            (Vector3)WormholeMouthPlacement.GetMouthWorldPosition(body, settings);

        internal static bool TargetsMouthLatLonAlt(RealAntenna antenna, CelestialBody body, WormholeLinkSettings settings)
        {
            if (!(antenna.Target is AntennaTargetLatLonAlt latLon))
                return false;

            if (!TargetsBody(latLon, body))
                return false;

            Vector3 mouth = WormholeMouthPlacement.GetMouthLatLonAlt(body, settings);
            return Math.Abs(latLon.latLonAlt.x - mouth.x) <= CoordinateToleranceDeg &&
                   Math.Abs(Mathf.DeltaAngle(latLon.latLonAlt.y, mouth.y)) <= CoordinateToleranceDeg &&
                   Math.Abs(latLon.latLonAlt.z - mouth.z) <= AltitudeToleranceMeters;
        }

        private static bool TargetsBody(AntennaTargetLatLonAlt target, CelestialBody body) =>
            target != null &&
            body != null &&
            (target.body == body || string.Equals(target.bodyName, body.name, StringComparison.Ordinal));

        private static string DescribeTarget(RealAntenna antenna)
        {
            if (antenna?.Target is AntennaTargetLatLonAlt latLon)
                return $"{latLon.bodyName}:({latLon.latLonAlt.x:F4}:{latLon.latLonAlt.y:F4}:{latLon.latLonAlt.z:F1})";

            return antenna?.Target == null ? "none" : antenna.Target.ToString();
        }

        private static string FormatCoordinates(Vector3 latLonAlt) =>
            $"({latLonAlt.x:F4}:{latLonAlt.y:F4}:{latLonAlt.z:F1})";
    }
}
