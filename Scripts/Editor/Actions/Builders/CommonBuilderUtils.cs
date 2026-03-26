// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.Actions;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class CommonBuilderUtils {
        internal static string Sanitize(string s) => string.IsNullOrEmpty(s) ? "Unnamed" : s.Replace("/", "_");
        
        internal static IEnumerable<Renderer> EnumerateRenderers(MaterialPropertyAction a, Transform root) {
            if (a.applyToAllRenderers) return root.GetComponentsInChildren<Renderer>(true);
            return a.targetRenderer != null ? new[] { a.targetRenderer } : Array.Empty<Renderer>();
        }
        internal static IEnumerable<Renderer> EnumerateRenderers(MaterialSwapAction a, Transform root) {
            if (a.applyToAllRenderers) return root.GetComponentsInChildren<Renderer>(true);
            return a.targetRenderer != null ? new[] { a.targetRenderer } : Array.Empty<Renderer>();
        }

        internal static void SetMatFloat(AnimationClip clip, string path, string prop, float value, float? startTime = null) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            curve.AddKey(startTime ?? 0f, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
        internal static void SetMatColor(AnimationClip clip, string path, string prop, Color value, float? startTime = null) {
            float t = startTime ?? 0f;
            SetChannel(clip, path, prop + ".r", value.r, t);
            SetChannel(clip, path, prop + ".g", value.g, t);
            SetChannel(clip, path, prop + ".b", value.b, t);
            SetChannel(clip, path, prop + ".a", value.a, t);
        }
        internal static void SetMatVector(AnimationClip clip, string path, string prop, Vector4 value, float? startTime = null) {
            float t = startTime ?? 0f;
            SetChannel(clip, path, prop + ".x", value.x, t);
            SetChannel(clip, path, prop + ".y", value.y, t);
            SetChannel(clip, path, prop + ".z", value.z, t);
            SetChannel(clip, path, prop + ".w", value.w, t);
        }

        private static void SetChannel(AnimationClip clip, string path, string prop, float value, float t) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            curve.AddKey(t, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        internal static void SetMatFloatCurve(AnimationClip clip, string path, string prop, AnimationCurve srcCurve, float startValue, float endValue) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var curve = MapCurve(srcCurve, startValue, endValue);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        internal static void SetMatColorCurve(AnimationClip clip, string path, string prop, AnimationCurve srcCurve, Color startValue, Color endValue) {
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.r" }, MapCurve(srcCurve, startValue.r, endValue.r));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.g" }, MapCurve(srcCurve, startValue.g, endValue.g));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.b" }, MapCurve(srcCurve, startValue.b, endValue.b));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.a" }, MapCurve(srcCurve, startValue.a, endValue.a));
        }

        internal static void SetMatVectorCurve(AnimationClip clip, string path, string prop, AnimationCurve srcCurve, Vector4 startValue, Vector4 endValue) {
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.x" }, MapCurve(srcCurve, startValue.x, endValue.x));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.y" }, MapCurve(srcCurve, startValue.y, endValue.y));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.z" }, MapCurve(srcCurve, startValue.z, endValue.z));
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.w" }, MapCurve(srcCurve, startValue.w, endValue.w));
        }

        private static AnimationCurve MapCurve(AnimationCurve src, float start, float end) {
            var keys = src.keys;
            var newKeys = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++) {
                newKeys[i] = keys[i];
                newKeys[i].value = Mathf.LerpUnclamped(start, end, keys[i].value);
            }
            return new AnimationCurve(newKeys);
        }
        internal static void SetMatTexture(AnimationClip clip, string path, string prop, Texture value) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var keys = new[] { new ObjectReferenceKeyframe { time = 0f, value = value } };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }

        public static string SelectParameter(ParameterSelection aParamSelection, VRCExpressionsMenu.Control menuControl) {
            return aParamSelection switch {
                ParameterSelection.Main => menuControl.parameter?.name ?? "",
                ParameterSelection.Sub1 => menuControl.subParameters[0]?.name ?? "",
                ParameterSelection.Sub2 => menuControl.subParameters[1]?.name ?? "",
                ParameterSelection.Sub3 => menuControl.subParameters[2]?.name ?? "",
                ParameterSelection.Sub4 => menuControl.subParameters[3]?.name ?? "",
                _ => ""
            };
        }
    }
}
