// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.EditorUtils {
    public static class AmityMenuUtils {
        public static void EnsureDescriptorAssetsDuplicated(
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

        public static VRCExpressionsMenu GetOrCreateMenuByPath(VRCExpressionsMenu root, string path) {
            if (string.IsNullOrWhiteSpace(path)) return root;
            var segments = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            foreach (var seg in segments) {
                var next = current.controls.FirstOrDefault(c => c != null && c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.name == seg);
                if (next == null) {
                    var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    newMenu.name = seg;
                    var ctl = new VRCExpressionsMenu.Control {
                        name = seg,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = newMenu
                    };
                    current.controls.Add(ctl);
                    current = newMenu;
                } else {
                    current = next.subMenu;
                }
            }
            return current;
        }

        public static void AppendControl(VRCExpressionsMenu menu, VRCExpressionsMenu.Control control) {
            if (menu == null || control == null) return;
            menu.controls.Add(control);
        }

        public static AnimatorController EnsureFxController(VRCAvatarDescriptor descriptor) {
            var fx = descriptor.baseAnimationLayers.FirstOrDefault(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (fx.animatorController == null) {
                var controller = new AnimatorController();
                controller.name = descriptor.gameObject.name + "_FX";
                fx.animatorController = controller;
            }
            return (AnimatorController)fx.animatorController;
        }

        public static VRCExpressionParameters.Parameter CreateOrGetVRCParameter(VRCExpressionParameters list, string name, VRCExpressionParameters.ValueType type, float defaultValue = 0, bool saved = true, bool synced = true) {
            var existing = list.FindParameter(name);
            if (existing != null) return existing;
            var param = new VRCExpressionParameters.Parameter {
                name = name,
                valueType = type,
                defaultValue = defaultValue,
                saved = saved,
                networkSynced = synced
            };
            var l = list.parameters.ToList();
            l.Add(param);
            list.parameters = l.ToArray();
            return param;
        }

        public static AnimatorControllerParameter CreateOrGetAnimatorParameter(AnimatorController controller, string name, AnimatorControllerParameterType type) {
            var existing = controller.parameters.FirstOrDefault(p => p != null && p.name == name);
            if (existing != null) return existing;
            var p = new AnimatorControllerParameter { name = name, type = type };
            controller.AddParameter(p);
            return p;
        }

        public static string RelativePath(Transform root, Transform target) {
            var path = new System.Text.StringBuilder();
            var current = target;
            while (current != null && current != root) {
                if (path.Length > 0) path.Insert(0, "/");
                path.Insert(0, current.name);
                current = current.parent;
            }
            return path.ToString();
        }
    }
}