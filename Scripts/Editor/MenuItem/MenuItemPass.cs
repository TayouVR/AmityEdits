using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using org.Tayou.AmityEdits.Actions;
using org.Tayou.AmityEdits.Actions.Editor;
using org.Tayou.AmityEdits.Actions.Editor.Builders;
using org.Tayou.AmityEdits.EditorUtils;

namespace org.Tayou.AmityEdits.MenuItem {
    public class MenuItemPass {
        private readonly BuildContext _buildContext;

        public MenuItemPass(BuildContext context) {
            _buildContext = context;
        }

        public void Process() {
            var baseAvatarObject = _buildContext.AvatarRootObject;
            var avatarDescriptor = _buildContext.VRChatAvatarDescriptor();
            MenuItem[] menuItems = baseAvatarObject.GetComponentsInChildren<MenuItem>(true);
            
            Debug.Log($"The Menu Item pass is processing {menuItems?.Length ?? 0} menu items");
            if (menuItems == null || menuItems.Length == 0) return;

            // Ensure we operate on duplicated descriptor assets
            AmityMenuUtils.EnsureDescriptorAssetsDuplicated(avatarDescriptor, out var vrcParameters, out var rootMenu);
            var fxController = AmityMenuUtils.EnsureFxController(avatarDescriptor);

            // Build actions directly using NDMF BuildContext
            foreach (var item in menuItems) {
                if (item == null) continue;
                
                Debug.Log($"Building menu item: {item.name}");
                
                // Append control to appropriate menu
                VRCExpressionsMenu targetMenu = null;
                if (item.pathMethod == PathMethod.Parent) {
                    targetMenu = item.parentMenu != null ? item.parentMenu : rootMenu;
                } else {
                    targetMenu = AmityMenuUtils.GetOrCreateMenuByPath(rootMenu, item.menuPath);
                }
                if (item.vrcMenuControl != null) {
                    AmityMenuUtils.AppendControl(targetMenu, item.vrcMenuControl);
                }

                // Collect and ensure all parameters exist
                var parametersToEnsure = new List<(string name, VRCExpressionParameters.ValueType expType, AnimatorControllerParameterType animType)>();
                
                // Main parameter
                if (item.vrcMenuControl?.parameter != null && !string.IsNullOrEmpty(item.vrcMenuControl.parameter.name)) {
                    // For Toggle/Button it's usually bool, but for Radial/Puppet it's float
                    var type = VRCExpressionParameters.ValueType.Bool;
                    var animType = AnimatorControllerParameterType.Bool;
                    
                    if (item.vrcMenuControl.type == VRCExpressionsMenu.Control.ControlType.RadialPuppet ||
                        item.vrcMenuControl.type == VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet ||
                        item.vrcMenuControl.type == VRCExpressionsMenu.Control.ControlType.FourAxisPuppet) {
                        
                        // User requested: "main is always a bool (in the vrc list anyways, in the the animator it should stay a float, for use in blendtrees.)"
                        // This applies to Puppets.
                        type = VRCExpressionParameters.ValueType.Bool;
                        animType = AnimatorControllerParameterType.Float;
                    }
                    parametersToEnsure.Add((item.vrcMenuControl.parameter.name, type, animType));
                }
                
                // Sub parameters (always float)
                if (item.vrcMenuControl?.subParameters != null) {
                    foreach (var subParam in item.vrcMenuControl.subParameters) {
                        if (subParam != null && !string.IsNullOrEmpty(subParam.name)) {
                            parametersToEnsure.Add((subParam.name, VRCExpressionParameters.ValueType.Float, AnimatorControllerParameterType.Float));
                        }
                    }
                }

                foreach (var (pName, expType, animType) in parametersToEnsure) {
                    AmityMenuUtils.CreateOrGetVRCParameter(vrcParameters, pName, expType, 0, true, true);
                    //AmityMenuUtils.CreateOrGetAnimatorParameter(fxController, pName, animType);
                }

                // Map ParameterSelection to actual parameter names
                string[] paramNames = new string[5]; // Main, Sub1, Sub2, Sub3, Sub4
                paramNames[0] = item.vrcMenuControl?.parameter?.name;
                if (item.vrcMenuControl?.subParameters != null) {
                    for (int i = 0; i < Math.Min(item.vrcMenuControl.subParameters.Length, 4); i++) {
                        paramNames[i + 1] = item.vrcMenuControl.subParameters[i]?.name;
                    }
                }

                // Build animator logic for actions using the selected parameter
                if (item.actions != null) {
                    foreach (var action in item.actions) {
                        if (action == null) continue;
                        try {
                            action.BuildFor(_buildContext, item.vrcMenuControl);
                        } catch (System.Exception e) {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}