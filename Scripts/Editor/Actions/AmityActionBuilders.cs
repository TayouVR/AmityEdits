// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using org.Tayou.AmityEdits.Actions;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor {
    public static class AmityActionBuilders {
        public class BuildCtx {
            public BuildCtx(VRCAvatarDescriptor descriptor, AnimatorController fx, VRCExpressionParameters parameters, Transform avatarRoot, UnityEngine.Object assetContainer) {
                Descriptor = descriptor; Fx = fx; Parameters = parameters; AvatarRoot = avatarRoot; AssetContainer = assetContainer;
            }
            public VRCAvatarDescriptor Descriptor { get; }
            public AnimatorController Fx { get; }
            public VRCExpressionParameters Parameters { get; }
            public Transform AvatarRoot { get; }
            public UnityEngine.Object AssetContainer { get; }
            // Optional: if set, all actions under the current MenuItem should use this menu parameter name.
            public string MenuParameterName { get; set; }
        }

        public static void BuildFor(this BaseAmityAction action, BuildCtx ctx) {
            switch (action) {
                case GameObjectToggleAction a: BuildGameObjectToggle(a, ctx); break;
                case ComponentToggleAction a: BuildComponentToggle(a, ctx); break;
                case MaterialPropertyAction a: BuildMaterialProperty(a, ctx); break;
                case MaterialSwapAction a: BuildMaterialSwap(a, ctx); break;
                case AnimationSlotAction a: BuildAnimationSlot(a, ctx); break;
                default:
                    Debug.LogWarning($"[Amity] No builder for action type {action?.GetType().Name}");
                    break;
            }
        }

        private static string Sanitize(string s) => string.IsNullOrEmpty(s) ? "Unnamed" : s.Replace("/", "_");

        private static void BuildGameObjectToggle(GameObjectToggleAction a, BuildCtx ctx) {
            if (a.target == null) return;
            string baseName = Sanitize(a.name ?? a.target.name);
            string paramName = !string.IsNullOrEmpty(ctx.MenuParameterName) ? ctx.MenuParameterName : $"Amity/Menu/{baseName}";

            // If no menu parameter was supplied, create a VRC parameter; always ensure animator parameter exists
            if (string.IsNullOrEmpty(ctx.MenuParameterName)) {
                AmityMenuUtils.CreateOrGetVRCParameter(ctx.Parameters, paramName, VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(ctx.Fx, paramName, AnimatorControllerParameterType.Bool);

            // create a layer
            var layer = ctx.Fx.NewLayer($"Amity {baseName} Toggle");
            var onState = layer.NewState("On");
            var offState = layer.NewState("Off");
            layer.stateMachine.defaultState = offState;

            // transitions
            var toOn = layer.stateMachine.AddAnyStateTransition(onState);
            toOn.hasExitTime = false; toOn.duration = 0f; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState);
            toOff.hasExitTime = false; toOff.duration = 0f; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            // create on/off clips for m_IsActive
            string path = AmityMenuUtils.RelativePath(ctx.AvatarRoot, a.target.transform);
            var onClip = new AnimationClip { name = $"on_{baseName}" };
            var offClip = new AnimationClip { name = $"off_{baseName}" };
            var onCurve = new AnimationCurve(); onCurve.AddKey(0f, 1f);
            var offCurve = new AnimationCurve(); offCurve.AddKey(0f, 0f);
            var binding = new EditorCurveBinding { path = path, propertyName = "m_IsActive", type = typeof(GameObject) };
            AnimationUtility.SetEditorCurve(onClip, binding, onCurve);
            AnimationUtility.SetEditorCurve(offClip, binding, offCurve);
            AssetDatabase.AddObjectToAsset(onClip, ctx.AssetContainer);
            AssetDatabase.AddObjectToAsset(offClip, ctx.AssetContainer);
            onState.motion = onClip;
            offState.motion = offClip;
        }

        private static void BuildComponentToggle(ComponentToggleAction a, BuildCtx ctx) {
            if (a.component == null) return;
            string baseName = Sanitize(a.name ?? a.component.name);
            string paramName = !string.IsNullOrEmpty(ctx.MenuParameterName) ? ctx.MenuParameterName : $"Amity/Menu/{baseName}";

            if (string.IsNullOrEmpty(ctx.MenuParameterName)) {
                AmityMenuUtils.CreateOrGetVRCParameter(ctx.Parameters, paramName, VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(ctx.Fx, paramName, AnimatorControllerParameterType.Bool);

            var layer = ctx.Fx.NewLayer($"Amity {baseName} Component");
            var onState = layer.NewState("Enabled");
            var offState = layer.NewState("Disabled");
            layer.stateMachine.defaultState = offState;
            var toOn = layer.stateMachine.AddAnyStateTransition(onState); toOn.hasExitTime = false; toOn.duration = 0; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState); toOff.hasExitTime = false; toOff.duration = 0; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            string path = AmityMenuUtils.RelativePath(ctx.AvatarRoot, a.component.transform);
            var onClip = new AnimationClip { name = $"on_{baseName}" };
            var offClip = new AnimationClip { name = $"off_{baseName}" };
            var onCurve = new AnimationCurve(); onCurve.AddKey(0f, 1f);
            var offCurve = new AnimationCurve(); offCurve.AddKey(0f, 0f);
            var binding = new EditorCurveBinding { path = path, propertyName = "m_Enabled", type = a.component.GetType() };
            AnimationUtility.SetEditorCurve(onClip, binding, onCurve);
            AnimationUtility.SetEditorCurve(offClip, binding, offCurve);
            AssetDatabase.AddObjectToAsset(onClip, ctx.AssetContainer);
            AssetDatabase.AddObjectToAsset(offClip, ctx.AssetContainer);
            onState.motion = onClip; offState.motion = offClip;
        }

        private static void BuildMaterialProperty(MaterialPropertyAction a, BuildCtx ctx) {
            if (a.targetRenderer == null || string.IsNullOrEmpty(a.propertyName)) return;
            string baseName = Sanitize(a.name ?? a.targetRenderer.name + "." + a.propertyName);
            string paramName = !string.IsNullOrEmpty(ctx.MenuParameterName) ? ctx.MenuParameterName : $"Amity/Menu/{baseName}";
            var layer = ctx.Fx.NewLayer($"Amity MatProp {baseName}");
            var onState = layer.NewState("Apply");
            var offState = layer.NewState("Idle");
            layer.stateMachine.defaultState = offState;

            // Ensure animator parameter exists (use provided MenuParameterName when present)
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(ctx.Fx, paramName, AnimatorControllerParameterType.Bool);
            var toOn = layer.stateMachine.AddAnyStateTransition(onState); toOn.hasExitTime = false; toOn.duration = 0; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState); toOff.hasExitTime = false; toOff.duration = 0; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            var clip = new AnimationClip { name = $"mat_{baseName}" };
            foreach (var r in EnumerateRenderers(a, ctx.AvatarRoot)) {
                string rPath = AmityMenuUtils.RelativePath(ctx.AvatarRoot, r.transform);
                switch (a.propertyType) {
                    case MaterialPropertyType.Float:
                        SetMatFloat(clip, rPath, a.propertyName, a.floatValue);
                        break;
                    case MaterialPropertyType.Color:
                        SetMatColor(clip, rPath, a.propertyName, a.colorValue);
                        break;
                    case MaterialPropertyType.Vector:
                        SetMatVector(clip, rPath, a.propertyName, a.vectorValue);
                        break;
                    case MaterialPropertyType.Texture:
                        SetMatTexture(clip, rPath, a.propertyName, a.textureValue);
                        break;
                }
            }
            AssetDatabase.AddObjectToAsset(clip, ctx.AssetContainer);
            onState.motion = clip;
            offState.motion = null;
        }

        private static void BuildMaterialSwap(MaterialSwapAction a, BuildCtx ctx) {
            if (a.targetRenderer == null || a.toMaterial == null) return;
            string baseName = Sanitize(a.name ?? a.targetRenderer.name + "_MatSwap");
            string paramName = !string.IsNullOrEmpty(ctx.MenuParameterName) ? ctx.MenuParameterName : $"Amity/Menu/{baseName}";
            var layer = ctx.Fx.NewLayer($"Amity {baseName} Swap");
            var onState = layer.NewState("Swap");
            var offState = layer.NewState("Revert");
            layer.stateMachine.defaultState = offState;

            // parameter (use provided MenuParameterName if present)
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(ctx.Fx, paramName, AnimatorControllerParameterType.Bool);
            var toOn = layer.stateMachine.AddAnyStateTransition(onState); toOn.hasExitTime = false; toOn.duration = 0; toOn.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toOff = layer.stateMachine.AddAnyStateTransition(offState); toOff.hasExitTime = false; toOff.duration = 0; toOff.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);

            var onClip = new AnimationClip { name = $"swap_{baseName}" };
            var offClip = new AnimationClip { name = $"revert_{baseName}" };
            foreach (var r in EnumerateRenderers(a, ctx.AvatarRoot)) {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) {
                    if (a.fromMaterial != null && mats[i] != a.fromMaterial) continue;
                    string path = AmityMenuUtils.RelativePath(ctx.AvatarRoot, r.transform);
                    var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"m_Materials.Array.data[{i}]" };
                    var onKeys = new[] { new ObjectReferenceKeyframe { time = 0f, value = a.toMaterial } };
                    AnimationUtility.SetObjectReferenceCurve(onClip, binding, onKeys);
                    if (a.fromMaterial != null) {
                        var offKeys = new[] { new ObjectReferenceKeyframe { time = 0f, value = a.fromMaterial } };
                        AnimationUtility.SetObjectReferenceCurve(offClip, binding, offKeys);
                    }
                }
            }
            AssetDatabase.AddObjectToAsset(onClip, ctx.AssetContainer);
            AssetDatabase.AddObjectToAsset(offClip, ctx.AssetContainer);
            onState.motion = onClip; offState.motion = offClip;
        }

        private static void BuildAnimationSlot(AnimationSlotAction a, BuildCtx ctx) {
            if (a.clip == null) return;
            string baseName = Sanitize(a.name ?? a.slotName ?? "AnimSlot");
            string paramName = !string.IsNullOrEmpty(ctx.MenuParameterName) ? ctx.MenuParameterName : $"Amity/Menu/{baseName}";
            if (string.IsNullOrEmpty(ctx.MenuParameterName)) {
                AmityMenuUtils.CreateOrGetVRCParameter(ctx.Parameters, paramName, VRCExpressionParameters.ValueType.Bool, 0, true, true);
            }
            var animParam = AmityMenuUtils.CreateOrGetAnimatorParameter(ctx.Fx, paramName, AnimatorControllerParameterType.Bool);

            var layer = ctx.Fx.NewLayer($"Amity {baseName} Clip");
            var playState = layer.NewState("Play");
            var idleState = layer.NewState("Idle");
            layer.stateMachine.defaultState = idleState;
            var toPlay = layer.stateMachine.AddAnyStateTransition(playState); toPlay.hasExitTime = false; toPlay.duration = 0; toPlay.AddCondition(AnimatorConditionMode.If, 0, animParam.name);
            var toIdle = layer.stateMachine.AddAnyStateTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = 0; toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, animParam.name);
            playState.motion = a.clip;
        }

        private static IEnumerable<Renderer> EnumerateRenderers(MaterialPropertyAction a, Transform root) {
            if (a.applyToAllRenderers) return root.GetComponentsInChildren<Renderer>(true);
            return a.targetRenderer != null ? new[] { a.targetRenderer } : Array.Empty<Renderer>();
        }
        private static IEnumerable<Renderer> EnumerateRenderers(MaterialSwapAction a, Transform root) {
            if (a.applyToAllRenderers) return root.GetComponentsInChildren<Renderer>(true);
            return a.targetRenderer != null ? new[] { a.targetRenderer } : Array.Empty<Renderer>();
        }

        private static void SetMatFloat(AnimationClip clip, string path, string prop, float value) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var curve = new AnimationCurve(); curve.AddKey(0f, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
        private static void SetMatColor(AnimationClip clip, string path, string prop, Color value) {
            // Unity color is 4 curves r,g,b,a
            var r = new AnimationCurve(); r.AddKey(0f, value.r);
            var g = new AnimationCurve(); g.AddKey(0f, value.g);
            var b = new AnimationCurve(); b.AddKey(0f, value.b);
            var a = new AnimationCurve(); a.AddKey(0f, value.a);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.r" }, r);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.g" }, g);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.b" }, b);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.a" }, a);
        }
        private static void SetMatVector(AnimationClip clip, string path, string prop, Vector4 value) {
            var x = new AnimationCurve(); x.AddKey(0f, value.x);
            var y = new AnimationCurve(); y.AddKey(0f, value.y);
            var z = new AnimationCurve(); z.AddKey(0f, value.z);
            var w = new AnimationCurve(); w.AddKey(0f, value.w);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.x" }, x);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.y" }, y);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.z" }, z);
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}.w" }, w);
        }
        private static void SetMatTexture(AnimationClip clip, string path, string prop, Texture value) {
            var binding = new EditorCurveBinding { path = path, type = typeof(Renderer), propertyName = $"material.{prop}" };
            var keys = new[] { new ObjectReferenceKeyframe { time = 0f, value = value } };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
    }
}