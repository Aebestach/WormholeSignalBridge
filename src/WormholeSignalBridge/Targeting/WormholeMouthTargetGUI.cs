using System.Collections.Generic;
using RealAntennas;
using UnityEngine;

namespace WormholeSignalBridge
{
    internal sealed class WormholeMouthTargetGUI : MonoBehaviour
    {
        private const string WindowTitle = "Wormhole Mouth Targeting";

        private Rect window = new Rect(40, 120, 450, 320);
        private Vector2 scrollPosition;
        private List<MouthTargetEntry> entries = new List<MouthTargetEntry>();

        internal ModuleRealAntenna Module { get; set; }

        private RealAntenna Antenna => Module?.RAAntenna;

        private void OnGUI()
        {
            if (Module == null || Antenna == null)
                return;

            GUI.skin = HighLogic.Skin;
            window = GUILayout.Window(GetHashCode(), window, DrawWindow, WindowTitle, HighLogic.Skin.window);
        }

        private void DrawWindow(int windowId)
        {
            Vessel vessel = Module.vessel;
            RACommNode node = WormholeMouthTargetEvaluator.GetVesselNode(vessel);

            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.Label($"Vessel: {vessel?.name ?? "None"}");
            GUILayout.Label($"Antenna: {Antenna.Name}");
            GUILayout.Label($"Band: {Antenna.RFBand.name}       Power: {Antenna.TxPower:F0} dBm");
            GUILayout.Label($"Target: {Antenna.Target}");
            GUILayout.Label(Local.MouthGuiCommStatus(node != null && node.CanComm()));
            GUILayout.EndVertical();
            GUILayout.Space(7);

            entries = WormholeMouthTargetEvaluator.BuildEntries(Module);

            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.Label(Local.MouthGuiListHeader, GUILayout.ExpandWidth(true));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200), GUILayout.ExpandWidth(true));

            if (entries.Count == 0)
                GUILayout.Label(Local.MouthGuiEmptyList);
            else
            {
                foreach (MouthTargetEntry entry in entries)
                    DrawEntry(entry);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(15);

            if (GUILayout.Button(Local.MouthGuiClose))
                Destroy(this);

            GUI.DragWindow();
        }

        private void DrawEntry(MouthTargetEntry entry)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = entry.Selectable;

            if (GUILayout.Button(entry.Label))
                SelectEntry(entry);

            GUI.enabled = previousEnabled;
        }

        private void SelectEntry(MouthTargetEntry entry)
        {
            if (entry?.Body == null || !entry.Selectable)
                return;

            WormholeMouthAiming.ApplyMouthTarget(Antenna, entry.Body);
            ScreenMessages.PostScreenMessage(Local.AimMouthSelected(CelestialBodyDisplay.ForMessage(entry.Body)), 5f, ScreenMessageStyle.UPPER_LEFT);
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (Antenna != null)
                WormholeMouthTargetManager.Release(Antenna, this);
        }
    }
}
