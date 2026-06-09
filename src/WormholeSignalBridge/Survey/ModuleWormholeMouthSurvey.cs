using System;
using UnityEngine;

namespace WormholeSignalBridge
{
    public sealed class ModuleWormholeMouthSurvey : PartModule
    {
        private const string PawGroup = "WormholeSignalBridge";

        private readonly WormholeMouthSurveyAltitudeLabels altitudeLabels = new WormholeMouthSurveyAltitudeLabels();

        private ModuleScienceExperiment stockExperiment;
        private int lastKerbalismStatus = -1;
        private bool lastStockHadData;
        private string surveyedBodyName = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyReqIntro", groupName = PawGroup, groupDisplayName = "#LOC_WSB_pawGroup")]
        public string surveyReqIntro = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyReq_wormholeBody", groupName = PawGroup)]
        public string surveyReqWormholeBody = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyReq_inOrbit", groupName = PawGroup)]
        public string surveyReqInOrbit = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyReq_minAltitude", groupName = PawGroup)]
        public string surveyReqMinAltitude = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyReq_maxAltitude", groupName = PawGroup)]
        public string surveyReqMaxAltitude = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_WSB_surveyOverall", groupName = PawGroup)]
        public string surveyOverall = string.Empty;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            stockExperiment = FindStockExperiment();
            UpdateSurveyStatus();
        }

        private void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            WormholeRegistry.Refresh();
            PollExperimentCompletion();
            UpdateSurveyStatus();
        }

        private void LateFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            EnforceKerbalismSurveyConstraints();
            EnforceStockSurveyConstraints();
        }

        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Run after Kerbalism Experiment.Update / stock ModuleScienceExperiment.updateModuleUI.
            EnforceKerbalismSurveyConstraints();
            EnforceStockSurveyConstraints();
        }

        private ModuleScienceExperiment FindStockExperiment()
        {
            foreach (ModuleScienceExperiment experiment in part.FindModulesImplementing<ModuleScienceExperiment>())
            {
                if (string.Equals(experiment.experimentID, DiscoveredMouthRegistry.ExperimentId, StringComparison.Ordinal))
                    return experiment;
            }

            return null;
        }

        private void EnforceKerbalismSurveyConstraints()
        {
            if (!KerbalismExperimentBridge.Available ||
                !KerbalismExperimentBridge.TryGetExperiment(part, out PartModule experiment, out int status))
                return;

            KerbalismWormholeSurveyGate.Enforce(experiment, vessel, status);
        }

        private void EnforceStockSurveyConstraints()
        {
            if (KerbalismExperimentBridge.Available)
                return;

            if (stockExperiment == null)
                stockExperiment = FindStockExperiment();

            StockWormholeSurveyGate.Enforce(stockExperiment, vessel);
        }

        private void PollExperimentCompletion()
        {
            CelestialBody body = vessel?.mainBody;
            if (body == null || !WormholeMouthSurveyLocation.IsWormholeBody(body))
                return;

            if (KerbalismExperimentBridge.Available &&
                KerbalismExperimentBridge.TryGetExperiment(part, out _, out int status))
            {
                if (lastKerbalismStatus >= 0 &&
                    !KerbalismExperimentBridge.IsWaiting(lastKerbalismStatus) &&
                    KerbalismExperimentBridge.IsWaiting(status))
                    CompleteSurvey(body);

                lastKerbalismStatus = status;
                return;
            }

            if (stockExperiment == null)
                stockExperiment = FindStockExperiment();

            if (stockExperiment == null)
                return;

            bool hasData = StockExperimentHasData(stockExperiment);
            if (!lastStockHadData && hasData)
                CompleteSurvey(body);

            lastStockHadData = hasData;
        }

        private static bool StockExperimentHasData(ModuleScienceExperiment experiment)
        {
            if (experiment == null)
                return false;

            if (experiment is IScienceDataContainer container)
            {
                ScienceData[] data = container.GetData();
                return data != null && data.Length > 0;
            }

            return false;
        }

        private void CompleteSurvey(CelestialBody body)
        {
            if (body == null)
                return;

            if (!WormholeMouthSurveyLocation.IsValidSurveyLocation(vessel))
                return;

            if (string.Equals(surveyedBodyName, body.name, StringComparison.Ordinal))
                return;

            surveyedBodyName = body.name;
            if (!DiscoveredMouthRegistry.TryDiscover(body, out bool firstDiscovery))
                return;

            WormholeMouthDiscoveryNotifier.Notify(body, firstDiscovery);
        }

        private void UpdateSurveyStatus()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                ClearSurveyFields();
                return;
            }

            WormholeMouthSurveyRequirementState state = WormholeMouthSurveyLocation.EvaluateRequirements(vessel);

            altitudeLabels.RefreshFor(vessel);
            SetFieldGuiName(nameof(surveyReqMinAltitude), altitudeLabels.MinLabel);
            SetFieldGuiName(nameof(surveyReqMaxAltitude), altitudeLabels.MaxLabel);

            surveyReqIntro = Local.SurveyReqIntro;
            surveyReqWormholeBody = SurveyPawMarks.For(state.WormholeBody);
            surveyReqInOrbit = SurveyPawMarks.For(state.InOrbit);
            surveyReqMinAltitude = SurveyPawMarks.For(state.MinAltitude);
            surveyReqMaxAltitude = FormatMaxAltitudeMark(state);
            surveyOverall = state.AllMet ? Local.SurveyOverallReady : Local.SurveyOverallNotReady;
        }

        private static string FormatMaxAltitudeMark(WormholeMouthSurveyRequirementState state)
        {
            if (!state.MaxAltitudeApplies)
                return Local.SurveyReqMaxAltitudeNa;

            return SurveyPawMarks.For(state.MaxAltitude);
        }

        private void SetFieldGuiName(string fieldName, string guiName)
        {
            if (string.IsNullOrEmpty(guiName) || Fields == null)
                return;

            foreach (BaseField field in Fields)
            {
                if (field.name != fieldName)
                    continue;

                field.guiName = guiName;
                return;
            }
        }

        private void ClearSurveyFields()
        {
            altitudeLabels.Reset();
            surveyReqIntro = string.Empty;
            surveyReqWormholeBody = string.Empty;
            surveyReqInOrbit = string.Empty;
            surveyReqMinAltitude = string.Empty;
            surveyReqMaxAltitude = string.Empty;
            surveyOverall = string.Empty;
        }
    }

    internal static class WormholeMouthDiscoveryNotifier
    {
        internal static void Notify(CelestialBody body, bool firstDiscovery)
        {
            if (body == null)
                return;

            ScreenMessages.PostScreenMessage(Local.MouthDiscoveredMessage(CelestialBodyDisplay.ForMessage(body)), 8f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage(WormholeMouthAiming.FormatMouthCoordinates(body), 8f, ScreenMessageStyle.UPPER_LEFT);

            if (firstDiscovery && Funding.Instance != null)
            {
                Funding.Instance.AddFunds(DiscoveredMouthRegistry.FirstDiscoveryFundsBonus, TransactionReasons.None);
                ScreenMessages.PostScreenMessage(
                    Local.MouthDiscoveryFunds(DiscoveredMouthRegistry.FirstDiscoveryFundsBonus.ToString("N0")),
                    8f,
                    ScreenMessageStyle.UPPER_LEFT);
            }
        }
    }
}
