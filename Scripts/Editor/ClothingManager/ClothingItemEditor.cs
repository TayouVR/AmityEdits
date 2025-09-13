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

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(ClothingItem))]
    public class ClothingItemEditor : AmityBaseEditor {
        private ClothingItem _targetComponent;

        public ClothingItemEditor() {
            _targetComponent = (ClothingItem) target;
        }

        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            if (!_targetComponent) {
                _targetComponent = (ClothingItem) target;
            }
            
            // Properties
            var nameProp = serializedObject.FindProperty("name");
            var actionProp = serializedObject.FindProperty("actionMethod");
            var parameterNameProp = serializedObject.FindProperty("parameterName");
            var objectToToggleProp = serializedObject.FindProperty("objectToToggle");
            var animationProp = serializedObject.FindProperty("animation");
            var incompatibilitiesProp = serializedObject.FindProperty("incompatibilities");
            
            // Fields
            var nameField = new PropertyField(nameProp);
            var actionField = new PropertyField(actionProp);
            var parameterNameField = new PropertyField(parameterNameProp) { name = "ParameterNameField" };
            var objectToToggleField = new PropertyField(objectToToggleProp) { name = "ObjectToToggleField" };
            var animationField = new PropertyField(animationProp) { name = "AnimationField" };
            var amityActionInfo = new Label("This feature is not yet implemented.") { name = "AmityActionInfo" };
            // var amityActionInfo = new PropertyField(serializedObject.FindProperty("amityAction")) { name = "AmityActionInfo" };
            var incompatibilitiesField = new PropertyField(incompatibilitiesProp);
            
            root.Add(nameField);
            root.Add(actionField);
            root.Add(parameterNameField);
            root.Add(objectToToggleField);
            root.Add(animationField);
            root.Add(amityActionInfo);
            root.Add(incompatibilitiesField);

            root.Bind(serializedObject);

            // Visibility controller
            void UpdateVisibility() {
                // Read current enum value from the property
                var method = (ItemActionMethod)actionProp.enumValueIndex;

                // Hide all first
                parameterNameField.style.display = DisplayStyle.None;
                objectToToggleField.style.display = DisplayStyle.None;
                animationField.style.display = DisplayStyle.None;
                amityActionInfo.style.display = DisplayStyle.None;

                // Show the ones relevant to the current selection
                switch (method) {
                    case ItemActionMethod.Parameter:
                        parameterNameField.style.display = DisplayStyle.Flex;
                        break;
                    case ItemActionMethod.ObjectToggle:
                        objectToToggleField.style.display = DisplayStyle.Flex;
                        break;
                    case ItemActionMethod.Animation:
                        animationField.style.display = DisplayStyle.Flex;
                        break;
                    case ItemActionMethod.AmityAction:
                        amityActionInfo.style.display = DisplayStyle.Flex;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // React to changes coming from the UI or from outside (Undo, scripts, etc.)
            root.TrackPropertyValue(actionProp, _ => UpdateVisibility());

            // Initialize visibility for the initial value
            UpdateVisibility();

            return root;
        }
    }
}