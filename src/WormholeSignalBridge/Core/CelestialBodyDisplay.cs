using KSP.Localization;

namespace WormholeSignalBridge
{
    internal static class CelestialBodyDisplay
    {
        /// <summary>
        /// User-facing body name without KSP localization suffix tags (e.g. WH-3141-A^N → WH-3141-A).
        /// </summary>
        internal static string ForMessage(CelestialBody body)
        {
            if (body == null)
                return "?";

            string name = body.displayName;
            if (string.IsNullOrEmpty(name))
                return body.name;

            if (name.StartsWith("#"))
                name = Localizer.Format(name);

            return StripNameTags(name);
        }

        internal static string StripNameTags(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            int caret = name.IndexOf('^');
            return caret >= 0 ? name.Substring(0, caret) : name;
        }
    }
}
