using System;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthPlacement
    {
        internal static Vector3d GetMouthWorldPosition(CelestialBody body, WormholeLinkSettings settings)
        {
            if (body == null)
                return Vector3d.zero;

            Vector3 latLonAlt = GetMouthLatLonAlt(body, settings);
            return body.GetWorldSurfacePosition(latLonAlt.x, latLonAlt.y, latLonAlt.z);
        }

        /// <summary>
        /// Cached Body Lat/Lon/Alt after survey; provisional parent-facing estimate before discovery.
        /// </summary>
        internal static Vector3 GetMouthLatLonAlt(CelestialBody body, WormholeLinkSettings settings)
        {
            if (body == null)
                return Vector3.zero;

            if (DiscoveredMouthRegistry.TryGetCachedLatLonAlt(body, out Vector3 cached))
                return cached;

            return ComputeProvisionalLatLonAlt(body, settings);
        }

        /// <summary>
        /// Parent-facing side at call time; used once when a mouth is first surveyed.
        /// </summary>
        internal static Vector3 ComputeProvisionalLatLonAlt(CelestialBody body, WormholeLinkSettings settings)
        {
            if (body == null)
                return Vector3.zero;

            double altitude = WormholeRegistry.GetMouthProxyAltitude(body, settings);
            Vector3d radial = GetParentFacingRadial(body);
            Vector3d position = body.position + (radial * (body.Radius + altitude));
            return new Vector3(
                (float)body.GetLatitude(position),
                (float)body.GetLongitude(position),
                (float)altitude);
        }

        private static Vector3d GetParentFacingRadial(CelestialBody body)
        {
            CelestialBody parent = body.referenceBody;
            if (parent != null)
            {
                Vector3d radial = parent.position - body.position;
                if (radial.sqrMagnitude > 1)
                    return radial.normalized;
            }

            return body.transform.up;
        }
    }
}
