// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2026 Tayou <git@tayou.org>
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
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using org.Tayou.AmityEdits.EditorUtils;
using VRC.SDKBase.Editor.BuildPipeline;

namespace org.Tayou.AmityEdits {
    /// Runs after NDMF and VRCFury have completed their own menu pagination.
    /// Deduplicates nested "Next"/"More" pages created independently by both tools.
    ///
    /// Typical pattern after both tools paginate the same menu:
    ///   Page 1: [item1…item7, "Next" (VRCFury) → Page2]
    ///   Page 2: ["More" (NDMF) → Page3, item8…item14]
    ///   Page 3: [item15+]
    ///
    /// This pass inlines Page2's stray "More" so Page2 contains the real content.
    internal class MenuDeduplicationPass : IVRCSDKPreprocessAvatarCallback {
        // Runs after NDMF (likely CallbackOrder ~0) and VRCFury (-10000)
        public int callbackOrder => 10000;

        private static readonly string[] PageButtonNames = {"More", "Next", "Next Page", "<color=green>More", "<color=green>More</color>"};
        private static Texture2D _ndmfMoreIcon;

        public bool OnPreprocessAvatar(GameObject avatarGameObject) {
            Debug.Log($"[MenuDedup] OnPreprocessAvatar called on '{avatarGameObject.name}'");

            var descriptor = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null) {
                Debug.LogWarning("[MenuDedup] No VRCAvatarDescriptor found on avatar, skipping");
                return true;
            }
            if (descriptor.expressionsMenu == null) {
                Debug.LogWarning("[MenuDedup] avatar.expressionsMenu is null, skipping");
                return true;
            }

            Debug.Log($"[MenuDedup] Root menu: '{descriptor.expressionsMenu.name}' ({descriptor.expressionsMenu.controls.Count} controls)");

            if (_ndmfMoreIcon == null) {
                _ndmfMoreIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/nadena.dev.modular-avatar/Runtime/Icons/Icon_More_A.png"
                );
                Debug.Log($"[MenuDedup] NDMF More icon loaded: {(_ndmfMoreIcon != null ? _ndmfMoreIcon.name : "null")}");
            }

            // Read MenuOptions for user-customized next-page appearance
            var menuOptions = avatarGameObject.GetComponentsInChildren<MenuOptions>(true);
            Debug.Log($"[MenuDedup] Found {menuOptions.Length} MenuOptions component(s)");
            var nextText = menuOptions.Length > 0 ? menuOptions[0].nextPageButtonTitle : null;
            var nextIcon = menuOptions.Length > 0 ? menuOptions[0].nextPageButtonIcon : null;
            Debug.Log($"[MenuDedup] MenuOptions text='{nextText}', icon={nextIcon?.name}");

            // Merge with VRCFury's OverrideMenuSettings
            GetVrcfOverrideSettings(avatarGameObject, out var vrcfText, out var vrcfIcon);
            Debug.Log($"[MenuDedup] VRCFury OverrideMenuSettings text='{vrcfText}', icon={vrcfIcon?.name}");
            nextText = nextText ?? vrcfText ?? "Next";
            nextIcon = nextIcon ?? vrcfIcon;
            Debug.Log($"[MenuDedup] Final nextPage: text='{nextText}', icon={nextIcon?.name}");

            FixMenu(descriptor.expressionsMenu, nextText, nextIcon, new HashSet<VRCExpressionsMenu>());
            Debug.Log("[MenuDedup] OnPreprocessAvatar complete");
            return true;
        }

        private void FixMenu(VRCExpressionsMenu menu, string nextText, Texture2D nextIcon,
                             HashSet<VRCExpressionsMenu> visited) {
            if (menu == null) {
                Debug.LogWarning("[MenuDedup] FixMenu called with null menu");
                return;
            }
            if (!visited.Add(menu)) {
                Debug.Log($"[MenuDedup] Skipping already-visited menu '{menu.name}'");
                return;
            }

            Debug.Log($"[MenuDedup] FixMenu '{menu.name}' — {menu.controls.Count} controls, {(menu.controls.Count > 8 ? "OVERFLOW" : "OK")}");

            // 1. Depth-first recursion into all submenus
            for (int i = 0; i < menu.controls.Count; i++) {
                var c = menu.controls[i];
                if (c?.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu != null) {
                    Debug.Log($"[MenuDedup]  '{menu.name}' recursing into submenu[{i}]: '{c.name}' → '{c.subMenu.name}'");
                    FixMenu(c.subMenu, nextText, nextIcon, visited);
                }
            }

            // 2. Inline stray pagination pages that appear as the FIRST control.
            //    After both NDMF and VRCFury have paginated independently, a page
            //    often starts with the other tool's "More"/"Next" button (e.g.
            //    VRCFury's "Next" → page whose first item is NDMF's "More").
            int inlineCount = 0;
            while (menu.controls.Count > 0 && IsPageButton(menu.controls[0])) {
                var btn = menu.controls[0];
                var sub = btn.subMenu;
                menu.controls.RemoveAt(0);
                inlineCount++;
                Debug.Log($"[MenuDedup]  '{menu.name}' inlined stray page btn #{inlineCount}: '{btn.name}' → '{sub?.name ?? "null"}' ({(sub != null ? sub.controls.Count : 0)} controls pulled up)");
                if (sub != null && sub != menu && sub.controls != null) {
                    menu.controls.InsertRange(0, sub.controls);
                    Debug.Log($"[MenuDedup]    '{menu.name}' after inline: {menu.controls.Count} controls");
                }
            }
            if (inlineCount > 0) {
                Debug.Log($"[MenuDedup]  '{menu.name}' after inlining {inlineCount} page(s): {menu.controls.Count} controls");
            }

            // 3. Standard pagination if the menu still exceeds 8 controls
            if (menu.controls.Count > 8) {
                Debug.Log($"[MenuDedup]  '{menu.name}' still has {menu.controls.Count} controls — running standard pagination");

                VRCExpressionsMenu nextPage = null;

                // Look for an existing "Next"/"More" control to reuse
                for (int i = 0; i < menu.controls.Count; i++) {
                    var c = menu.controls[i];
                    if (c?.type == VRCExpressionsMenu.Control.ControlType.SubMenu && IsPageButton(c)) {
                        Debug.Log($"[MenuDedup]    Reusing existing page btn at index {i}: '{c.name}' → '{c.subMenu?.name ?? "null"}'");
                        nextPage = c.subMenu;
                        menu.controls.RemoveAt(i);
                        break;
                    }
                }

                if (nextPage == null || nextPage == menu) {
                    nextPage = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    nextPage.name = menu.name + " (Next)";
                    Debug.Log($"[MenuDedup]    Created new VRCExpressionsMenu '{nextPage.name}'");
                    if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(menu))) {
                        AssetDatabase.AddObjectToAsset(nextPage, menu);
                        Debug.Log($"[MenuDedup]    Added as sub-asset of '{menu.name}'");
                    }
                }

                // Move excess controls (indices 7+) to the next page
                int moved = 0;
                while (menu.controls.Count > 7) {
                    var movedCtrl = menu.controls[7];
                    nextPage.controls.Add(movedCtrl);
                    menu.controls.RemoveAt(7);
                    moved++;
                }
                Debug.Log($"[MenuDedup]    Moved {moved} controls to '{nextPage.name}' (now has {nextPage.controls.Count})");

                menu.controls.Add(new VRCExpressionsMenu.Control {
                    name = nextText ?? "Next",
                    icon = nextIcon,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = nextPage
                });
                Debug.Log($"[MenuDedup]    Added '{(nextText ?? "Next")}' btn → '{nextPage.name}'. '{menu.name}' now has {menu.controls.Count} controls");

                // Recurse into the new page (it might also exceed 8 controls)
                visited.Remove(nextPage);
                FixMenu(nextPage, nextText, nextIcon, visited);
            } else {
                Debug.Log($"[MenuDedup]  '{menu.name}' done — {menu.controls.Count} controls, no pagination needed");
            }
        }

        private static bool IsPageButton(VRCExpressionsMenu.Control control) {
            if (control?.type != VRCExpressionsMenu.Control.ControlType.SubMenu) return false;
            if (PageButtonNames.Contains(control.name)) {
                Debug.Log($"[MenuDedup] IsPageButton: YES — name '{control.name}'");
                return true;
            }
            if (_ndmfMoreIcon != null && control.icon == _ndmfMoreIcon) {
                Debug.Log($"[MenuDedup] IsPageButton: YES — icon matches NDMF More icon (control name was '{control.name}')");
                return true;
            }
            return false;
        }

        private static void GetVrcfOverrideSettings(GameObject root, out string nextText, out Texture2D nextIcon) {
            nextText = null;
            nextIcon = null;
            var model = VRCFuryFeatureUtils.GetOverrideMenuSettingsModel(root, true);
            if (model != null) {
                Debug.Log("[MenuDedup] Found VRCFury OverrideMenuSettings model, trying to read fields");
                bool ok = VRCFuryFeatureUtils.TryReadOverrideMenuSettings(model, out nextText, out nextIcon);
                Debug.Log($"[MenuDedup] TryReadOverrideMenuSettings returned {ok}, text='{nextText}', icon={nextIcon?.name}");
            } else {
                Debug.Log("[MenuDedup] No VRCFury OverrideMenuSettings found on avatar");
            }
        }
    }
}
