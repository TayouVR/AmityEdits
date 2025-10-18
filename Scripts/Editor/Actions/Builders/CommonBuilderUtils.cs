// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.Actions;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

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

        internal static void SetMatFloat(AnimationClip clip, string path, string prop, float value) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var curve = new AnimationCurve(); curve.AddKey(0f, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
        internal static void SetMatColor(AnimationClip clip, string path, string prop, Color value) {
            var r = new AnimationCurve(); r.AddKey(0f, value.r);
            var g = new AnimationCurve(); g.AddKey(0f, value.g);
            var b = new AnimationCurve(); b.AddKey(0f, value.b);
            var a = new AnimationCurve(); a.AddKey(0f, value.a);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.r" }, r);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.g" }, g);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.b" }, b);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.a" }, a);
        }
        internal static void SetMatVector(AnimationClip clip, string path, string prop, Vector4 value) {
            var x = new AnimationCurve(); x.AddKey(0f, value.x);
            var y = new AnimationCurve(); y.AddKey(0f, value.y);
            var z = new AnimationCurve(); z.AddKey(0f, value.z);
            var w = new AnimationCurve(); w.AddKey(0f, value.w);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.x" }, x);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.y" }, y);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.z" }, z);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.w" }, w);
        }
        internal static void SetMatTexture(AnimationClip clip, string path, string prop, Texture value) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var keys = new[] { new ObjectReferenceKeyframe { time = 0f, value = value } };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
    }
}
