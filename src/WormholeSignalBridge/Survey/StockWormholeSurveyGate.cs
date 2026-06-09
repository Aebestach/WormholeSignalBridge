namespace WormholeSignalBridge
{
    /// <summary>
    /// Stock ModuleScienceExperiment does not know WSB survey rules; gate deploy/reset here.
    /// </summary>
    internal static class StockWormholeSurveyGate
    {
        private const string DeployEventName = "DeployExperiment";

        internal static void Enforce(ModuleScienceExperiment experiment, Vessel vessel)
        {
            if (experiment == null || vessel == null || KerbalismExperimentBridge.Available)
                return;

            bool valid = WormholeMouthSurveyLocation.IsValidSurveyLocation(vessel);

            if (experiment.Deployed && !valid)
                experiment.ResetExperiment();

            if (experiment.Events == null || !experiment.Events.Contains(DeployEventName))
                return;

            BaseEvent deploy = experiment.Events[DeployEventName];
            bool canDeploy = valid && !experiment.Inoperable;
            deploy.active = canDeploy;
            deploy.guiActive = canDeploy;
            deploy.guiActiveUncommand = canDeploy;
            deploy.guiActiveUnfocused = canDeploy;
        }
    }
}
