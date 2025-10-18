// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.Actions;
using org.Tayou.AmityEdits.Actions.Editor.Builders;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class MaterialPropertyActionBuilder {
        internal static void Build(MaterialPropertyAction a, BuildContext ctx, string menuParameterName) {
            if (a == null || a.targetRenderer == null || string.IsNullOrEmpty(a.propertyName)) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.targetRenderer.name + "." + a.propertyName);
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            var layer = fx.NewLayer($"Amity MatProp {baseName}");
            var onState = layer.NewState("Apply");
            var offState = layer.NewState("Idle");
            layer.stateMachine.defaultState = offState;

            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);
            var toOn = layer.stateMachine.AddAnyStateTransition(onState); toOn.hasExitTime = false; toOn.duration = 0; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState); toOff.hasExitTime = false; toOff.duration = 0; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            var clip = new AnimationClip { name = $"mat_{baseName}" };
            foreach (var r in CommonBuilderUtils.EnumerateRenderers(a, avatarRoot)) {
                string rPath = AmityMenuUtils.RelativePath(avatarRoot, r.transform);
                switch (a.propertyType) {
                    case MaterialPropertyType.Float:
                        CommonBuilderUtils.SetMatFloat(clip, rPath, a.propertyName, a.floatValue);
                        break;
                    case MaterialPropertyType.Color:
                        CommonBuilderUtils.SetMatColor(clip, rPath, a.propertyName, a.colorValue);
                        break;
                    case MaterialPropertyType.Vector:
                        CommonBuilderUtils.SetMatVector(clip, rPath, a.propertyName, a.vectorValue);
                        break;
                    case MaterialPropertyType.Texture:
                        CommonBuilderUtils.SetMatTexture(clip, rPath, a.propertyName, a.textureValue);
                        break;
                }
            }
            AssetDatabase.AddObjectToAsset(clip, assetContainer);
            onState.motion = clip;
            offState.motion = null;
        }
    }
}
