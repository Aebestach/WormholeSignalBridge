namespace WormholeSignalBridge
{
    /// <summary>
    /// Kerbalism Experiment ignores WSB survey rules; mirror stock gate via ToggleEvent and Toggle().
    /// </summary>
    internal static class KerbalismWormholeSurveyGate
    {
        internal static void Enforce(PartModule experiment, Vessel vessel, int status)
        {
            if (experiment == null || vessel == null || !KerbalismExperimentBridge.Available)
                return;

            bool valid = WormholeMouthSurveyLocation.IsValidSurveyLocation(vessel);
            string issue = valid ? string.Empty : WormholeMouthSurveyLocation.DescribeIssue(vessel);

            if (!valid && KerbalismExperimentBridge.IsCollecting(status))
                KerbalismExperimentBridge.StopExperiment(experiment);

            if (!valid && !KerbalismExperimentBridge.IsCollecting(status) && !KerbalismExperimentBridge.IsWaiting(status))
            {
                KerbalismExperimentBridge.SetIssue(experiment, issue);
                KerbalismExperimentBridge.SetStartEventsEnabled(experiment, false);
                return;
            }

            if (valid && !KerbalismExperimentBridge.IsCollecting(status) && !KerbalismExperimentBridge.IsWaiting(status))
                KerbalismExperimentBridge.SetIssue(experiment, string.Empty);
        }
    }
}
