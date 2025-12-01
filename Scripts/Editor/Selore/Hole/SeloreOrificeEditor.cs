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
 *
 *
 *  This file includes code from VRCFury from commit https://github.com/TayouVR/VRCFury/commit/4d3aa38c25e32cf07d629dce68bbcdfa1840c3d6 as MIT:
 * 
 *     * VRCFury may be used for commercial purposes only if the source code is downloaded directly by the end-user from an archive distributed on https://vcc.vrcfury.com
 *       * The package may be downloaded by an interactive guided process and extracted from a compressed archive, but the source files must be left unmodified.
 *       * A commercial use is one primarily intended for commercial advantage or monetary compensation (including, but not limited to, one-time payments, subscription payments, and donations).
 *       * Packages containing portions of VRCFury code which are available on VRChat asset servers ("uploaded avatar asset bundles") are excluded from this rule as a special exception.
 *     
 *     PROVIDED that the above restriction(s) are not violated, you are free to use VRCFury under the MIT license as follows:
 *     Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *     The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
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
    [CustomEditor(typeof(SeloreHole), true)]
    public class SeloreOrificeEditor : AmityBaseEditor {
        private SeloreHole _targetComponent;
        
        private void DrawHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, "Targets");
        }
        
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(SeloreHole seloreHole, GizmoType gizmoType) {
            //if (!gizmo.show) return;
            var rootObject = (UnityEngine.Object)seloreHole.targetObject != null ? seloreHole.targetObject : seloreHole.gameObject.transform;
            DrawGizmo(rootObject.position, rootObject.rotation, seloreHole.role, "", Selection.activeGameObject == seloreHole.gameObject);
        }
        
        static void DrawGizmo(Vector3 worldPos, Quaternion worldRot, ApsRole type, string name, bool selected) {
            var orange = new Color(1f, 0.5f, 0);

            var discColor = orange;
            
            var text = "Selore Hole";
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
            _targetComponent = (SeloreHole) target;
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            _targetComponent ??= (SeloreHole) target;
            
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