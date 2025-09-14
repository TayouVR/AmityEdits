// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2025 Tayou <git@tayou.org>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits {
    
    public class ClothingManagerPass {

        private const string ONE_PARAMETER_NAME = "Amity/Internal/One";
        private const string MENU_NAME = "Clothing";
        private const string OUTFITS_MENU_NAME = "Outfits";
        private const string ITEMS_MENU_NAME = "Individual Items";
        private const string PARAMETER_PREFIX = "Amity/Clothing";
        
        private readonly BuildContext _buildContext;
        
        // during build data. will be null outside of build
        AnimationClip _emptyClip;

        public ClothingManagerPass(BuildContext context) {
            _buildContext = context;
        }
        
        public void Process() {
            Debug.Log("The Clothing Manager pass is running");
            var baseAvatarObject = _buildContext.AvatarRootObject;
            var avatarDescriptor = _buildContext.VRChatAvatarDescriptor();
            ClothingItem[] clothingItemComponents = baseAvatarObject.GetComponentsInChildren<ClothingItem>(true);
            Outfit[] outfitComponents = baseAvatarObject.GetComponentsInChildren<Outfit>(true);

            if (clothingItemComponents.Length == 0 && outfitComponents.Length == 0) {
                Debug.Log("The Clothing Manager didn't find any components and is returning");
                return;
            }
            
            EnsureDescriptorAssetsDuplicated(avatarDescriptor, out var vrcParameterList, out var rootMenu);
            var clothingMenu = CreateClothingMenu(rootMenu, out var clothingItemMenu, out var outfitMenu);

            
            var fxController = (AnimatorController)avatarDescriptor.baseAnimationLayers.FirstOrDefault(animatorLayer =>
                animatorLayer.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController;
            
            // parameter that is always 1 for direct blend trees
            var oneParam = CreateAnimatorParameter(ONE_PARAMETER_NAME, fxController, 1);
            
            // empty animation clip for empty states
            _emptyClip = new AnimationClip();

            AnimatorControllerLayer driverLayer = fxController.NewLayer("Clothing Driver");
            AnimatorControllerLayer toggleLayer = fxController.NewLayer("Clothing Toggles");
            
            // generate Direct BlendTree
            var directTreeState = toggleLayer.NewDirectTreeState(out var directBlendTree, fxController, "Clothing Toggles");
            
            // generate all parameters before doing animator and menus
            foreach (var clothingItem in clothingItemComponents) {
                GenerateParameter(clothingItem, fxController, vrcParameterList);
            }

            foreach (var clothingItem in clothingItemComponents) {
                
                // generate Clothing Item State
                AnimatorState onState = driverLayer.NewState(clothingItem.name);
                var onTransition = driverLayer.stateMachine.AddAnyStateTransition(onState);
                onTransition.AddCondition(AnimatorConditionMode.Greater, 0.5f, clothingItem.ParameterReference.name);
                onTransition.AddCondition(AnimatorConditionMode.Less, 0.5f, clothingItem.ParameterShadowReference.name);
                onTransition.duration = 0f;
                onTransition.hasExitTime = false;
                onState.Drives(clothingItem.ParameterReference, 1);
                onState.Drives(clothingItem.ParameterShadowReference, 1);
                onState.motion = _emptyClip;
                
                // handle incompatabilities
                foreach (var incompatibleItem in clothingItem.incompatibilities) {
                    onState.Drives(incompatibleItem.ParameterReference, 0);
                    onState.Drives(incompatibleItem.ParameterShadowReference, 0);
                }
                
                AnimatorState offState = driverLayer.NewState(clothingItem.name);
                var offTransition = driverLayer.stateMachine.AddAnyStateTransition(offState);
                offTransition.AddCondition(AnimatorConditionMode.Less, 0.5f, clothingItem.ParameterReference.name);
                offTransition.AddCondition(AnimatorConditionMode.Greater, 0.5f, clothingItem.ParameterShadowReference.name);
                offTransition.duration = 0f;
                offTransition.hasExitTime = false;
                offState.Drives(clothingItem.ParameterShadowReference, 0);
                offState.motion = _emptyClip;
                
                // build animation
                switch (clothingItem.actionMethod) {
                    case ItemActionMethod.ObjectToggle:
                        GenerateObjectToggle(clothingItem, baseAvatarObject);
                        break;
                    case ItemActionMethod.AmityAction:
                        // amity actions don't exist yet
                        break;
                    case ItemActionMethod.Parameter:
                    case ItemActionMethod.Animation:
                    default:
                        break;
                }

                CreateMotionForClothingItem(clothingItem, directBlendTree);
                
                // build menus
                BuildMenu(clothingItem, clothingItemMenu);
            }
            
            AssetDatabase.SaveAssets();
            
            // generate Outfit States
            foreach (var outfit in outfitComponents) {
                outfit.ParameterReference = CreateAnimatorParameter($"{PARAMETER_PREFIX}/Outfit/{outfit.name}", fxController);
                outfit.VRChatParameterReference = CreateVrcParameter($"{PARAMETER_PREFIX}/Outfit/{outfit.name}", avatarDescriptor.expressionParameters);
                var state = driverLayer.NewState(outfit.name);
                state.motion = _emptyClip;
                var transition = driverLayer.stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Greater, 0.5f, outfit.ParameterReference.name);
                transition.duration = 0f;
                transition.hasExitTime = false;
                state.Drives(outfit.ParameterReference, 0);

                foreach (var clothingItem in outfit.clothingItems) {
                    if (clothingItem == null) continue;
                    state.Drives(clothingItem.ParameterReference, 1);
                    foreach (var incompatibleItem in clothingItem.incompatibilities) {
                        state.Drives(incompatibleItem.ParameterReference, 0);
                    }
                }
                BuildMenu(outfit, outfitMenu);
            }
            
            Debug.Log("The Clothing Manager pass has finished. \n" +
                      $"Created {clothingItemComponents.Length} parameters for clothing items");
        }

        private void GenerateParameter(ClothingItem clothingItem, AnimatorController fxController, VRCExpressionParameters vrcParameters) {
            string parameterName = clothingItem.actionMethod == ItemActionMethod.Parameter 
                                   && !String.IsNullOrEmpty(clothingItem.parameterName) 
                ? clothingItem.parameterName 
                : $"{PARAMETER_PREFIX}/{clothingItem.name}";

            // animator parameter
            clothingItem.ParameterReference = CreateAnimatorParameter(parameterName, fxController, clothingItem.defaultState ? 1f : 0f);

            // animator shadow parameter
            clothingItem.ParameterShadowReference = CreateAnimatorParameter($"{PARAMETER_PREFIX}/Shadow/{clothingItem.name}", fxController, clothingItem.defaultState ? 1f : 0f);

            // vrchat parameter
            clothingItem.VRChatParameterReference = CreateVrcParameter(parameterName, vrcParameters, clothingItem.defaultState ? 1f : 0f);
        }

        private VRCExpressionParameters.Parameter CreateVrcParameter(string name, VRCExpressionParameters vrcParameters, float defaultValue = 0f) {
            var vrcParameter = vrcParameters.FindParameter(name);
            if (vrcParameter == null) {
                vrcParameter = new VRCExpressionParameters.Parameter() {
                    name = name,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    networkSynced = true,
                    defaultValue = defaultValue,
                };
                var params1 = vrcParameters.parameters.ToList();
                params1.Add(vrcParameter);
                vrcParameters.parameters = params1.ToArray();
            }
            return vrcParameter;
        }

        private AnimatorControllerParameter CreateAnimatorParameter(string name, AnimatorController fxController, float defaultValue = 0f) {
            var animatorParameter = fxController.parameters.ToList().Find(param => param.name == name);
            if (animatorParameter == null) {
                animatorParameter = new AnimatorControllerParameter {
                    name = name,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = defaultValue,
                    defaultBool = defaultValue > 0.5f,
                    defaultInt = (int)Math.Round(defaultValue),
                };
            
                fxController.AddParameter(animatorParameter);
            }

            return animatorParameter;
        }

        private void BuildMenu(ClothingItem clothingItem, VRCExpressionsMenu parentMenu) {
            parentMenu.controls.Add(new VRCExpressionsMenu.Control() {
                name = clothingItem.name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter() {
                    name = clothingItem.ParameterReference.name,
                },
                value = 1,
            });
        }

        private void BuildMenu(Outfit outfit, VRCExpressionsMenu parentMenu) {
            Debug.Log("awaaaaa" + outfit.ParameterReference.name);
            parentMenu.controls.Add(new VRCExpressionsMenu.Control() {
                name = outfit.name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter() {
                    name = outfit.ParameterReference.name,
                },
                value = 1,
            });
        }

        private VRCExpressionsMenu CreateClothingMenu(VRCExpressionsMenu rootMenu, out VRCExpressionsMenu clothingItemMenu, out VRCExpressionsMenu outfitMenu) {
            var mainMenu = rootMenu;

            if (mainMenu == null) {
                Debug.Log("An error occurred while creating the clothing menu. No root menu was found.\n" +
                          "This should have been automatically created on the build copy.");
            }

            var clothingMenu = CreateSubMenu(mainMenu, MENU_NAME);
            outfitMenu = CreateSubMenu(clothingMenu, OUTFITS_MENU_NAME);
            clothingItemMenu = CreateSubMenu(clothingMenu, ITEMS_MENU_NAME);

            return clothingMenu;
        }

        private static VRCExpressionsMenu CreateSubMenu(VRCExpressionsMenu parentMenu, string name) {
            var menuControl = parentMenu.controls.Find(control => control.name == name && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu);
            if (menuControl == null) {
                menuControl = new VRCExpressionsMenu.Control {
                    name = name,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                };
                parentMenu.controls.Add(menuControl);
            }

            VRCExpressionsMenu newMenu;
            if (menuControl.subMenu == null) {
                newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                newMenu.name = name;
                menuControl.subMenu = newMenu;
            } else {
                newMenu = menuControl.subMenu;
            }

            return newMenu;
        }

        private void CreateMotionForClothingItem(ClothingItem clothingItem, BlendTree directBlendTree) {
            var offMotion = clothingItem.offAnimation ?? _emptyClip;
            var motion = new BlendTree {
                name = clothingItem.name,
                blendParameter = clothingItem.ParameterReference.name,
            };
            motion.AddChild(offMotion, 0f);
            motion.AddChild(clothingItem.onAnimation, 1f);
            AssetDatabase.AddObjectToAsset(motion, directBlendTree);
            directBlendTree.AddChild(motion, ONE_PARAMETER_NAME);
            //directBlendTree.AddChild(clothingItem.animation, clothingItem.ParameterReference.name);
        }

        private void GenerateObjectToggle(ClothingItem clothingItem, GameObject baseAvatarObject) {
            var go = clothingItem.gameObject;
            var path = clothingItem.transform.GetHierarchyPath(baseAvatarObject.transform);

            // Create ON animation
            var onAnimation = new AnimationClip();
            onAnimation.name = $"on_{clothingItem.name}";
            var onCurve = new AnimationCurve();
            onCurve.AddKey(0f, 1f);
            var onBinding = new EditorCurveBinding {
                path = path,
                propertyName = "m_IsActive",
                type = typeof(GameObject)
            };
            AnimationUtility.SetEditorCurve(onAnimation, onBinding, onCurve);
            clothingItem.onAnimation = onAnimation;

            // Create OFF animation
            var offAnimation = new AnimationClip();
            offAnimation.name = $"off_{clothingItem.name}";
            var offCurve = new AnimationCurve();
            offCurve.AddKey(0f, 0f);
            var offBinding = new EditorCurveBinding {
                path = path,
                propertyName = "m_IsActive",
                type = typeof(GameObject)
            };
            AnimationUtility.SetEditorCurve(offAnimation, offBinding, offCurve);
            clothingItem.offAnimation = offAnimation;

            AssetDatabase.AddObjectToAsset(onAnimation, _buildContext.AssetContainer);
            AssetDatabase.AddObjectToAsset(offAnimation, _buildContext.AssetContainer);
        }

        private string GetRelativePath(Transform root, Transform target) {
            var path = new System.Text.StringBuilder();
            var current = target;

            while (current != null && current != root) {
                if (path.Length > 0) {
                    path.Insert(0, "/");
                }
                path.Insert(0, current.name);
                current = current.parent;
            }

            return path.ToString();
        }
        
        private static void EnsureDescriptorAssetsDuplicated(
            VRCAvatarDescriptor avatarDescriptor,
            out VRCExpressionParameters vrcParametersOut,
            out VRCExpressionsMenu expressionsMenuOut
        ) {
            // Parameters
            if (avatarDescriptor.expressionParameters == null) {
                avatarDescriptor.expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                avatarDescriptor.expressionParameters.name = "Parameters";
            } else {
                avatarDescriptor.expressionParameters = DuplicateParametersAsset(avatarDescriptor.expressionParameters);
            }
            vrcParametersOut = avatarDescriptor.expressionParameters;

            // Menu (deep-copy the entire tree)
            if (avatarDescriptor.expressionsMenu == null) {
                avatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                avatarDescriptor.expressionsMenu.name = "Root Menu";
            } else {
                avatarDescriptor.expressionsMenu = DuplicateMenuAssetDeep(avatarDescriptor.expressionsMenu);
            }
            expressionsMenuOut = avatarDescriptor.expressionsMenu;
        }

        private static VRCExpressionParameters DuplicateParametersAsset(VRCExpressionParameters original) {
            var dup = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            dup.name = original.name;
            if (original?.parameters != null) {
                var copied = new List<VRCExpressionParameters.Parameter>(original.parameters.Length);
                foreach (var p in original.parameters) {
                    if (p == null) continue;
                    copied.Add(new VRCExpressionParameters.Parameter {
                        name = p.name,
                        valueType = p.valueType,
                        defaultValue = p.defaultValue,
                        saved = p.saved,
                        networkSynced = p.networkSynced
                    });
                }
                dup.parameters = copied.ToArray();
            } else {
                dup.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
            }
            return dup;
        }

        private static VRCExpressionsMenu DuplicateMenuAssetDeep(VRCExpressionsMenu original) {
            var dup = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            dup.name = original.name;
            if (original == null) return dup;

            if (original.controls != null) {
                foreach (var c in original.controls) {
                    if (c == null) continue;
                    var nc = new VRCExpressionsMenu.Control {
                        name = c.name,
                        type = c.type,
                        icon = c.icon,
                        parameter = c.parameter != null ? new VRCExpressionsMenu.Control.Parameter { name = c.parameter.name } : null,
                        value = c.value,
                        style = c.style
                    };
                    // Copy sub-parameters (for 2-axis/radial) if present
                    if (c.subParameters != null && c.subParameters.Length > 0) {
                        var subParams = new VRCExpressionsMenu.Control.Parameter[c.subParameters.Length];
                        for (int i = 0; i < c.subParameters.Length; i++) {
                            var sp = c.subParameters[i];
                            subParams[i] = sp != null ? new VRCExpressionsMenu.Control.Parameter { name = sp.name } : null;
                        }
                        nc.subParameters = subParams;
                    }
                    // Recurse for submenus
                    if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null) {
                        nc.subMenu = DuplicateMenuAssetDeep(c.subMenu);
                    }
                    dup.controls.Add(nc);
                }
            }
            return dup;
        }
    }
}