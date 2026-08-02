using System;
using System.Reflection;
using UnityEngine;

namespace WormholeSignalBridge
{
    /// <summary>
    /// Kerbalism Experiment integration without a compile-time dependency.
    /// </summary>
    internal static class KerbalismExperimentBridge
    {
        private static readonly string ExperimentId = DiscoveredMouthRegistry.ExperimentId;

        private static Type experimentType;
        private static FieldInfo experimentIdField;
        private static FieldInfo statusField;
        private static FieldInfo issueField;
        private static FieldInfo expStateField;
        private static PropertyInfo statusProperty;
        private static PropertyInfo subjectProperty;
        private static PropertyInfo scienceCollectedTotalProperty;
        private static MethodInfo toggleMethod;
        private static bool initialized;
        private static bool available;

        private const int ExpStatusRunning = 1;
        private const int ExpStatusForced = 2;
        private const int ExpStatusIssue = 4;

        private const int ExpStateRunning = 1;
        private const int ExpStateForced = 2;

        internal static bool Available
        {
            get
            {
                EnsureInitialized();
                return available;
            }
        }

        internal static bool TryGetExperiment(Part part, out PartModule module, out int statusValue)
        {
            module = null;
            statusValue = -1;
            if (!Available || part == null)
                return false;

            foreach (PartModule candidate in part.Modules)
            {
                if (candidate == null || candidate.GetType() != experimentType)
                    continue;

                string id = experimentIdField.GetValue(candidate) as string;
                if (!string.Equals(id, ExperimentId, StringComparison.Ordinal))
                    continue;

                module = candidate;
                statusValue = ReadStatus(candidate);
                return true;
            }

            return false;
        }

        internal static void SetIssue(PartModule experiment, string issue)
        {
            if (!Available || experiment == null || issueField == null)
                return;

            issueField.SetValue(experiment, issue ?? string.Empty);
        }

        internal static bool IsWaiting(int statusValue) => statusValue == 3;

        internal static bool IsRunning(int statusValue) => statusValue == ExpStatusRunning || statusValue == ExpStatusForced;

        /// <summary>Experiment motor is on: collecting, forced, or paused on issue.</summary>
        internal static bool IsCollecting(int statusValue) =>
            statusValue == ExpStatusRunning || statusValue == ExpStatusForced || statusValue == ExpStatusIssue;

        internal static bool HasCollectedScience(PartModule experiment)
        {
            if (!Available || experiment == null || subjectProperty == null || scienceCollectedTotalProperty == null)
                return false;

            object subject = subjectProperty.GetValue(experiment, null);
            if (subject == null)
                return false;

            object collected = scienceCollectedTotalProperty.GetValue(subject, null);
            return collected != null && (double)collected > 0.0;
        }

        internal static bool IsExpStateActive(PartModule experiment)
        {
            if (experiment == null || expStateField == null)
                return false;

            object value = expStateField.GetValue(experiment);
            if (value == null)
                return false;

            int state = (int)value;
            return state == ExpStateRunning || state == ExpStateForced;
        }

        internal static void StopExperiment(PartModule experiment)
        {
            if (!Available || experiment == null || toggleMethod == null)
                return;

            if (!IsExpStateActive(experiment))
                return;

            toggleMethod.Invoke(experiment, new object[] { false });
        }

        internal static void SetStartEventsEnabled(PartModule experiment, bool enabled)
        {
            if (experiment?.Events == null)
                return;

            SetEventEnabled(experiment, "ToggleEvent", enabled);
            SetEventEnabled(experiment, "ShowPopup", enabled);
        }

        private static void SetEventEnabled(PartModule experiment, string eventName, bool enabled)
        {
            if (!experiment.Events.Contains(eventName))
                return;

            BaseEvent evt = experiment.Events[eventName];
            evt.active = enabled;
            evt.guiActive = enabled;
            evt.guiActiveUncommand = enabled;
            evt.guiActiveUnfocused = enabled;
        }

        private static int ReadStatus(PartModule experiment)
        {
            object status = statusProperty != null
                ? statusProperty.GetValue(experiment, null)
                : statusField?.GetValue(experiment);
            return status != null ? (int)status : -1;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            foreach (var assembly in AssemblyLoader.loadedAssemblies)
            {
                if (!string.Equals(assembly.assembly.GetName().Name, "Kerbalism", StringComparison.Ordinal))
                    continue;

                experimentType = assembly.assembly.GetType("KERBALISM.Experiment");
                if (experimentType == null)
                    return;

                const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                experimentIdField = experimentType.GetField("experiment_id", instance);
                statusField = experimentType.GetField("status", instance);
                issueField = experimentType.GetField("issue", instance);
                expStateField = experimentType.GetField("expState", instance);
                statusProperty = experimentType.GetProperty("Status", BindingFlags.Instance | BindingFlags.Public);
                subjectProperty = experimentType.GetProperty("Subject", BindingFlags.Instance | BindingFlags.Public);
                toggleMethod = experimentType.GetMethod("Toggle", instance, null, new[] { typeof(bool) }, null);

                Type subjectDataType = assembly.assembly.GetType("KERBALISM.SubjectData");
                if (subjectDataType != null)
                    scienceCollectedTotalProperty = subjectDataType.GetProperty("ScienceCollectedTotal", BindingFlags.Instance | BindingFlags.Public);

                available = experimentIdField != null &&
                            (statusField != null || statusProperty != null) &&
                            expStateField != null &&
                            toggleMethod != null &&
                            issueField != null &&
                            subjectProperty != null &&
                            scienceCollectedTotalProperty != null;
                return;
            }
        }
    }
}
