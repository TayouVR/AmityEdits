// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor.Animations;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class AnimationSlotActionBuilder {
        internal static void Build(AnimationSlotAction a, BuildContext ctx, string menuParameterName) {
            if (a == null || a.clip == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.slotName ?? "AnimSlot");
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            if (string.IsNullOrEmpty(menuParameterName)) {
                var parameters = NdmfCtxUtils.Parameters(ctx);
                AmityMenuUtils.CreateOrGetVRCParameter(parameters, paramName, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);

            var layer = fx.NewLayer($"Amity {baseName} Clip");
            var playState = layer.NewState("Play");
            var idleState = layer.NewState("Idle");
            layer.stateMachine.defaultState = idleState;
            var toPlay = layer.stateMachine.AddAnyStateTransition(playState); toPlay.hasExitTime = false; toPlay.duration = 0; toPlay.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toIdle = layer.stateMachine.AddAnyStateTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = 0; toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);
            playState.motion = a.clip;
        }
    }
}
