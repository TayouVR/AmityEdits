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
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(ReorderMenus), true)]
    public class ReorderMenusEditor : AmityBaseEditor {
        private ReorderMenus _reorderMenus;
        
        private void DrawHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, "Targets");
        }
        
        private void OnEnable() {
            _reorderMenus = (ReorderMenus) target;
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override VisualElement CreateInspector() {
            // Each editor window contains a root VisualElement object
            VisualElement root = new VisualElement();

            MyTreeViewItem item = new MyTreeViewItem();
            item.children.Add(new MyTreeViewItem());
            item.children.Add(new MyTreeViewItem());
            item.children.Add(new MyTreeViewItem());
            MyTreeViewItem[] items = { item };

            TreeView treeView = new TreeView();
            treeView.reorderable = true;
            treeView.bindItem = BindMenuItem;
            treeView.viewController.itemsSource = items;
            root.Add(treeView);
            
            // Default Inspector is ugly af, need to find a way to do (reorderable) lists, which don't suck ass
            //root.Add(new PropertyField(serializedObject.FindProperty("targets")));

            // Import UXML
            // VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("7cf2c731f759d7d4390271437e0b08b7"));
            // if (visualTree) {
            //     VisualElement labelFromUXML = visualTree.CloneTree();
            //     root.Add(labelFromUXML);
            // }
            
            return root;
        }

        private void BindMenuItem(VisualElement arg1, int arg2) {
            arg1.Add(new Label("Test!!!"));
        }
    }

    class MyTreeViewItem : TreeViewItem {
    }
}