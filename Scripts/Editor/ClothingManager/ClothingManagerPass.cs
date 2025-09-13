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
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace org.Tayou.AmityEdits {
    
    public class ClothingManagerPass {

        private static string MENU_NAME = "Clothing";
        private static string OUTFITS_MENU_NAME = "Outfits";
        private static string ITEMS_MENU_NAME = "Individual Items";
        private static string PARAMETER_PREFIX = "Amity/Clothing";
        
        private readonly BuildContext _buildContext;

        public ClothingManagerPass(BuildContext context) {
            _buildContext = context;
        }

        private void GenerateParameter(ClothingItem clothingItem, AnimatorController fxController, List<AnimatorControllerParameter> parameters, VRCExpressionParameters vrcParameters) {
            string parameterName = clothingItem.actionMethod == ItemActionMethod.Parameter 
                                   && !String.IsNullOrEmpty(clothingItem.parameterName) 
                ? clothingItem.parameterName 
                : $"{PARAMETER_PREFIX}/{clothingItem.name}";
            
            var existingParameter = parameters.Find(param => param.name == parameterName);
            if (existingParameter != null) {
                clothingItem.ParameterReference = existingParameter;
            } else {
                clothingItem.ParameterReference = fxController.NewParameter(parameterName,
                    AnimatorControllerParameterType.Float);
                parameters.Add(clothingItem.ParameterReference);
            }

            
            var existingVrcParameter = vrcParameters.FindParameter(parameterName);
            if (existingVrcParameter == null) {
                existingVrcParameter = new VRCExpressionParameters.Parameter() {
                    name = parameterName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    networkSynced = true,
                    defaultValue = 0,
                };
                var params1 = vrcParameters.parameters.ToList();
                params1.Add(existingVrcParameter);
                vrcParameters.parameters = params1.ToArray();
                
            }
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

            var vrcParameterList = avatarDescriptor.expressionParameters;
            var clothingMenu = CreateClothingMenu(avatarDescriptor, out var clothingItemMenu, out var outfitMenu);;
            
            var fxController = (AnimatorController)avatarDescriptor.baseAnimationLayers.FirstOrDefault(animatorLayer =>
                animatorLayer.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController;

            AnimatorControllerLayer driverLayer = fxController.NewLayer("Clothing Driver");
            AnimatorControllerLayer toggleLayer = fxController.NewLayer("Clothing Toggles");

            List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
            
            foreach (var clothingItem in clothingItemComponents) {
                GenerateParameter(clothingItem, fxController, parameters, vrcParameterList);
                
                // generate Clothing Item State
                AnimatorState state = driverLayer.NewState(clothingItem.name);
                driverLayer.stateMachine.AddAnyStateTransition(state);
                state.Drives(clothingItem.ParameterReference, 1);
                
                // handle incompatabilities
                foreach (var incompatibleItem in clothingItem.incompatibilities) {
                    state.Drives(incompatibleItem.ParameterReference, 0);
                }
                
                // build animation
                switch (clothingItem.actionMethod) {
                    case ItemActionMethod.ObjectToggle:
                        GenerateObjectToggle(clothingItem.objectToToggle);
                        break;
                    case ItemActionMethod.AmityAction:
                        // amity actions don't exist yet
                        break;
                    case ItemActionMethod.Parameter:
                    case ItemActionMethod.Animation:
                    default:
                        break;
                }
                
                
                // generate Direct BlendTree
                var directTreeState = toggleLayer.NewDirectTreeState(out var directBlendTree, fxController, "Clothing Toggles");

                CreateMotionForClothingItem(clothingItem, directBlendTree);
                
                // build menus
                BuildMenu(clothingItem, clothingItemMenu);
            }
            
            AssetDatabase.SaveAssets();
            
            // generate Outfit States
            foreach (var outfit in outfitComponents) {
                var state = driverLayer.NewState(outfit.name);

                foreach (var clothingItem in outfit.ClothingItems) {
                    state.Drives(clothingItem.ParameterReference, 1);
                    foreach (var incompatibleItem in clothingItem.incompatibilities) {
                        state.Drives(incompatibleItem.ParameterReference, 0);
                    }
                }
            }
            
            Debug.Log("The Clothing Manager pass has finished. \n" +
                      $"Created {parameters.Count} parameters for clothing items");
        }

        private void BuildMenu(ClothingItem clothingItem, VRCExpressionsMenu clothingItemMenu) {
            clothingItemMenu.controls.Add(new VRCExpressionsMenu.Control() {
                name = clothingItem.name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter() {
                    name = clothingItem.ParameterReference.name,
                } ,
            });
        }

        private VRCExpressionsMenu CreateClothingMenu(VRCAvatarDescriptor avatarDescriptor, out VRCExpressionsMenu clothingItemMenu, out VRCExpressionsMenu outfitMenu) {
            var mainMenu = avatarDescriptor.expressionsMenu;

            if (mainMenu == null) {
                mainMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            }

            var clothingMenu = CreateSubMenu(mainMenu, MENU_NAME);
            clothingItemMenu = CreateSubMenu(clothingMenu, ITEMS_MENU_NAME);
            outfitMenu = CreateSubMenu(clothingMenu, OUTFITS_MENU_NAME);

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
                menuControl.subMenu = newMenu;
            } else {
                newMenu = menuControl.subMenu;
            }

            return newMenu;
        }

        private static void CreateMotionForClothingItem(ClothingItem clothingItem, BlendTree directBlendTree) {
            // var motion = new BlendTree {
            //     name = clothingItem.name
            // };
            // motion.AddChild(clothingItem.animation);
            // AssetDatabase.AddObjectToAsset(motion, directBlendTree);
            // directBlendTree.AddChild(motion, clothingItem.ParameterReference.defaultFloat);
            directBlendTree.AddChild(clothingItem.animation, clothingItem.ParameterReference.defaultFloat);
        }

        private void GenerateObjectToggle(GameObject gameObject) {
            
        }
    }
}