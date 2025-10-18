// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class GameObjectToggleActionBuilder {
        internal static void Build(GameObjectToggleAction a, BuildContext ctx, string menuParameterName) {
            if (a == null || a.target == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);
            var parameters = NdmfCtxUtils.Parameters(ctx);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.target.name);
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            if (string.IsNullOrEmpty(menuParameterName)) {
                AmityMenuUtils.CreateOrGetVRCParameter(parameters, paramName, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);

            var layer = fx.NewLayer($"Amity {baseName} Toggle");
            var onState = layer.NewState("On");
            var offState = layer.NewState("Off");
            layer.stateMachine.defaultState = offState;

            var toOn = layer.stateMachine.AddAnyStateTransition(onState);
            toOn.hasExitTime = false; toOn.duration = 0f; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState);
            toOff.hasExitTime = false; toOff.duration = 0f; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            string path = AmityMenuUtils.RelativePath(avatarRoot, a.target.transform);
            var onClip = new AnimationClip { name = $"on_{baseName}" };
            var offClip = new AnimationClip { name = $"off_{baseName}" };
            var onCurve = new AnimationCurve(); onCurve.AddKey(0f, 1f);
            var offCurve = new AnimationCurve(); offCurve.AddKey(0f, 0f);
            var binding = new EditorCurveBinding { path = path, propertyName = "m_IsActive", type = typeof(GameObject) };
            AnimationUtility.SetEditorCurve(onClip, binding, onCurve);
            AnimationUtility.SetEditorCurve(offClip, binding, offCurve);
            AssetDatabase.AddObjectToAsset(onClip, assetContainer);
            AssetDatabase.AddObjectToAsset(offClip, assetContainer);
            onState.motion = onClip;
            offState.motion = offClip;
        }
    }
}
