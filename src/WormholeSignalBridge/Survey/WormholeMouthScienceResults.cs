using System.Collections.Generic;

namespace WormholeSignalBridge
{
    /// <summary>
    /// Injects per-wormhole science report text at runtime so narratives use live body and partner names.
    /// </summary>
    internal static class WormholeMouthScienceResults
    {
        internal static void RegisterAll()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(DiscoveredMouthRegistry.ExperimentId);
            if (experiment?.Results == null)
                return;

            Dictionary<string, string> results = experiment.Results;
            results["default"] = WormholeMouthSurveyNarrative.Default();

            foreach (CelestialBody body in WormholeRegistry.AllWormholeBodies)
            {
                if (body == null)
                    continue;

                string narrative = WormholeMouthSurveyNarrative.ForBody(body);
                results[body.name + "InSpaceLow"] = narrative;
                results[body.name + "InSpaceHigh"] = narrative;
            }
        }
    }
}
