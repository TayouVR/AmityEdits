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
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(Orifice), true)]
    public class OrificeEditor : AmityBaseEditor {
        private Orifice _targetComponent;
        
        private void DrawHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, "Targets");
        }
        
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(Orifice orifice, GizmoType gizmoType) {
            //if (!gizmo.show) return;
            var rootObject = (UnityEngine.Object)orifice.targetObject != null ? orifice.targetObject : orifice.gameObject.transform;
            DrawGizmo(rootObject.position, rootObject.rotation, orifice.role, "", Selection.activeGameObject == orifice.gameObject);
        }
        
        static void DrawGizmo(Vector3 worldPos, Quaternion worldRot, ApsRole type, string name, bool selected) {
            var orange = new Color(1f, 0.5f, 0);

            var discColor = orange;
            
            var text = "SPS Socket";
            if (!string.IsNullOrWhiteSpace(name)) text += $" '{name}'";
            if (!Utils.IsDesktop()) {
                text += " (Deformation Disabled)\nThis is an Android/iOS project!";
                discColor = Color.red;
            } else if (type == ApsRole.Hole) {
                text += " (Hole)\nPlug follows orange arrow";
            } else if (type == ApsRole.ReversibleRing) {
                text += " (Ring)\nSPS enters either direction\nDPS/TPS only follow orange arrow";
            } else if (type == ApsRole.Ring) {
                text += " (One-Way Ring)\nPlug follows orange arrow";
            } else {
                text += " (Deformation disabled)";
                discColor = Color.red;
            }

            var worldForward = worldRot * Vector3.forward;
            GizmoUtils.DrawDisc(worldPos, worldForward, 0.02f, discColor);
            GizmoUtils.DrawDisc(worldPos, worldForward, 0.04f, discColor);
            if (type == ApsRole.Ring) {
                GizmoUtils.DrawArrow(
                    worldPos + worldForward * 0.05f,
                    worldPos + worldForward * -0.05f,
                    orange
                );
            } else if (type == ApsRole.ReversibleRing) {
                GizmoUtils.DrawArrow(
                    worldPos,
                    worldPos + worldForward * -0.05f,
                    orange
                );
                GizmoUtils.DrawArrow(
                    worldPos,
                    worldPos + worldForward * 0.05f,
                    Color.white
                );
            } else {
                GizmoUtils.DrawArrow(
                    worldPos + worldForward * 0.1f,
                    worldPos,
                    orange
                );
            }

            if (selected) {
                GizmoUtils.DrawText(
                    worldPos,
                    "\n" + text,
                    Color.gray,
                    true,
                    true
                );
            }

            // So that it's actually clickable
            Gizmos.color = Color.clear;
            Gizmos.DrawSphere(worldPos, 0.04f);
        }
        
        private void OnEnable() {
            _targetComponent = (Orifice) target;
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            _targetComponent ??= (Orifice) target;
            
            // Properties
            var targetObjectProp = serializedObject.FindProperty("targetObject");
            
            var depthParameterNameProp = serializedObject.FindProperty("depthParameterName");
            var penetratorWidthParameterNameProp = serializedObject.FindProperty("penetratorWidthParameterName");
            var penetratorLengthParameterNameProp = serializedObject.FindProperty("penetratorLengthParameterName");
            
            var enableDeformationProp = serializedObject.FindProperty("enableDeformation");
            var enableToyContactsProp = serializedObject.FindProperty("enableToyContacts");
            var channelProp = serializedObject.FindProperty("channel");
            var roleProp = serializedObject.FindProperty("role");
            
            // Fields
            var targetObjectField = new PropertyField(targetObjectProp);
            
            var depthParameterNameField = new PropertyField(depthParameterNameProp);
            var penetratorWidthParameterNameField = new PropertyField(penetratorWidthParameterNameProp);
            var penetratorLengthParameterNameField = new PropertyField(penetratorLengthParameterNameProp);
            
            var enableDeformationField = new PropertyField(enableDeformationProp);
            var enableToyContactsField = new PropertyField(enableToyContactsProp);
            var channelField = new PropertyField(channelProp);
            var roleField = new PropertyField(roleProp);
            
            root.Add(targetObjectField);
            
            root.Add(new Label("Parameter Names"));
            root.Add(depthParameterNameField);
            root.Add(penetratorWidthParameterNameField);
            root.Add(penetratorLengthParameterNameField);
            
            root.Add(new Label("Properties (animatable)"));
            root.Add(enableDeformationField);
            root.Add(enableToyContactsField);
            root.Add(channelField);
            root.Add(roleField);

            return root;
        }
    }
}