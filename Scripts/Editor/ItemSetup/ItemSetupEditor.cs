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
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(ItemSetup), true)]
    public class ItemSetupEditor : AmityBaseEditor {
        private ItemSetup _itemSetup;

        /*private void OnSceneGUI() {
            for (var i = 0; i < _itemSetup.targets.Count; i++) {
                var itemTarget = _itemSetup.targets[i];
                if (_itemSetup.itemPreviewIndex == i) {
                    EditorGUI.BeginChangeCheck();
                    var position1 = itemTarget.transform.position;
                    var positionOffset = Handles.DoPositionHandle(itemTarget.transform.TransformPoint(itemTarget.PositionOffset),
                        Quaternion.Euler(itemTarget.RotationOffset) * itemTarget.transform.rotation) - position1;
                    var rotationOffset =
                        Handles.DoRotationHandle(Quaternion.Euler(itemTarget.RotationOffset), itemTarget.PositionOffset + position1).eulerAngles;
                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(target, "Altered offsets");
                        itemTarget.PositionOffset = positionOffset;
                        itemTarget.RotationOffset = rotationOffset;
                    }
                } 
            }
        }*/

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(ItemSetup itemSetup, GizmoType gizmoType) {
            for (var i = 0; i < itemSetup.targets.Count; i++) {
                var itemTarget = itemSetup.targets[i];
                if (itemSetup.itemPreviewIndex != i) {
                    var position1 = itemTarget.transform.TransformPoint(itemTarget.position);
                    var cumAngle = Quaternion.Euler(itemTarget.rotation.eulerAngles) * itemTarget.transform.rotation;
                    var direction = new Vector3(Mathf.Cos(cumAngle.x), Mathf.Sin(cumAngle.y), Mathf.Tan(cumAngle.z));
                    GizmosUtil.DrawArrow(position1, cumAngle * Vector3.up * 0.25f + position1);
                    //VRCFuryGizmoUtils.DrawArrow(position1, cumAngle * Vector3.up * 0.25f + position1, Color.cyan);
                }

            }
        }

        /// <summary>
        /// Saves the current Transform (position & rotation) to the corresponding fields based on the preview index (-1 for rest state)
        /// </summary>
        private void SaveCurrentTransformToOffsets() {
            Transform itemSetupTrans = _itemSetup.transform;
            if (_itemSetup.itemPreviewIndex == -1) {
                _itemSetup.restPosition = itemSetupTrans.position;
                _itemSetup.restRotation = itemSetupTrans.rotation;
            } else {
                _itemSetup.targets[_itemSetup.itemPreviewIndex].position = itemSetupTrans.position;
                _itemSetup.targets[_itemSetup.itemPreviewIndex].rotation = itemSetupTrans.rotation;
            }
        }
        
        private void OnEnable() {
            _itemSetup = (ItemSetup) target;
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override VisualElement CreateInspector() {
            // Each editor window contains a root VisualElement object
            // Import UXML
            VisualElement root;
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("7cf2c731f759d7d4390271437e0b08b7"));
            if (visualTree) {
                root = visualTree.CloneTree();
            } else {
                root = new VisualElement();
            }

            root.Q<PropertyField>("itemDefaultActiveState")
                .BindProperty(serializedObject.FindProperty("itemDefaultActiveState"));
            root.Q<PropertyField>("restPosition")
                .BindProperty(serializedObject.FindProperty("restPosition"));
            root.Q<PropertyField>("restRotation")
                .BindProperty(serializedObject.FindProperty("restRotation"));
            
            // Default Inspector is ugly af, need to find a way to do (reorderable) lists, which don't suck ass
            //root.Add(new PropertyField(serializedObject.FindProperty("targets")));

            var targetsList = root.Q<ListView>("targetsList");
            targetsList.BindProperty(serializedObject.FindProperty("targets"));
            targetsList.makeItem = MakeItem;
            targetsList.bindItem = BindItem;

            //root.Add(new IMGUIContainer(() => Targets.DoLayoutList()));
            
            return root;
        }

        private void BindItem(VisualElement itemRoot, int index) {
            SerializedObject targetsObj = serializedObject.FindProperty("targets").GetArrayElementAtIndex(index).serializedObject;
            
            //ItemData itemSetupTarget = targetsObj.targetObject as ItemData;

            var transformField = itemRoot.Q<PropertyField>("transform");
            transformField.BindProperty(targetsObj.FindProperty("transform"));
            transformField.label = $"Target {index}";
            //HierarchyTransform.OnInspectorGUI(serializedObject.FindProperty("targets").GetArrayElementAtIndex(index).FindPropertyRelative("path").serializedObject);

            if (targetsObj.targetObject as object == null) return;
            
            itemRoot.Q<PropertyField>("positionOffset").BindProperty(targetsObj.FindProperty("position"));
            itemRoot.Q<PropertyField>("rotationOffset").BindProperty(targetsObj.FindProperty("rotation"));

            var previewButton = itemRoot.Q<Button>("previewButton");
            previewButton.text = _itemSetup.itemPreviewIndex == index ? "Preview" : "Stop Preview";

            previewButton.clicked -= null;
            previewButton.clicked += () => PreviewButtonOnClicked(index);
            serializedObject.ApplyModifiedProperties();
        }

        private void PreviewButtonOnClicked(int index) {
            if (_itemSetup.itemPreviewIndex == index) {
                SaveCurrentTransformToOffsets();
                //EditorUtility.SetDirty(_itemSetup);
                _itemSetup.itemPreviewIndex = -1;
                //Undo.RecordObject(target, $"Took Item Target #{index} out of Preview mode");
                _itemSetup.gameObject.SetActive(_itemSetup.itemDefaultActiveState);
            } else {
                //EditorUtility.SetDirty(_itemSetup);
                _itemSetup.itemPreviewIndex = index;
                //Undo.RecordObject(target, $"Set Item Target #{index} in Preview mode");
                //_itemSetup.transform.position = itemSetupTarget.transform.position + itemSetupTarget.position;
                    
                //_itemSetup.transform.rotation = itemSetupTarget.transform.rotation * itemSetupTarget.rotation;
                _itemSetup.gameObject.SetActive(true);
            }
        }

        private VisualElement MakeItem() {
            VisualElement itemRoot = new VisualElement();

            itemRoot.Add(new PropertyField { label = "Target", name = "transform", style = { height = 20}});
            //HierarchyTransform.OnInspectorGUI(serializedObject.FindProperty("targets").GetArrayElementAtIndex(index).FindPropertyRelative("path").serializedObject);

            itemRoot.Add(new PropertyField { label = "Position Offset", name = "positionOffset" });
            itemRoot.Add(new PropertyField { label = "Rotation Offset", name = "rotationOffset" });

            itemRoot.Add(new Button { text = "Preview", name = "previewButton" });
            return itemRoot;
        }
    }
}