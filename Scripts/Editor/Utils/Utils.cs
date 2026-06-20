using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    public class Utils {

        // Helper to compare two curves for equality (keys and their properties)
        public static bool CurvesEqual(AnimationCurve a, AnimationCurve b) {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.keys.Length != b.keys.Length) return false;

            var ak = a.keys;
            var bk = b.keys;
            for (int i = 0; i < ak.Length; i++) {
                var ka = ak[i];
                var kb = bk[i];
                if (!Mathf.Approximately(ka.time, kb.time)) return false;
                if (!Mathf.Approximately(ka.value, kb.value)) return false;
                if (!Mathf.Approximately(ka.inTangent, kb.inTangent)) return false;
                if (!Mathf.Approximately(ka.outTangent, kb.outTangent)) return false;
#if UNITY_2018_1_OR_NEWER
                if (ka.weightedMode != kb.weightedMode) return false;
                if (!Mathf.Approximately(ka.inWeight, kb.inWeight)) return false;
                if (!Mathf.Approximately(ka.outWeight, kb.outWeight)) return false;
#endif
            }

            // Also compare wrap modes
            if (a.preWrapMode != b.preWrapMode) return false;
            if (a.postWrapMode != b.postWrapMode) return false;
            return true;
        }

        // Human-readable description of a binding
        public static string DescribeBinding(EditorCurveBinding binding) {
            var typeName = binding.type != null ? binding.type.Name : "<null>";
            return $"path='{binding.path}', property='{binding.propertyName}', type='{typeName}'";
        }

        // Compact single-line summary of a curve for logs
        public static string CurveSummary(AnimationCurve curve) {
            if (curve == null) return "<null>";
            var keys = curve.keys;
            if (keys == null || keys.Length == 0) return "keys=0";
            if (keys.Length == 1) {
                var k = keys[0];
                return $"keys=1 @({k.time:F3},{k.value:F3})";
            }
            var first = keys[0];
            var last = keys[keys.Length - 1];
            return $"keys={keys.Length} first@({first.time:F3},{first.value:F3}) last@({last.time:F3},{last.value:F3})";
        }

        // Ensures consistent equality for EditorCurveBinding as Dictionary key
        public class EditorCurveBindingComparer : IEqualityComparer<EditorCurveBinding> {
            public bool Equals(EditorCurveBinding x, EditorCurveBinding y) {
                return x.type == y.type
                       && string.Equals(x.path, y.path, StringComparison.Ordinal)
                       && string.Equals(x.propertyName, y.propertyName, StringComparison.Ordinal);
            }

            public int GetHashCode(EditorCurveBinding obj) {
                unchecked {
                    int hash = 17;
                    hash = hash * 31 + (obj.type != null ? obj.type.GetHashCode() : 0);
                    hash = hash * 31 + (obj.path != null ? obj.path.GetHashCode() : 0);
                    hash = hash * 31 + (obj.propertyName != null ? obj.propertyName.GetHashCode() : 0);
                    return hash;
                }
            }
        }
        
        public static bool IsDesktop() {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows
                   || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64
                   || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX
                   || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64;
        }

        public static Box InfoBox([CanBeNull] string text = null) {
            var box = new Box()
                .Border(1, 1)
                .BorderColor(new Color(.3f,.3f,1,1))
                .BorderRadius(2)
                .Padding(5)
                .Margin(5);
            if (text != null) {
                box.Add(new Label(text));
            }
            return box;
        }

        public static Label Header(string text) {
            return new Label(text).Bold();
        }

        /// <summary>
        /// Convenience: creates PropertyFields from property names on the given SerializedObject.
        /// </summary>
        public static void AddOverrideRow(
            VisualElement root,
            SerializedObject serializedObject,
            string overrideProp,
            string valueProp,
            string label,
            bool showWhenOn = true,
            int indentPx = 20
        ) {
            var toggleProp = serializedObject.FindProperty(overrideProp);
            var valueSerialized = serializedObject.FindProperty(valueProp);
            var overrideField = new PropertyField(toggleProp, $"Override {label}");
            var valueField = new PropertyField(valueSerialized, label);
            AddOverrideRow(root, toggleProp, overrideField, new[] { valueField }, showWhenOn, indentPx);
        }

        /// <summary>
        /// Full control: supply the toggle field and child elements.
        /// Child fields are indented and shown/hidden based on the toggle state.
        /// </summary>
        public static void AddOverrideRow(
            VisualElement root,
            SerializedProperty toggleProp,
            PropertyField overrideField,
            VisualElement[] childFields,
            bool showWhenOn = true,
            int indentPx = 20
        ) {
            var childContainer = new VisualElement();
            childContainer.style.marginLeft = indentPx;
            foreach (var child in childFields) {
                childContainer.Add(child);
            }

            root.Add(overrideField);
            root.Add(childContainer);

            UpdateOverrideVisibility(childContainer, toggleProp.boolValue, showWhenOn);

            root.TrackPropertyValue(toggleProp, prop => {
                UpdateOverrideVisibility(childContainer, prop.boolValue, showWhenOn);
            });
        }

        private static void UpdateOverrideVisibility(VisualElement container, bool toggleValue, bool showWhenOn) {
            container.style.display = toggleValue == showWhenOn ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Builds the receiver count summary line, e.g. "Generating Contact Receivers: 11 (6 Plug, 2 Touch, 1 Frot)"
        /// </summary>
        public static string BuildReceiverCountString(bool toyReceiversEnabled, bool plugEnabled, bool touchEnabled, bool frotEnabled) {
            if (!toyReceiversEnabled) return "Generating Contact Receivers: 0";

            int plugCount = plugEnabled ? 6 : 0;
            int touchCount = touchEnabled ? 2 : 0;
            int frotCount = frotEnabled ? 1 : 0;
            int total = plugCount + touchCount + frotCount;

            var parts = new List<string>();
            if (plugCount > 0) parts.Add($"{plugCount} Plug");
            if (touchCount > 0) parts.Add($"{touchCount} Touch");
            if (frotCount > 0) parts.Add($"{frotCount} Frot");
            string detail = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";
            return $"Generating Contact Receivers: {total}{detail}";
        }

        /// <summary>
        /// Creates a row with colored toy-support labels. Returns the three colored labels for updating.
        /// </summary>
        public static void CreateToySupportRow(VisualElement parent, out Label overall, out Label plug, out Label touch, out Label frot) {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.Add(new Label("Toy Support"));
            row.Add(overall = new Label("ENABLED"));
            row.Add(new Label(" ["));
            row.Add(plug = new Label("Plugs"));
            row.Add(new Label(", "));
            row.Add(touch = new Label("Touch"));
            row.Add(new Label(", "));
            row.Add(frot = new Label("Frotting"));
            row.Add(new Label("]"));
            parent.Add(row);
        }
    }
}