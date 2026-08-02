using KSP.Localization;

namespace WormholeSignalBridge
{
    /// <summary>
    /// Cached PAW labels for survey altitude bounds; refreshes when mainBody or limits change.
    /// </summary>
    internal sealed class WormholeMouthSurveyAltitudeLabels
    {
        private CelestialBody cachedBody;
        private double cachedMinMeters;
        private double cachedMaxMeters;
        private bool cachedMaxApplies;

        internal string MinLabel { get; private set; } = string.Empty;
        internal string MaxLabel { get; private set; } = string.Empty;

        internal void RefreshFor(Vessel vessel)
        {
            CelestialBody body = vessel?.mainBody;
            WormholeLinkSettings settings = WormholeSettings.Current;

            bool wormhole = body != null && WormholeMouthSurveyLocation.IsWormholeBody(body);
            double minMeters = wormhole ? WormholeRegistry.GetMinimumRelayAltitude(body) : 0;
            bool maxApplies = wormhole && settings.AdvancedOrbitConstraints && settings.MaxMouthAltitude > 0;
            double maxMeters = maxApplies ? settings.MaxMouthAltitude : 0;

            if (body == cachedBody &&
                minMeters == cachedMinMeters &&
                maxMeters == cachedMaxMeters &&
                maxApplies == cachedMaxApplies)
                return;

            cachedBody = body;
            cachedMinMeters = minMeters;
            cachedMaxMeters = maxMeters;
            cachedMaxApplies = maxApplies;

            MinLabel = wormhole && minMeters > 0
                ? Local.SurveyReqMinAltitudeAt(AltitudeDisplay.Format(minMeters))
                : Local.SurveyReqMinAltitudeGeneric;

            if (wormhole && maxApplies)
                MaxLabel = Local.SurveyReqMaxAltitudeAt(AltitudeDisplay.Format(maxMeters));
            else
                MaxLabel = Local.SurveyReqMaxAltitudeGeneric;
        }

        internal void Reset()
        {
            cachedBody = null;
            cachedMinMeters = 0;
            cachedMaxMeters = 0;
            cachedMaxApplies = false;
            MinLabel = string.Empty;
            MaxLabel = string.Empty;
        }
    }

    internal static class AltitudeDisplay
    {
        internal static string Format(double meters) =>
            meters >= 1_000_000
                ? $"{meters / 1_000_000:0.##} Mm"
                : meters >= 1_000
                    ? $"{meters / 1_000:0.#} km"
                    : $"{meters:0} m";
    }
}
