using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class DiscoveredMouthRecord
    {
        internal string BodyName;
        internal double DiscoveredUt;
        internal double Latitude;
        internal double Longitude;
        internal double Altitude;
        internal bool HasFixedCoordinates;
    }

    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames | ScenarioCreationOptions.AddToExistingGames,
        GameScenes.FLIGHT,
        GameScenes.TRACKSTATION,
        GameScenes.SPACECENTER)]
    public sealed class DiscoveredMouthRegistry : ScenarioModule
    {
        internal const string ExperimentId = "WSB_wormholeMouthSurvey";
        internal const double FirstDiscoveryFundsBonus = 75000;

        private static DiscoveredMouthRegistry instance;

        [KSPField(isPersistant = true)]
        public string discoveryData = string.Empty;

        private readonly Dictionary<string, DiscoveredMouthRecord> discoveries = new Dictionary<string, DiscoveredMouthRecord>(StringComparer.Ordinal);

        internal static DiscoveredMouthRegistry Instance => instance;

        internal static IReadOnlyCollection<DiscoveredMouthRecord> AllDiscoveries =>
            instance == null
                ? (IReadOnlyCollection<DiscoveredMouthRecord>)Array.Empty<DiscoveredMouthRecord>()
                : instance.discoveries.Values.ToList();

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            instance = this;
            discoveries.Clear();

            if (string.IsNullOrEmpty(discoveryData))
                return;

            foreach (string entry in discoveryData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split('|');
                if (parts.Length < 1 || string.IsNullOrEmpty(parts[0]))
                    continue;

                var record = new DiscoveredMouthRecord
                {
                    BodyName = parts[0],
                    DiscoveredUt = parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ut) ? ut : 0
                };

                if (parts.Length >= 5 &&
                    double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double lon) &&
                    double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double alt))
                {
                    record.Latitude = lat;
                    record.Longitude = lon;
                    record.Altitude = alt;
                    record.HasFixedCoordinates = true;
                }
                else
                    BackfillFixedCoordinates(record);

                discoveries[record.BodyName] = record;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            discoveryData = string.Join(";", discoveries.Values.Select(FormatRecord));
            base.OnSave(node);
        }

        internal static bool IsDiscovered(string bodyName) =>
            !string.IsNullOrEmpty(bodyName) && instance != null && instance.discoveries.ContainsKey(bodyName);

        internal static bool IsDiscovered(CelestialBody body) =>
            body != null && IsDiscovered(body.name);

        internal static bool TryGetCachedLatLonAlt(CelestialBody body, out Vector3 latLonAlt)
        {
            latLonAlt = Vector3.zero;
            if (body == null || instance == null || !instance.discoveries.TryGetValue(body.name, out DiscoveredMouthRecord record))
                return false;

            if (!record.HasFixedCoordinates)
                return false;

            latLonAlt = new Vector3((float)record.Latitude, (float)record.Longitude, (float)record.Altitude);
            return true;
        }

        internal static bool TryDiscover(CelestialBody body, out bool firstDiscovery)
        {
            firstDiscovery = false;
            if (body == null || instance == null)
                return false;

            if (instance.discoveries.ContainsKey(body.name))
            {
                firstDiscovery = false;
                return false;
            }

            Vector3 latLonAlt = WormholeMouthPlacement.ComputeProvisionalLatLonAlt(body, WormholeSettings.Current);
            instance.discoveries[body.name] = new DiscoveredMouthRecord
            {
                BodyName = body.name,
                DiscoveredUt = Planetarium.GetUniversalTime(),
                Latitude = latLonAlt.x,
                Longitude = latLonAlt.y,
                Altitude = latLonAlt.z,
                HasFixedCoordinates = true
            };

            firstDiscovery = true;
            Log.Info($"Discovered wormhole mouth for {body.name} at {latLonAlt.x:F2}, {latLonAlt.y:F2}, {latLonAlt.z:F0} m.");
            return true;
        }

        internal static IEnumerable<CelestialBody> DiscoveredBodies()
        {
            if (instance == null)
                yield break;

            foreach (DiscoveredMouthRecord record in instance.discoveries.Values)
            {
                CelestialBody body = FlightGlobals.GetBodyByName(record.BodyName);
                if (body != null)
                    yield return body;
            }
        }

        private static string FormatRecord(DiscoveredMouthRecord record)
        {
            if (record.HasFixedCoordinates)
            {
                return string.Join("|",
                    record.BodyName,
                    record.DiscoveredUt.ToString("F3", CultureInfo.InvariantCulture),
                    record.Latitude.ToString("R", CultureInfo.InvariantCulture),
                    record.Longitude.ToString("R", CultureInfo.InvariantCulture),
                    record.Altitude.ToString("R", CultureInfo.InvariantCulture));
            }

            return $"{record.BodyName}|{record.DiscoveredUt.ToString("F3", CultureInfo.InvariantCulture)}";
        }

        private static void BackfillFixedCoordinates(DiscoveredMouthRecord record)
        {
            CelestialBody body = FlightGlobals.GetBodyByName(record.BodyName);
            if (body == null)
                return;

            Vector3 latLonAlt = WormholeMouthPlacement.ComputeProvisionalLatLonAlt(body, WormholeSettings.Current);
            record.Latitude = latLonAlt.x;
            record.Longitude = latLonAlt.y;
            record.Altitude = latLonAlt.z;
            record.HasFixedCoordinates = true;
        }
    }
}
