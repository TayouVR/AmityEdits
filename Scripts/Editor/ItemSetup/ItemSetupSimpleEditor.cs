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
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    
    [CustomEditor(typeof(ItemSetupSimple), true)]
    public class ItemSetupSimpleEditor : Editor {
        /*protected override void OnHeaderGUI() {
            ((VRCFuryItemSetupSimple) target).enabled = GUILayout.Toggle(((VRCFuryItemSetupSimple) target).enabled, "");
            GUI.Box(new Rect(0, 0, 200, EditorGUIUtility.singleLineHeight), new GUIContent("Tayou"));
            InvokeMethod(this, "OnHeaderGUI", this, "Simple Item Setup", 200);
        }
        
        private static object InvokeMethod(object targetObject, string methodName, params object[] parameters) {
            return targetObject.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(targetObject, parameters);
        }*/

        public override VisualElement CreateInspectorGUI() {
            
            // Each editor window contains a root VisualElement object
            VisualElement root = new VisualElement();

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            //VisualElement label = new Label("Hello World! From C#");
            //root.Add(label);

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/org.tayou.vrcfury-extensions/Scripts/Editor/ItemSetupSimpleEditor.uxml");
            VisualElement uxmlFileContents = visualTree.CloneTree();
            root.Add(uxmlFileContents);
            switch (((ItemSetupSimple)target).itemPreviewSelection) {
                case ItemSetupSimple.ItemPreviewSelection.RestPosition:
                    break;
                case ItemSetupSimple.ItemPreviewSelection.LeftHand:
                    break;
                case ItemSetupSimple.ItemPreviewSelection.RightHand:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/org.tayou.vrcfury-extensions/Scripts/Editor/ItemSetupSimpleEditor.uss");
            //VisualElement labelWithStyle = new Label("Hello World! With Style");
            //labelWithStyle.styleSheets.Add(styleSheet);
            //root.Add(labelWithStyle);
            
            return root;
        }
    }
}