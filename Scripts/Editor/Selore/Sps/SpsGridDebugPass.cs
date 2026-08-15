// SPDX-License-Identifier: GPL-3.0-only
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    [DependsOnContext(typeof(AnimatorServicesContext))]
    public class SpsGridDebugPass : Pass<SpsGridDebugPass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.SpsGridDebug";
        public override string DisplayName => "SPS Grid Debug Overlay";

        protected override void Execute(BuildContext ctx) {
            var components = ctx.AvatarRootObject.GetComponentsInChildren<SpsGridDebug>(true);
            if (components.Length == 0) return;

            foreach (var debug in components) {
                if (!debug.showOverlay) continue;
                CreateOverlay(debug, ctx);
            }
        }

        private static void CreateOverlay(SpsGridDebug debug, BuildContext ctx) {
            var shader = Shader.Find("Hidden/Amity/SpsDebugOverlay");
            if (shader == null) {
                Debug.LogWarning("[SpsGridDebug] SPS debug overlay shader not found. " +
                                 "Make sure Amity SPS is installed.");
                return;
            }

            var obj = new GameObject("SPS Debug Overlay");

            // Attach to the component's transform (under the avatar)
            obj.transform.SetParent(debug.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            // One point per possible SPS slot. The geometry shader decodes the
            // slot and emits a gizmo only when it contains a unique valid cell.
            var mesh = SpsGridDebugRendererBuilder.CreateDispatchMesh("SpsDebugDispatchMesh");
            AssetDatabase.AddObjectToAsset(mesh, ctx.AssetContainer);

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // Material
            var mat = new Material(shader) {
                name = "SpsDebugOverlay_Generated"
            };
            AssetDatabase.AddObjectToAsset(mat, ctx.AssetContainer);

            SpsGridDebugRendererBuilder.ApplyMaterialProperties(mat, debug);

            // Prevent property strippers from removing our properties
            // I don't think we need this, but I'll need to test that
            if (mat.shader != null) {
                var count = ShaderUtil.GetPropertyCount(mat.shader);
                for (var i = 0; i < count; i++) {
                    var propName = ShaderUtil.GetPropertyName(mat.shader, i);
                    mat.SetOverrideTag(propName + "Animated", "1");
                }
            }

            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            RepathAnimations(ctx, mr, debug);
        }

        private static void RepathAnimations(BuildContext ctx, MeshRenderer mr, SpsGridDebug debug) {
            var index = ctx.Extension<AnimatorServicesContext>().AnimationIndex;
            var root = ctx.AvatarRootTransform;

            var sourcePath = AnimationUtility.CalculateTransformPath(debug.transform, root);
            var targetPath = AnimationUtility.CalculateTransformPath(mr.transform, root);

            foreach (var clip in index.GetClipsForObjectPath(sourcePath)) {
                foreach (var binding in clip.GetFloatCurveBindings()) {
                    if (binding.type != typeof(SpsGridDebug)) continue;

                    var (prop, channel) = MapProperty(binding.propertyName);
                    if (prop == null) continue;

                    var curve = clip.GetFloatCurve(binding);
                    if (curve == null) continue;

                    var newBinding = EditorCurveBinding.FloatCurve(
                        targetPath,
                        typeof(Renderer),
                        $"material.{prop}{channel}"
                    );

                    if (prop == "_ZTest") {
                        curve = RemapZTestCurve(curve);
                    }

                    clip.SetFloatCurve(newBinding, curve);
                    clip.SetFloatCurve(binding, null);
                }
            }
        }

        private static (string prop, string channel) MapProperty(string propertyName) {
            var field = propertyName;
            var channel = "";

            if (field.Length > 2) {
                var suffix = field.Substring(field.Length - 2);
                if (suffix == ".r" || suffix == ".g" || suffix == ".b" || suffix == ".a") {
                    channel = suffix;
                    field = field.Substring(0, field.Length - 2);
                }
            }

            if (FieldToProp.TryGetValue(field, out var prop)) {
                return (prop, channel);
            }

            return (null, null);
        }

        private static readonly Dictionary<string, string> FieldToProp = new Dictionary<string, string> {
            { "showSockets", "_ShowSockets" },
            { "showPlugs", "_ShowPlugs" },
            { "showRing", "_ShowRing" },
            { "showArrow", "_ShowArrow" },
            { "showTags", "_ShowTags" },
            { "showChainLinks", "_ShowChain" },
            { "gizmoScale", "_GizmoScale" },
            { "lineWidthPixels", "_LineWidthPx" },
            { "depthTested", "_ZTest" },
            { "radius", "_Radius" },
            { "fadeWidth", "_FadeWidth" },
            { "holeColor", "_HoleColor" },
            { "ringColor", "_RingColor" },
            { "reversibleColor", "_ReversibleColor" },
            { "plugColor", "_PlugColor" },
            { "chainColor", "_ChainColor" }
        };

        private static AnimationCurve RemapZTestCurve(AnimationCurve curve) {
            var keys = curve.keys;
            var newKeys = new Keyframe[keys.Length];
            for (var i = 0; i < keys.Length; i++) {
                newKeys[i] = keys[i];
                newKeys[i].value = keys[i].value > 0.5f
                    ? (float)UnityEngine.Rendering.CompareFunction.LessEqual
                    : (float)UnityEngine.Rendering.CompareFunction.Always;
            }
            return new AnimationCurve(newKeys);
        }
    }
}
