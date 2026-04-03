// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2023 Tayou <git@tayou.org>
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
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using org.Tayou.AmityEdits.EditorUtils;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits {
    
    public class ReorderMenusPass : Pass<ReorderMenusPass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.ReorderMenusPass";
        public override string DisplayName => "Reorder Menus Pass";
        
        // private static Texture2D _moreIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
        //     "Packages/nadena.dev.modular-avatar/Runtime/Icons/Icon_More_A.png"
        // );
        private static readonly string[] nextPageButtonNames = {"Next", "More", "Next Page"};

        protected override void Execute(BuildContext ctx) {
            var avatarDescriptor = ctx.VRChatAvatarDescriptor();
            var rootMenu = avatarDescriptor.expressionsMenu;
            var components = avatarDescriptor.GetComponentsInChildren<ReorderMenus>(true);
            var menuOptionsComponents = avatarDescriptor.GetComponentsInChildren<MenuOptions>(true);
            
            var nextText = null as string;
            var nextIcon = null as Texture2D;
            
            if (menuOptionsComponents.Length > 0) {
                var menuOptions = menuOptionsComponents[0];
                nextText = menuOptions.nextPageButtonTitle;
                nextIcon = menuOptions.nextPageButtonIcon;

                if (menuOptionsComponents.Length > 1) {
                    Debug.LogWarning(
                        $"Multiple MenuOptions components found. Using first one: {menuOptionsComponents[0].name}");
                }
            }
            
            //if (components.Length == 0 || rootMenu == null) return;

            var menuOperations = components.SelectMany(a => a.MenuOperations);
            // foreach (var menuOperation in menuOperations) {
            //     // TODO: traverse menu tree and find target menu as well as parent.
            //     //  make sure target menu isn't still referenced at original location, and insert at parent
            //     var sourceMenu = GetSourceMenu(menuOperation.SourceMenu);
            //     
            //     var targetMenu = GetTargetMenu(menuOperation.TargetMenu, rootMenu);
            //     
            //     // TODO: use your brain
            //     targetMenu.AddMenuControl(new VRCExpressionsMenu.Control() {
            //         //name = menuOperation.Name,
            //         type = VRCExpressionsMenu.Control.ControlType.SubMenu,
            //         subMenu = sourceMenu,
            //     });
            //     
            //     
            // }
            
            
            
            GetVrcfFields(avatarDescriptor, out string vrcfNextText, out var vrcfNextIcon );
            
            nextText ??= vrcfNextText ?? "Next";
            nextIcon ??= vrcfNextIcon;
            
            if (rootMenu != null) {
                FixMenu(rootMenu, nextText, nextIcon, new HashSet<VRCExpressionsMenu>());
            }
        }

        private void FixMenu(VRCExpressionsMenu menu, string nextText, Texture2D nextIcon, HashSet<VRCExpressionsMenu> visited) {
            if (menu == null || visited.Contains(menu)) return;
            visited.Add(menu);
            
            Debug.Log($"[FixMenus] Parsing menu {menu.name}");

            // Important: we must recurse into existing submenus BEFORE we might move them to a next page
            // to ensure they are also fixed.
            for (int i = 0; i < menu.controls.Count; i++) {
                var control = menu.controls[i];
                if (control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null) {
                    FixMenu(control.subMenu, nextText, nextIcon, visited);
                }
            }

            if (menu.controls.Count > 8) {
                VRCExpressionsMenu nextPageMenu = null;
                
                // Look for existing "Next" or "More" menu
                for (int i = 0; i < menu.controls.Count; i++) {
                    var control = menu.controls[i];
                    if (control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && 
                        (nextPageButtonNames.Contains(control.name) || (!string.IsNullOrEmpty(nextText) && control.name == nextText))) {
                        nextPageMenu = control.subMenu;
                        
                        menu.controls.RemoveAt(i);
                        break;
                    }
                }

                if (nextPageMenu == null || nextPageMenu == menu) {
                    nextPageMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    nextPageMenu.name = menu.name + " (Next)";
                    if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(menu))) {
                        AssetDatabase.AddObjectToAsset(nextPageMenu, menu);
                    }
                }

                // Move excess controls to the next page
                while (menu.controls.Count > 7) {
                    nextPageMenu.controls.Add(menu.controls[7]);
                    menu.controls.RemoveAt(7);
                }

                // Add the next page control
                menu.controls.Add(new VRCExpressionsMenu.Control {
                    name = string.IsNullOrEmpty(nextText) ? "Next" : nextText,
                    icon = nextIcon,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = nextPageMenu
                });
                
                // Recurse into the next page menu as it might now have > 8 controls
                visited.Remove(nextPageMenu); 
                FixMenu(nextPageMenu, nextText, nextIcon, visited);
            }
        }

        private VRCExpressionsMenu GetTargetMenu(MenuLocation menuOperationTargetMenu, VRCExpressionsMenu rootMenu) {
            throw new System.NotImplementedException();
        }

        private VRCExpressionsMenu GetSourceMenu(MenuLocation menuOperationSourceMenu) {
            throw new System.NotImplementedException();
        }

        // Use VRCFuryFeatureUtils to look for OverrideMenuSettings feature on the avatar
        private void GetVrcfFields(VRCAvatarDescriptor avatarDescriptor, out string vrcfNextText, out Texture2D vrcfNextIcon) {
            vrcfNextText = null;
            vrcfNextIcon = null;
            
            var overrideMenuSettings = VRCFuryFeatureUtils.GetOverrideMenuSettingsModel(avatarDescriptor.gameObject, true);
            if (overrideMenuSettings != null) {
                if (VRCFuryFeatureUtils.TryReadOverrideMenuSettings(overrideMenuSettings, out vrcfNextText, out vrcfNextIcon)) {
                    Debug.Log($"[Amity] VRCFury OverrideMenuSettings detected: nextText='{vrcfNextText}', nextIcon={(vrcfNextIcon != null ? vrcfNextIcon.name : "<none>")}");
                } else {
                    Debug.Log("[Amity] VRCFury OverrideMenuSettings detected but could not parse fields");
                }
            } else {
                Debug.Log("[Amity] No VRCFury OverrideMenuSettings feature found on this avatar");
            }
        }
    }
}