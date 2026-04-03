// SPDX-License-Identifier: GPL-3.0-only
using System.Linq;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.EditorUtils;
using org.Tayou.AmityEdits.Internal;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    internal static class MaterialSwapActionBuilder {
        internal static void Build(MaterialSwapAction a, BuildContext ctx, VRCExpressionsMenu.Control menuControl) {
            if (a == null || a.targetRenderer == null || a.toMaterial == null) return;
            var fx = NdmfCtxUtils.Fx(ctx);
            var avatarRoot = NdmfCtxUtils.AvatarRoot(ctx);
            var assetContainer = NdmfCtxUtils.AssetContainer(ctx);
            var menuParameterName = CommonBuilderUtils.SelectParameter(a.parameterSelection, menuControl);

            string baseName = CommonBuilderUtils.Sanitize(a.name ?? a.targetRenderer.name + "_MatSwap");
            string paramName = !string.IsNullOrEmpty(menuParameterName) ? menuParameterName : $"Amity/Menu/{baseName}";

            // Initialize Animator As Code.
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = $"Amity {baseName} Swap",
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

            var onState = layer.NewState("Swap");
            var offState = layer.NewState("Revert");
            layer.WithDefaultState(offState);

            var animParam = fx.parameters.FirstOrDefault(p => p != null && p.name == paramName);
            if (animParam == null) {
                animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(fx, paramName, AnimatorControllerParameterType.Bool);
            }

            var floatParam = layer.FloatParameter(animParam.name, 0);
            offState.TransitionsTo(onState).When(floatParam.IsGreaterThan(0.01f));
            onState.TransitionsTo(offState).When(floatParam.IsLessThan(0.01f));

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
            onState.WithAnimation(onClip); offState.WithAnimation(offClip);

            // Create a new object in the scene. We will add Modular Avatar components inside it.
            var modularAvatar = MaAc.Create(new GameObject($"Amity {baseName} Swap")
            {
                transform = { parent = ctx.AvatarRootTransform }
            });
            // var mergeBlendTree = a.targetRenderer.gameObject.AddComponent<MotionMerger>();
            // mergeBlendTree.Motion = directBlendTree.BlendTree;
            // mergeBlendTree.LayerPriority = int.MinValue + 100;
            
            // By creating a Modular Avatar Merge Animator component,
            // our animator controller will be added to the avatar's FX layer.
            modularAvatar.NewMergeAnimator(ctrl.AnimatorController, VRCAvatarDescriptor.AnimLayerType.FX);
        }
    }
}
