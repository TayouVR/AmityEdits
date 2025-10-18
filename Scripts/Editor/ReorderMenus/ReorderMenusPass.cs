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
    
    public class ReorderMenusPass {
        
        private readonly BuildContext _buildContext;

        public ReorderMenusPass(BuildContext context) {
            _buildContext = context;
        }

        public void Process() {
            var avatarDescriptor = _buildContext.VRChatAvatarDescriptor();
            var rootMenu = avatarDescriptor.expressionsMenu;
            var components = avatarDescriptor.GetComponentsInChildren<ReorderMenus>(true);

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
            
            // TODO: deduplicate "next page" menus
            // TODO: make sure max menu size is respected (8 per page) 
            
            
            GetVRCFFields(avatarDescriptor, out string vrcfNextText, out var vrcfNextIcon );
            
            
            
        }

        private VRCExpressionsMenu GetTargetMenu(MenuLocation menuOperationTargetMenu, VRCExpressionsMenu rootMenu) {
            throw new System.NotImplementedException();
        }

        private VRCExpressionsMenu GetSourceMenu(MenuLocation menuOperationSourceMenu) {
            throw new System.NotImplementedException();
        }

        // Use VRCFuryFeatureUtils to look for OverrideMenuSettings feature on the avatar
        private void GetVRCFFields(VRCAvatarDescriptor avatarDescriptor, out string vrcfNextText, out Texture2D vrcfNextIcon) {
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

            vrcfNextText = "";
            vrcfNextIcon = null;
        }
    }
}