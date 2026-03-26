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
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3A.Editor.Elements;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

namespace org.Tayou.AmityEdits.MenuItem {
    [CustomEditor(typeof(MenuItem))]
    public class MenuItemEditor : AmityBaseEditor {
        private MenuItem _targetComponent;

        public MenuItemEditor() {
            _targetComponent = (MenuItem) target;
        }

        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            if (!_targetComponent) {
                _targetComponent = (MenuItem) target;
            }
            
            // Properties
            var pathMethodProp = serializedObject.FindProperty("pathMethod");
            var parentMenuProp = serializedObject.FindProperty("parentMenu");
            var menuPathProp = serializedObject.FindProperty("menuPath");
            var vrcMenuControlProp = serializedObject.FindProperty("vrcMenuControl");
            var actionsProp = serializedObject.FindProperty("actions");
            
            // Fields
            var pathMethod = new PropertyField(pathMethodProp);
            var parentMenu = new PropertyField(parentMenuProp);
            var menuPath = new PropertyField(menuPathProp);
            var actionsField = new PropertyField(actionsProp);
            
            root.Add(pathMethod);
            root.Add(parentMenu);
            root.Add(menuPath);

            var controlOptionsContainer = new VisualElement();
            root.Add(controlOptionsContainer);

            void UpdateControlOptions() {
                controlOptionsContainer.Clear();
                var menu = _targetComponent.parentMenu ?? _targetComponent.transform.GetComponentsInParent<VRCAvatarDescriptor>().First().expressionsMenu;
                if (((object)menu) != null) {
                    var controlOptions = new ExpressionsControlOptions(vrcMenuControlProp, menu);
                    controlOptionsContainer.Add(controlOptions);
                } else {
                    // Fallback to default property field if no menu is available
                    var vrcMenuControl = new PropertyField(vrcMenuControlProp);
                    controlOptionsContainer.Add(vrcMenuControl);
                }
            }

            root.Add(actionsField);

            root.Bind(serializedObject);

            // Visibility controller
            void UpdateVisibility() {
                // Read current enum value from the property
                var method = (PathMethod)pathMethodProp.enumValueIndex;

                // Hide all first
                parentMenu.style.display = DisplayStyle.None;
                menuPath.style.display = DisplayStyle.None;

                // Show the ones relevant to the current selection
                switch (method) {
                    case PathMethod.Parent:
                        parentMenu.style.display = DisplayStyle.Flex;
                        break;
                    case PathMethod.Path:
                        menuPath.style.display = DisplayStyle.Flex;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                UpdateControlOptions();
            }

            // React to changes coming from the UI or from outside (Undo, scripts, etc.)
            root.TrackPropertyValue(pathMethodProp, _ => UpdateVisibility());
            root.TrackPropertyValue(parentMenuProp, _ => UpdateControlOptions());

            // Initialize visibility for the initial value
            UpdateVisibility();

            return root;
        }
        
    }
}