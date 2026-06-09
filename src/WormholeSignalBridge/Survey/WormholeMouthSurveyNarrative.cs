using KSP.Localization;

namespace WormholeSignalBridge
{
    internal static class WormholeMouthSurveyNarrative
    {
        internal static string Default() =>
            Localizer.Format("#LOC_WSB_sciResult_default");

        internal static string ForBody(CelestialBody body)
        {
            if (body == null)
                return Default();

            string bodyName = CelestialBodyDisplay.ForMessage(body);
            string parentName = body.referenceBody != null
                ? CelestialBodyDisplay.ForMessage(body.referenceBody)
                : Localizer.Format("#LOC_WSB_sciResult_unknownParent");

            if (WormholeRegistry.TryGetPartner(body, out CelestialBody partner))
            {
                return Localizer.Format(
                    "#LOC_WSB_sciResult_bodyWithPartner",
                    bodyName,
                    parentName,
                    CelestialBodyDisplay.ForMessage(partner));
            }

            return Localizer.Format("#LOC_WSB_sciResult_body", bodyName, parentName);
        }
    }
}
