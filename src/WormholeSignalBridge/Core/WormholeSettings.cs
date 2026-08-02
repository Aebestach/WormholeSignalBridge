using System.Reflection;
using KSP.Localization;

namespace WormholeSignalBridge
{
    internal static class WormholeParameterContext
    {
        internal static GameParameters Active { get; private set; }

        internal static void Capture(GameParameters parameters)
        {
            if (parameters != null)
                Active = parameters;
        }

        internal static WormholeBridgeParameters Bridge =>
            (Active ?? HighLogic.CurrentGame?.Parameters)?.CustomParams<WormholeBridgeParameters>();
    }

    internal sealed class WormholeLinkSettings
    {
        internal bool Enabled = true;
        internal bool DebugLogging;
        internal double EffectiveDistance = 1000;
        internal double InsertionLoss;
        internal bool AdvancedOrbitConstraints;
        internal double MaxMouthAltitude = 500000;
        internal double OptimalMaxAltitude = 200000;
        internal double EdgeQuality = 0.2;
        internal bool StrictPeApBounds;
        internal double MaxApA = 500000;
        internal bool StrictInclinationBounds;
        internal double MinInclination = 0;
        internal double MaxInclination = 180;
        internal bool UseInclinationQuality;
        internal double PreferredInclinationMin = 0;
        internal double PreferredInclinationMax = 180;
        internal bool StrictEccentricityBounds;
        internal double IdealMaxEccentricity = 0.2;
        internal double MaxEccentricity = 0.8;
        internal double MinUsableOrbitQuality = 0.05;
        internal double OrbitLossScale = 1.0;
        internal double MouthProxyTxPower = 60;
        internal double MouthProxyDishDiameter = 30;
        internal double MouthProxySymbolRate = 1e9;
    }

    internal static class WormholeBridgePresets
    {
        internal const int Easy = 0;
        internal const int Normal = 1;
        internal const int Moderate = 2;
        internal const int Hard = 3;

        internal static int FromGlobal(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy: return Easy;
                case GameParameters.Preset.Normal: return Normal;
                case GameParameters.Preset.Moderate: return Moderate;
                case GameParameters.Preset.Hard: return Hard;
                default: return Normal;
            }
        }

        internal static void Apply(WormholeBridgeParameters p, int preset)
        {
            switch (preset)
            {
                case Easy: ApplyEasy(p); break;
                case Normal: ApplyNormal(p); break;
                case Moderate: ApplyModerate(p); break;
                case Hard: ApplyHard(p); break;
            }
        }

        internal static void ApplyEasy(WormholeBridgeParameters p)
        {
            p.enabled = true;
            p.debugLogging = false;
            p.effectiveDistance = 500f;
            p.insertionLoss = 0f;
            p.advancedOrbitConstraints = false;

            p.maxMouthAltitude = 1000000f;
            p.optimalMaxAltitude = 300000f;
            p.edgeQuality = 0.4f;
            p.strictPeApBounds = false;
            p.maxApA = 1000000f;
            p.strictInclinationBounds = false;
            p.minInclination = 0f;
            p.maxInclination = 180f;
            p.useInclinationQuality = false;
            p.preferredInclinationMin = 0f;
            p.preferredInclinationMax = 180f;
            p.strictEccentricityBounds = false;
            p.idealMaxEccentricity = 0.4f;
            p.maxEccentricity = 0.95f;
            p.minUsableOrbitQuality = 0.02f;
            p.orbitLossScale = 0.5f;
        }

        internal static void ApplyNormal(WormholeBridgeParameters p)
        {
            p.enabled = true;
            p.debugLogging = false;
            p.effectiveDistance = 1000f;
            p.insertionLoss = 0f;
            p.advancedOrbitConstraints = true;

            p.maxMouthAltitude = 500000f;
            p.optimalMaxAltitude = 200000f;
            p.edgeQuality = 0.3f;
            p.strictPeApBounds = false;
            p.maxApA = 500000f;
            p.strictInclinationBounds = false;
            p.minInclination = 0f;
            p.maxInclination = 180f;
            p.useInclinationQuality = false;
            p.preferredInclinationMin = 0f;
            p.preferredInclinationMax = 180f;
            p.strictEccentricityBounds = false;
            p.idealMaxEccentricity = 0.3f;
            p.maxEccentricity = 0.85f;
            p.minUsableOrbitQuality = 0.05f;
            p.orbitLossScale = 0.75f;
        }

        internal static void ApplyModerate(WormholeBridgeParameters p)
        {
            p.enabled = true;
            p.debugLogging = false;
            p.effectiveDistance = 1500f;
            p.insertionLoss = 2f;
            p.advancedOrbitConstraints = true;

            p.maxMouthAltitude = 400000f;
            p.optimalMaxAltitude = 180000f;
            p.edgeQuality = 0.22f;
            p.strictPeApBounds = true;
            p.maxApA = 400000f;
            p.strictInclinationBounds = false;
            p.minInclination = 0f;
            p.maxInclination = 180f;
            p.useInclinationQuality = true;
            p.preferredInclinationMin = 10f;
            p.preferredInclinationMax = 170f;
            p.strictEccentricityBounds = false;
            p.idealMaxEccentricity = 0.2f;
            p.maxEccentricity = 0.65f;
            p.minUsableOrbitQuality = 0.08f;
            p.orbitLossScale = 1f;
        }

        internal static void ApplyHard(WormholeBridgeParameters p)
        {
            p.enabled = true;
            p.debugLogging = false;
            p.effectiveDistance = 2500f;
            p.insertionLoss = 4f;
            p.advancedOrbitConstraints = true;

            p.maxMouthAltitude = 350000f;
            p.optimalMaxAltitude = 170000f;
            p.edgeQuality = 0.15f;
            p.strictPeApBounds = true;
            p.maxApA = 350000f;
            p.strictInclinationBounds = true;
            p.minInclination = 5f;
            p.maxInclination = 175f;
            p.useInclinationQuality = true;
            p.preferredInclinationMin = 15f;
            p.preferredInclinationMax = 165f;
            p.strictEccentricityBounds = true;
            p.idealMaxEccentricity = 0.15f;
            p.maxEccentricity = 0.45f;
            p.minUsableOrbitQuality = 0.12f;
            p.orbitLossScale = 1.5f;
        }
    }

    public class WormholeBridgePresetParameters : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#LOC_WSB_localPresets");
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override string Section => Localizer.Format("#LOC_WSB_Section");
        public override string DisplaySection => Section;
        public override int SectionOrder => 0;
        public override bool HasPresets => false;

        [GameParameters.CustomParameterUI("#LOC_WSB_presetEasy", toolTip = "#LOC_WSB_presetEasy_tip", autoPersistance = false)]
        public bool PresetEasy
        {
            get => WormholeParameterContext.Bridge?.IsLocalPreset(WormholeBridgePresets.Easy) ?? false;
            set { if (value) WormholeParameterContext.Bridge?.SelectLocalPreset(WormholeBridgePresets.Easy); }
        }

        [GameParameters.CustomParameterUI("#LOC_WSB_presetNormal", toolTip = "#LOC_WSB_presetNormal_tip", autoPersistance = false)]
        public bool PresetNormal
        {
            get => WormholeParameterContext.Bridge?.IsLocalPreset(WormholeBridgePresets.Normal) ?? false;
            set { if (value) WormholeParameterContext.Bridge?.SelectLocalPreset(WormholeBridgePresets.Normal); }
        }

        [GameParameters.CustomParameterUI("#LOC_WSB_presetModerate", toolTip = "#LOC_WSB_presetModerate_tip", autoPersistance = false)]
        public bool PresetModerate
        {
            get => WormholeParameterContext.Bridge?.IsLocalPreset(WormholeBridgePresets.Moderate) ?? false;
            set { if (value) WormholeParameterContext.Bridge?.SelectLocalPreset(WormholeBridgePresets.Moderate); }
        }

        [GameParameters.CustomParameterUI("#LOC_WSB_presetHard", toolTip = "#LOC_WSB_presetHard_tip", autoPersistance = false)]
        public bool PresetHard
        {
            get => WormholeParameterContext.Bridge?.IsLocalPreset(WormholeBridgePresets.Hard) ?? false;
            set { if (value) WormholeParameterContext.Bridge?.SelectLocalPreset(WormholeBridgePresets.Hard); }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            WormholeParameterContext.Capture(parameters);
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            WormholeParameterContext.Capture(parameters);
            return true;
        }
    }

    public class WormholeBridgeParameters : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#LOC_WSB_Title");
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override string Section => Localizer.Format("#LOC_WSB_Section");
        public override string DisplaySection => Section;
        public override int SectionOrder => 1;
        public override bool HasPresets => true;

        public int localPreset = WormholeBridgePresets.Normal;

        internal bool IsLocalPreset(int preset) => localPreset == preset;

        internal void SelectLocalPreset(int preset)
        {
            localPreset = preset;
            WormholeBridgePresets.Apply(this, preset);
        }

        [GameParameters.CustomParameterUI("#LOC_WSB_enabled")]
        public bool enabled = true;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_effectiveDistance", toolTip = "#LOC_WSB_effectiveDistance_tip", minValue = 1f, maxValue = 10000000f, stepCount = 1000, displayFormat = "N0")]
        public float effectiveDistance = 1000f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_insertionLoss", toolTip = "#LOC_WSB_insertionLoss_tip", minValue = 0f, maxValue = 200f, stepCount = 200, displayFormat = "N1")]
        public float insertionLoss = 0f;

        [GameParameters.CustomParameterUI("#LOC_WSB_debugLogging")]
        public bool debugLogging = false;

        [GameParameters.CustomParameterUI("#LOC_WSB_advancedOrbitConstraints", toolTip = "#LOC_WSB_advancedOrbitConstraints_tip")]
        public bool advancedOrbitConstraints = false;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_maxMouthAltitude", toolTip = "#LOC_WSB_maxMouthAltitude_tip", minValue = 1f, maxValue = 10000000f, stepCount = 1000, displayFormat = "N0")]
        public float maxMouthAltitude = 500000f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_optimalMaxAltitude", toolTip = "#LOC_WSB_optimalMaxAltitude_tip", minValue = 1f, maxValue = 10000000f, stepCount = 1000, displayFormat = "N0")]
        public float optimalMaxAltitude = 200000f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_edgeQuality", toolTip = "#LOC_WSB_edgeQuality_tip", minValue = 0.01f, maxValue = 1f, stepCount = 99, displayFormat = "N2")]
        public float edgeQuality = 0.2f;

        [GameParameters.CustomParameterUI("#LOC_WSB_strictPeApBounds", toolTip = "#LOC_WSB_strictPeApBounds_tip")]
        public bool strictPeApBounds = false;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_maxApA", toolTip = "#LOC_WSB_maxApA_tip", minValue = 1f, maxValue = 10000000f, stepCount = 1000, displayFormat = "N0")]
        public float maxApA = 500000f;

        [GameParameters.CustomParameterUI("#LOC_WSB_strictInclinationBounds")]
        public bool strictInclinationBounds = false;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_minInclination", minValue = 0f, maxValue = 180f, stepCount = 180, displayFormat = "N1")]
        public float minInclination = 0f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_maxInclination", minValue = 0f, maxValue = 180f, stepCount = 180, displayFormat = "N1")]
        public float maxInclination = 180f;

        [GameParameters.CustomParameterUI("#LOC_WSB_useInclinationQuality")]
        public bool useInclinationQuality = false;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_preferredInclinationMin", minValue = 0f, maxValue = 180f, stepCount = 180, displayFormat = "N1")]
        public float preferredInclinationMin = 0f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_preferredInclinationMax", minValue = 0f, maxValue = 180f, stepCount = 180, displayFormat = "N1")]
        public float preferredInclinationMax = 180f;

        [GameParameters.CustomParameterUI("#LOC_WSB_strictEccentricityBounds")]
        public bool strictEccentricityBounds = false;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_idealMaxEccentricity", minValue = 0f, maxValue = 1f, stepCount = 100, displayFormat = "N2")]
        public float idealMaxEccentricity = 0.2f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_maxEccentricity", minValue = 0f, maxValue = 1f, stepCount = 100, displayFormat = "N2")]
        public float maxEccentricity = 0.8f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_minUsableOrbitQuality", toolTip = "#LOC_WSB_minUsableOrbitQuality_tip", minValue = 0.01f, maxValue = 1f, stepCount = 99, displayFormat = "N2")]
        public float minUsableOrbitQuality = 0.05f;

        [GameParameters.CustomFloatParameterUI("#LOC_WSB_orbitLossScale", toolTip = "#LOC_WSB_orbitLossScale_tip", minValue = 0f, maxValue = 5f, stepCount = 100, displayFormat = "N2")]
        public float orbitLossScale = 1f;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            SelectLocalPreset(WormholeBridgePresets.FromGlobal(preset));
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            WormholeParameterContext.Capture(parameters);
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            WormholeParameterContext.Capture(parameters);
            return true;
        }
    }

    internal static class WormholeSettings
    {
        internal static WormholeLinkSettings Current
        {
            get
            {
                WormholeBridgeParameters p = HighLogic.CurrentGame?.Parameters?.CustomParams<WormholeBridgeParameters>() ?? new WormholeBridgeParameters();
                return new WormholeLinkSettings
                {
                    Enabled = p.enabled,
                    DebugLogging = p.debugLogging,
                    EffectiveDistance = p.effectiveDistance,
                    InsertionLoss = p.insertionLoss,
                    AdvancedOrbitConstraints = p.advancedOrbitConstraints,
                    MaxMouthAltitude = p.maxMouthAltitude,
                    OptimalMaxAltitude = p.optimalMaxAltitude,
                    EdgeQuality = p.edgeQuality,
                    StrictPeApBounds = p.strictPeApBounds,
                    MaxApA = p.maxApA,
                    StrictInclinationBounds = p.strictInclinationBounds,
                    MinInclination = p.minInclination,
                    MaxInclination = p.maxInclination,
                    UseInclinationQuality = p.useInclinationQuality,
                    PreferredInclinationMin = p.preferredInclinationMin,
                    PreferredInclinationMax = p.preferredInclinationMax,
                    StrictEccentricityBounds = p.strictEccentricityBounds,
                    IdealMaxEccentricity = p.idealMaxEccentricity,
                    MaxEccentricity = p.maxEccentricity,
                    MinUsableOrbitQuality = p.minUsableOrbitQuality,
                    OrbitLossScale = p.orbitLossScale
                };
            }
        }

        internal static bool DebugLogging => Current.DebugLogging;
    }
}
