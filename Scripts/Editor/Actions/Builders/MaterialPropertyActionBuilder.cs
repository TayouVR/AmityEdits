// SPDX-License-Identifier: GPL-3.0-only
using AnimatorAsCode.V1;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using org.Tayou.AmityEdits.Internal;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class MaterialPropertyActionBuilder {
        internal static void Build(MaterialPropertyAction a, BuildContext ctx, VRCExpressionsMenu.Control menuControl) {
            if (a == null || (a.targetRenderer == null && !a.applyToAllRenderers) || string.IsNullOrEmpty(a.propertyName)) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);
            var menuParameterName = CommonBuilderUtils.SelectParameter(a.parameterSelection, menuControl);

            string paramName = menuParameterName ?? "Unnamed";
            
            Debug.Log($"MaterialPropertyActionBuilder: Building material property action: {a.propertyName}");
            
            // Initialize Animator As Code.
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = $"Amity MatProp {paramName}",
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

            var clip = CreateCurvedClip(a, avatarRoot, aac.NewClip($"mat_{paramName}"));

            var floatParam = layer.FloatParameter(paramName, 0);
            var directBlendTree = aac.NewBlendTree().Direct().WithAnimation(clip, floatParam);

            layer.NewState("Apply").WithAnimation(directBlendTree);
            
            Debug.Log($"MaterialPropertyActionBuilder: Created animator logic for material property: {a.propertyName}");

            // Create a new object in the scene. We will add Modular Avatar components inside it.
            var maGameObject = new GameObject($"Amity MatProp {paramName}") {
                transform = { parent = ctx.AvatarRootTransform }
            };
            var mergeBlendTree = maGameObject.AddComponent<MotionMerger>();
            mergeBlendTree.Motion = directBlendTree.BlendTree;
            mergeBlendTree.LayerPriority = int.MinValue + 100;
            // var modularAvatar = MaAc.Create(maGameObject);
            //
            // // By creating a Modular Avatar Merge Animator component,
            // // our animator controller will be added to the avatar's FX layer.
            // modularAvatar.NewMergeAnimator(ctrl.AnimatorController, VRCAvatarDescriptor.AnimLayerType.FX);
        }

        private static AacFlClip CreateCurvedClip(MaterialPropertyAction a, Transform avatarRoot, AacFlClip clip) {
            var curve = a.blendCurve ?? AnimationCurve.Linear(0, 0, 1, 1);
            var clip2 = clip.Animating(clip => {
                foreach (var r in CommonBuilderUtils.EnumerateRenderers(a, avatarRoot)) {
                    clip.Animates(r, $"material.{a.propertyName}").WithAnimationCurve(curve);
                }
            });
            return clip2;
        }

    }
}
