// SPDX-License-Identifier: GPL-3.0-only
using System.Linq;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class GameObjectToggleActionBuilder {
        internal static void Build(GameObjectToggleAction a, BuildContext ctx, VRCExpressionsMenu.Control menuControl) {
            if (a == null || a.target == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);
            var parameters = NdmfCtxUtils.Parameters(ctx);
            var menuParameterName = CommonBuilderUtils.SelectParameter(a.parameterSelection, menuControl);

            string paramName = menuParameterName ?? "Unnamed";

            if (string.IsNullOrEmpty(menuParameterName)) {
                AmityMenuUtils.CreateOrGetVRCParameter(parameters, paramName, VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }

            // Initialize Animator As Code.
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = $"Amity {paramName} Toggle",
                AnimatorRoot = ctx.AvatarRootTransform,
                DefaultValueRoot = ctx.AvatarRootTransform,
                AssetKey = GUID.Generate().ToString(),
                AssetContainer = ctx.AssetContainer,
                ContainerMode = AacConfiguration.Container.OnlyWhenPersistenceRequired,
                AssetContainerProvider = new NDMFContainerProvider(ctx),
                DefaultsProvider = new AacDefaultsProvider(true)
            });

            var ctrl = aac.NewAnimatorController();
            var layer = ctrl.NewLayer();
            
            var onClip = aac.NewClip($"on_{paramName}").Toggling(a.target, true);
            var offClip = aac.NewClip($"off_{paramName}").Toggling(a.target, false);
            
            var floatParam = layer.FloatParameter(paramName, 0);
            var blendTree = aac.NewBlendTree()
                .Simple1D(floatParam)
                .WithAnimation(offClip, 0)
                .WithAnimation(onClip, 1);
            
            var blendTreeState = layer.NewState("Toggle").WithWriteDefaultsSetTo(true);
            blendTreeState.WithAnimation(blendTree);
            layer.WithDefaultState(blendTreeState);

            // Create a new object in the scene. We will add Modular Avatar components inside it.
            var modularAvatar = MaAc.Create(new GameObject($"Amity {paramName} Toggle")
            {
                transform = { parent = ctx.AvatarRootTransform }
            });
            
            // By creating a Modular Avatar Merge Animator component,
            // our animator controller will be added to the avatar's FX layer.
            modularAvatar.NewMergeAnimator(ctrl.AnimatorController, VRCAvatarDescriptor.AnimLayerType.FX);
        }
    }
}
