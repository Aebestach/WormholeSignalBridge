using KSP.Localization;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    /// <summary>
    /// Adds wormhole mouth targeting to directional RA antennas without modifying RA UI code.
    /// </summary>
    public sealed class ModuleWormholeMouthAiming : PartModule
    {
        private const string RaPawGroup = "RealAntennas";

        [KSPField(isPersistant = true)]
        public string wsbMouthTargetBody = string.Empty;

        private ModuleRealAntenna realAntenna;

        /// <summary>
        /// KSPCF collapses PAW items by PartModule within a shared groupName; without this the sub-group header is blank.
        /// </summary>
        public override string GetModuleDisplayName() =>
            Localizer.Format("#LOC_WSB_pawGroup");

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            realAntenna = part.FindModuleImplementing<ModuleRealAntenna>();
            RefreshRememberedTargetFromAntenna();
            RefreshSelectorEvent();
        }

        private void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            RefreshSelectorEvent();
            RefreshRememberedTargetFromAntenna();
        }

        private void RefreshSelectorEvent()
        {
            if (Events["SelectWormholeMouth"] == null)
                return;

            bool canOpen = realAntenna != null && WormholeMouthTargetEvaluator.AntennaCanOpenSelector(realAntenna);
            Events["SelectWormholeMouth"].active = canOpen;
            Events["SelectWormholeMouth"].guiActive = canOpen;
        }

        private void RefreshRememberedTargetFromAntenna()
        {
            if (realAntenna?.RAAntenna == null)
                return;

            WormholeLinkSettings settings = WormholeSettings.Current;
            if (!string.IsNullOrEmpty(wsbMouthTargetBody))
            {
                CelestialBody rememberedBody = FlightGlobals.GetBodyByName(wsbMouthTargetBody);
                if (rememberedBody != null &&
                    WormholeMouthPointing.TargetsMouthLatLonAlt(realAntenna.RAAntenna, rememberedBody, settings))
                    return;

                wsbMouthTargetBody = string.Empty;
            }

            foreach (CelestialBody body in DiscoveredMouthRegistry.DiscoveredBodies())
            {
                if (WormholeMouthPointing.TargetsMouthLatLonAlt(realAntenna.RAAntenna, body, settings))
                {
                    wsbMouthTargetBody = body.name;
                    return;
                }
            }
        }

        [KSPEvent(active = false, guiActive = true, guiName = "#LOC_WSB_selectMouthEvent", name = "SelectWormholeMouth", groupName = RaPawGroup, groupDisplayName = "#LOC_WSB_pawGroup")]
        public void SelectWormholeMouth()
        {
            if (realAntenna == null || !WormholeMouthTargetEvaluator.AntennaCanOpenSelector(realAntenna))
                return;

            WormholeMouthTargetManager.Acquire(realAntenna);
        }

        internal static void RememberMouthTarget(RealAntenna antenna, CelestialBody body)
        {
            ModuleWormholeMouthAiming module = FindLoadedModule(antenna);
            if (module != null)
                module.wsbMouthTargetBody = body?.name ?? string.Empty;
        }

        internal static bool IsRememberedMouthTarget(RealAntenna antenna, Vessel vessel, CelestialBody body)
        {
            if (body == null)
                return false;

            string rememberedBody = GetRememberedMouthTargetBody(antenna, vessel);
            return string.Equals(rememberedBody, body.name, System.StringComparison.Ordinal);
        }

        private static string GetRememberedMouthTargetBody(RealAntenna antenna, Vessel vessel)
        {
            ModuleWormholeMouthAiming loaded = FindLoadedModule(antenna);
            if (loaded != null)
                return loaded.wsbMouthTargetBody;

            ProtoPartModuleSnapshot antennaSnapshot = antenna?.ParentSnapshot;
            if (antennaSnapshot == null || vessel?.protoVessel?.protoPartSnapshots == null)
                return string.Empty;

            foreach (ProtoPartSnapshot partSnapshot in vessel.protoVessel.protoPartSnapshots)
            {
                if (!partSnapshot.modules.Contains(antennaSnapshot))
                    continue;

                ProtoPartModuleSnapshot aimingSnapshot = partSnapshot.FindModule(nameof(ModuleWormholeMouthAiming));
                string rememberedBody = string.Empty;
                if (aimingSnapshot?.moduleValues != null &&
                    aimingSnapshot.moduleValues.TryGetValue(nameof(wsbMouthTargetBody), ref rememberedBody))
                    return rememberedBody;

                return string.Empty;
            }

            return string.Empty;
        }

        private static ModuleWormholeMouthAiming FindLoadedModule(RealAntenna antenna)
        {
            if (!(antenna?.Parent is ModuleRealAntenna module) || module.part == null)
                return null;

            return module.part.FindModuleImplementing<ModuleWormholeMouthAiming>();
        }
    }
}
