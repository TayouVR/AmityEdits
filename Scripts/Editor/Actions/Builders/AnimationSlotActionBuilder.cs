// SPDX-License-Identifier: GPL-3.0-only
using System.Linq;
using AnimatorAsCode.V1;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class AnimationSlotActionBuilder {
        internal static void Build(AnimationSlotAction a, BuildContext ctx, VRCExpressionsMenu.Control menuControl) {
            if (a == null || a.clip == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var menuParameterName = CommonBuilderUtils.SelectParameter(a.parameterSelection, menuControl);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.slotName ?? "AnimSlot");
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            if (string.IsNullOrEmpty(menuParameterName)) {
                var parameters = NdmfCtxUtils.Parameters(ctx);
                AmityMenuUtils.CreateOrGetVRCParameter(parameters, paramName, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }

            // Initialize Animator As Code.
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = $"Amity {baseName} Clip",
                AnimatorRoot = ctx.AvatarRootTransform,
                DefaultValueRoot = ctx.AvatarRootTransform,
                AssetKey = GUID.Generate().ToString(),
                AssetContainer = ctx.AssetContainer,
                ContainerMode = AacConfiguration.Container.OnlyWhenPersistenceRequired,
                AssetContainerProvider = new NDMFContainerProvider(ctx),
                DefaultsProvider = new AacDefaultsProvider(true)
            });

            var ctrl = aac.CreateSupportingArbitraryControllerLayer(fx, "");
            var layer = ctrl.StateMachine;

            var playState = layer.NewState("Play");
            var idleState = layer.NewState("Idle");
            layer.WithDefaultState(idleState);

            var animParam = fx.parameters.FirstOrDefault(p => p != null && p.name == paramName);
            if (animParam == null) {
                animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);
            }

            var floatParam = ctrl.FloatParameter(animParam.name, 0);
            idleState.TransitionsTo(playState).When(floatParam.IsGreaterThan(0.01f));
            playState.TransitionsTo(idleState).When(floatParam.IsLessThan(0.01f));

            playState.WithAnimation(a.clip);
        }
    }
}
