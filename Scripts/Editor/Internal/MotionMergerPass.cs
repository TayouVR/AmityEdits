using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace org.Tayou.AmityEdits.Internal {
    public class MotionMergerPass : Pass<MotionMergerPass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.MergeMotions";
        public override string DisplayName => "Merge Motions";
        
        public const string AlwaysOne = "Amity/Internal/One";
        internal const string BlendTreeLayerName = "Amity: Merge Blend Tree";
        
        private AnimatorServicesContext _asc;
        private Dictionary<int, VirtualBlendTree> _rootBlendTrees;
        private HashSet<string> _parameterNames;

        protected override void Execute(BuildContext ctx) {
            _asc = ctx.Extension<AnimatorServicesContext>();
            _rootBlendTrees =  new Dictionary<int, VirtualBlendTree>();
            _parameterNames = new HashSet<string>();

            var fx = _asc.ControllerContext.Controllers[VRCAvatarDescriptor.AnimLayerType.FX];

            foreach (var component in
                     ctx.AvatarRootObject.GetComponentsInChildren<MotionMerger>(true))
            {
                ErrorReport.WithContextObject(component, () => ProcessComponent(ctx, component));
            }
            
            // always add the ALWAYS_ONE parameter
            fx.Parameters = fx.Parameters.SetItem(AlwaysOne, new AnimatorControllerParameter()
            {
                name = AlwaysOne,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1
            });

            foreach (var name in _parameterNames)
            {
                if (fx.Parameters.TryGetValue(name, out var existingParameter))
                {
                    if (existingParameter.type != AnimatorControllerParameterType.Float)
                    {
                        existingParameter = new AnimatorControllerParameter
                        {
                            type = AnimatorControllerParameterType.Float,
                            name = name,
                            defaultFloat = existingParameter.type switch
                            {
                                AnimatorControllerParameterType.Bool => existingParameter.defaultBool ? 1 : 0,
                                AnimatorControllerParameterType.Int => existingParameter.defaultInt,
                                _ => 0
                            }
                        };
                    }
                }
                else
                {
                    existingParameter = new AnimatorControllerParameter
                    {
                        name = name,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 0.0f
                    };
                }

                fx.Parameters = fx.Parameters.SetItem(name, existingParameter);
            }
        }
        


        public void ProcessComponent(BuildContext context, MotionMerger component) {
            AnimatorServicesContext asc = context.Extension<AnimatorServicesContext>();
            var virtualMotion = asc.ControllerContext.GetVirtualizedMotion(component);
            var parameterNames = new HashSet<string>();
            
            
            var fx = asc.ControllerContext.Controllers[component.LayerType];
            // always add the ALWAYS_ONE parameter
            fx.Parameters = fx.Parameters.SetItem(AlwaysOne, new AnimatorControllerParameter {
                name = AlwaysOne,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1
            });

            if (virtualMotion == null) {
                //ErrorReport.ReportError(Localization.L, ErrorSeverity.NonFatal, "error.merge_blend_tree.missing_tree");
                return;
            }

            var rootBlend = GetRootBlendTree(component.LayerPriority, component.LayerType);
            
            rootBlend.Children = rootBlend.Children.Add(new() {
                Motion = virtualMotion,
                DirectBlendParameter = AlwaysOne,
                Threshold = 1,
                CycleOffset = 1,
                TimeScale = 1,
            });

            foreach (var asset in virtualMotion.AllReachableNodes()) {
                if (asset is VirtualBlendTree bt2) {
                    if (!string.IsNullOrEmpty(bt2.BlendParameter) && bt2.BlendType != BlendTreeType.Direct) {
                        parameterNames.Add(bt2.BlendParameter);
                    }

                    if (bt2.BlendType != BlendTreeType.Direct && bt2.BlendType != BlendTreeType.Simple1D) {
                        if (!string.IsNullOrEmpty(bt2.BlendParameterY)) {
                            parameterNames.Add(bt2.BlendParameterY);
                        }
                    }

                    if (bt2.BlendType == BlendTreeType.Direct) {
                        foreach (var childMotion in bt2.Children) {
                            if (!string.IsNullOrEmpty(childMotion.DirectBlendParameter)) {
                                parameterNames.Add(childMotion.DirectBlendParameter);
                            }
                        }
                    }
                }
            }

            foreach (var name in parameterNames) {
                if (fx.Parameters.TryGetValue(name, out var existingParameter)) {
                    if (existingParameter.type != AnimatorControllerParameterType.Float) {
                        existingParameter = new AnimatorControllerParameter {
                            type = AnimatorControllerParameterType.Float,
                            name = name,
                            defaultFloat = existingParameter.type switch {
                                AnimatorControllerParameterType.Bool => existingParameter.defaultBool ? 1 : 0,
                                AnimatorControllerParameterType.Int => existingParameter.defaultInt,
                                _ => 0
                            }
                        };
                    }
                }
                else
                {
                    existingParameter = new AnimatorControllerParameter
                    {
                        name = name,
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 0.0f
                    };
                }

                fx.Parameters = fx.Parameters.SetItem(name, existingParameter);
            }
        }

        private VirtualBlendTree GetRootBlendTree(int layerPriority = int.MinValue, VRCAvatarDescriptor.AnimLayerType layerType = VRCAvatarDescriptor.AnimLayerType.FX) {
            if (_rootBlendTrees.ContainsKey(layerPriority)) return _rootBlendTrees[layerPriority];

            var fx = _asc.ControllerContext.Controllers[layerType];
            var controller = fx.AddLayer(new LayerPriority(layerPriority), $"{BlendTreeLayerName} - {layerPriority}");
            var stateMachine = controller.StateMachine;
            if (fx == null)
            {
                throw new Exception("FX layer not found");
            }
            
            _rootBlendTrees[layerPriority] = VirtualBlendTree.Create("Root");
            var state = stateMachine.AddState("State", _rootBlendTrees[layerPriority]);
            stateMachine.DefaultState = state;
            state.WriteDefaultValues = true;
            
            _rootBlendTrees[layerPriority].BlendType = BlendTreeType.Direct;
            _rootBlendTrees[layerPriority].BlendParameter = AlwaysOne;
            
            return _rootBlendTrees[layerPriority];
        }
    }
}