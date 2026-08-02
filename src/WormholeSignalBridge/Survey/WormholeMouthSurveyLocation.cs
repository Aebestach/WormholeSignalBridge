using UnityEngine;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthSurveyLocation
    {
        internal static bool IsWormholeBody(CelestialBody body) =>
            body != null && WormholeRegistry.TryGetBodyInfo(body, out _);

        internal static WormholeMouthSurveyRequirementState EvaluateRequirements(Vessel vessel)
        {
            var state = new WormholeMouthSurveyRequirementState
            {
                MaxAltitude = true,
                MaxAltitudeApplies = false
            };

            if (vessel?.mainBody == null)
                return state;

            CelestialBody body = vessel.mainBody;
            state.WormholeBody = IsWormholeBody(body);
            state.InOrbit = vessel.situation == Vessel.Situations.ORBITING ||
                            vessel.situation == Vessel.Situations.SUB_ORBITAL ||
                            vessel.situation == Vessel.Situations.ESCAPING;

            if (!state.WormholeBody)
            {
                state.MinAltitude = false;
                state.MaxAltitude = false;
                return state;
            }

            state.MinAltitudeRequired = WormholeRegistry.GetMinimumRelayAltitude(body);
            if (state.MinAltitudeRequired > 0)
                state.MinAltitude = vessel.altitude >= state.MinAltitudeRequired;
            else
                state.MinAltitude = true;

            WormholeLinkSettings settings = WormholeSettings.Current;
            if (settings.AdvancedOrbitConstraints && settings.MaxMouthAltitude > 0)
            {
                state.MaxAltitudeApplies = true;
                state.MaxAltitudeAllowed = settings.MaxMouthAltitude;
                state.MaxAltitude = vessel.altitude <= settings.MaxMouthAltitude;
            }

            return state;
        }

        internal static bool IsValidSurveyLocation(Vessel vessel) =>
            EvaluateRequirements(vessel).AllMet;

        internal static string DescribeIssue(Vessel vessel)
        {
            WormholeMouthSurveyRequirementState state = EvaluateRequirements(vessel);

            if (vessel?.mainBody == null)
                return Local.WormholeSurveyIssueNoBody;

            if (!state.WormholeBody)
                return Local.WormholeSurveyIssueNotWormholeBody;

            if (!state.MinAltitude && state.MinAltitudeRequired > 0)
                return Local.WormholeSurveyIssueTooLow.Format(state.MinAltitudeRequired);

            if (state.MaxAltitudeApplies && !state.MaxAltitude)
                return Local.WormholeSurveyIssueTooHigh;

            if (!state.InOrbit)
                return Local.WormholeSurveyIssueNotInOrbit;

            return string.Empty;
        }
    }
}
