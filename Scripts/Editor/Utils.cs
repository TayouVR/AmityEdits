using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    }
}