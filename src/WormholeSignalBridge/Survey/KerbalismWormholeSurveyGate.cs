namespace WormholeSignalBridge
{
    /// <summary>
    /// Kerbalism Experiment ignores WSB survey rules; mirror stock gate via ToggleEvent and Toggle().
    /// </summary>
    internal static class KerbalismWormholeSurveyGate
    {
        internal static void Enforce(PartModule experiment, Vessel vessel, int status)
        {
            if (experiment == null || vessel == null)
                return;

            bool valid = WormholeMouthSurveyLocation.IsValidSurveyLocation(vessel);
            string issue = valid ? string.Empty : WormholeMouthSurveyLocation.DescribeIssue(vessel);

            if (!valid && KerbalismExperimentBridge.IsCollecting(status))
                KerbalismExperimentBridge.StopExperiment(experiment);

            if (KerbalismExperimentBridge.IsWaiting(status))
                return;

            bool canStart = valid;
            KerbalismExperimentBridge.SetIssue(experiment, canStart ? string.Empty : issue);
            KerbalismExperimentBridge.SetStartEventsEnabled(experiment, canStart);
        }
    }
}
