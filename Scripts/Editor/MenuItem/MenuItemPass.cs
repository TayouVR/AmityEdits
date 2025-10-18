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
            Debug.Log("The Menu Item pass is running");
            var baseAvatarObject = _buildContext.AvatarRootObject;
            var avatarDescriptor = _buildContext.VRChatAvatarDescriptor();
            MenuItem[] menuItems = baseAvatarObject.GetComponentsInChildren<MenuItem>(true);
            if (menuItems == null || menuItems.Length == 0) return;

            // Ensure we operate on duplicated descriptor assets
            AmityMenuUtils.EnsureDescriptorAssetsDuplicated(avatarDescriptor, out var vrcParameters, out var rootMenu);
            var fxController = AmityMenuUtils.EnsureFxController(avatarDescriptor);

            // Build actions directly using NDMF BuildContext
            foreach (var item in menuItems) {
                if (item == null) continue;
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

                // Determine/control parameter name from the menu control, ensure it exists (bool) in expressions and animator
                string menuParamName = item?.vrcMenuControl?.parameter?.name;
                if (!string.IsNullOrEmpty(menuParamName)) {
                    AmityMenuUtils.CreateOrGetVRCParameter(vrcParameters, menuParamName, VRCExpressionParameters.ValueType.Bool, 0, true, true);
                    AmityMenuUtils.CreateOrGetAnimatorParameter(fxController, menuParamName, AnimatorControllerParameterType.Bool);
                }

                // Build animator logic for actions using the same parameter name when provided
                if (item.actions != null) {
                    foreach (var action in item.actions) {
                        try {
                            action?.BuildFor(_buildContext, menuParamName);
                        } catch (System.Exception e) {
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }
    }
}