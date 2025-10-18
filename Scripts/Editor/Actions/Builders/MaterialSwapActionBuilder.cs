// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class MaterialSwapActionBuilder {
        internal static void Build(MaterialSwapAction a, BuildContext ctx, string menuParameterName) {
            if (a == null || a.targetRenderer == null || a.toMaterial == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.targetRenderer.name + "_MatSwap");
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            var layer = fx.NewLayer($"Amity {baseName} Swap");
            var onState = layer.NewState("Swap");
            var offState = layer.NewState("Revert");
            layer.stateMachine.defaultState = offState;

            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);
            var toOn = layer.stateMachine.AddAnyStateTransition(onState); toOn.hasExitTime = false; toOn.duration = 0; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState); toOff.hasExitTime = false; toOff.duration = 0; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            var onClip = new AnimationClip { name = $"swap_{baseName}" };
            var offClip = new AnimationClip { name = $"revert_{baseName}" };
            foreach (var r in CommonBuilderUtils.EnumerateRenderers(a, avatarRoot)) {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (a.fromMaterial != null && mats[i] != a.fromMaterial) continue;
                    string path = AmityMenuUtils.RelativePath(avatarRoot, r.transform);
                    var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"m_Materials.Array.data[{i}]" };
                    var onKeys = new[] { new ObjectReferenceKeyframe { time = 0f, value = a.toMaterial } };
                    AnimationUtility.SetObjectReferenceCurve(onClip, binding, onKeys);
                    if (a.fromMaterial != null) {
                        var offKeys = new[] { new ObjectReferenceKeyframe { time = 0f, value = a.fromMaterial } };
                        AnimationUtility.SetObjectReferenceCurve(offClip, binding, offKeys);
                    }
                }
            }
            AssetDatabase.AddObjectToAsset(onClip, assetContainer);
            AssetDatabase.AddObjectToAsset(offClip, assetContainer);
            onState.motion = onClip; offState.motion = offClip;
        }
    }
}
