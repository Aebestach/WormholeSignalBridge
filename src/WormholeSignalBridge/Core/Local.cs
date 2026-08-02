using KSP.Localization;

namespace WormholeSignalBridge
{
    internal static class Local
    {
        internal static string WormholeSurveyIssueNoBody =>
            Localizer.Format("#LOC_WSB_surveyIssueNoBody");

        internal static string WormholeSurveyIssueNotWormholeBody =>
            Localizer.Format("#LOC_WSB_surveyIssueNotWormhole");

        internal static string WormholeSurveyIssueTooLow =>
            Localizer.Format("#LOC_WSB_surveyIssueTooLow");

        internal static string WormholeSurveyIssueTooHigh =>
            Localizer.Format("#LOC_WSB_surveyIssueTooHigh");

        internal static string WormholeSurveyIssueNotInOrbit =>
            Localizer.Format("#LOC_WSB_surveyIssueNotInOrbit");

        internal static string MouthDiscoveredMessage(string bodyDisplayName) =>
            Localizer.Format("#LOC_WSB_mouthDiscovered", bodyDisplayName);

        internal static string MouthDiscoveryFunds(string amount) =>
            Localizer.Format("#LOC_WSB_mouthDiscoveryFunds", amount);

        internal static string AimMouthSelected(string bodyDisplayName) =>
            Localizer.Format("#LOC_WSB_aimMouthSelected", bodyDisplayName);

        internal static string SurveyStatusIssue(string issue) =>
            Localizer.Format("#LOC_WSB_surveyStatusIssue", issue);

        internal static string SurveyReqIntro =>
            Localizer.Format("#LOC_WSB_surveyReqIntroHint");

        internal static string SurveyOverallReady =>
            Localizer.Format("#LOC_WSB_surveyOverallReady");

        internal static string SurveyOverallNotReady =>
            Localizer.Format("#LOC_WSB_surveyOverallNotReady");

        internal static string SurveyReqMinAltitudeGeneric =>
            Localizer.Format("#LOC_WSB_surveyReq_minAltitude");

        internal static string SurveyReqMaxAltitudeGeneric =>
            Localizer.Format("#LOC_WSB_surveyReq_maxAltitude");

        internal static string SurveyReqMinAltitudeAt(string altitude) =>
            Localizer.Format("#LOC_WSB_surveyReq_minAltitude_at", altitude);

        internal static string SurveyReqMaxAltitudeAt(string altitude) =>
            Localizer.Format("#LOC_WSB_surveyReq_maxAltitude_at", altitude);

        internal static string SurveyReqMaxAltitudeNa =>
            Localizer.Format("#LOC_WSB_surveyReq_maxAltitudeNa");

        internal static string SurveyReqAltitudeHintMin(double meters) =>
            Localizer.Format("#LOC_WSB_surveyReq_altHintMin", AltitudeDisplay.Format(meters));

        internal static string SurveyReqAltitudeHintMax(double meters) =>
            Localizer.Format("#LOC_WSB_surveyReq_altHintMax", AltitudeDisplay.Format(meters));

        internal static string MouthGuiCommStatus(bool online) =>
            online ? Localizer.Format("#LOC_WSB_mouthGuiCommOnline") : Localizer.Format("#LOC_WSB_mouthGuiCommOffline");

        internal static string MouthGuiListHeader =>
            Localizer.Format("#LOC_WSB_mouthGuiListHeader");

        internal static string MouthGuiEmptyList =>
            Localizer.Format("#LOC_WSB_mouthGuiEmptyList");

        internal static string MouthGuiClose =>
            Localizer.Format("#LOC_WSB_mouthGuiClose");

        internal static string MouthEntryLabel(string bodyName) =>
            Localizer.Format("#LOC_WSB_mouthEntryLabel", bodyName);

        internal static string MouthEntryNotDiscovered =>
            Localizer.Format("#LOC_WSB_mouthEntryNotDiscovered");

        internal static string MouthEntryNoVessel =>
            Localizer.Format("#LOC_WSB_mouthEntryNoVessel");

        internal static string MouthEntryWrongBody(string bodyDisplayName) =>
            Localizer.Format("#LOC_WSB_mouthEntryWrongBody", bodyDisplayName);

        internal static string MouthEntryAimOffBody(string bodyDisplayName) =>
            Localizer.Format("#LOC_WSB_mouthEntryAimOffBody", bodyDisplayName);

        internal static string MouthEntryNoComm =>
            Localizer.Format("#LOC_WSB_mouthEntryNoComm");

        internal static string MouthEntryOrbitBlocked(string reason) =>
            Localizer.Format("#LOC_WSB_mouthEntryOrbitBlocked", reason);

        internal static string MouthEntryReady =>
            Localizer.Format("#LOC_WSB_mouthEntryReady");

        internal static string MouthEntryNoLink =>
            Localizer.Format("#LOC_WSB_mouthEntryNoLink");

        internal static string MouthEntryLinkRate(double rate) =>
            Localizer.Format("#LOC_WSB_mouthEntryLinkRate", rate);
    }

    internal static class LocalExtensions
    {
        internal static string Format(this string format, params object[] args) =>
            string.Format(format, args);
    }
}
