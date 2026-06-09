namespace WormholeSignalBridge
{
    internal struct WormholeMouthSurveyRequirementState
    {
        internal bool WormholeBody;
        internal bool InOrbit;
        internal bool MinAltitude;
        internal bool MaxAltitude;
        internal bool MaxAltitudeApplies;
        internal double MinAltitudeRequired;
        internal double MaxAltitudeAllowed;

        internal bool AllMet =>
            WormholeBody &&
            InOrbit &&
            MinAltitude &&
            MaxAltitude;
    }

    internal static class SurveyPawMarks
    {
        private const string Ok = "<color=#3cba54>OK</color>";
        private const string Pending = "<color=#888888>-</color>";

        internal static string For(bool met) => met ? Ok : Pending;
    }
}
