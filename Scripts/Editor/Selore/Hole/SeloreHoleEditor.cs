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
using org.Tayou.AmityEdits.EditorSeloreSps;
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
    public class SeloreHoleEditor : AmityBaseEditor<SeloreHole> {

        private void DrawHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, "Targets");
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(SeloreHole seloreHole, GizmoType gizmoType) {
            //if (!gizmo.show) return;
            var rootObject = (UnityEngine.Object)seloreHole.targetObject != null
                ? seloreHole.targetObject
                : seloreHole.gameObject.transform;
            DrawGizmo(rootObject.position, rootObject.rotation, seloreHole.role, "",
                Selection.activeGameObject == seloreHole.gameObject);
        }

        static void DrawGizmo(Vector3 worldPos, Quaternion worldRot, SeloreRole type, string name, bool selected) {
            var orange = new Color(1f, 0.5f, 0);

            var discColor = orange;

            var text = "Selore Hole";
            if (!string.IsNullOrWhiteSpace(name)) text += $" '{name}'";
            if (!Utils.IsDesktop()) {
                text += " (Deformation Disabled)\nThis is an Android/iOS project!";
                discColor = Color.red;
            } else if (type == SeloreRole.Hole) {
                text += " (Hole)\nPlug follows orange arrow";
            } else if (type == SeloreRole.ReversibleRing) {
                text += " (Ring)\nSPS enters either direction\nDPS/TPS only follow orange arrow";
            } else if (type == SeloreRole.Ring) {
                text += " (One-Way Ring)\nPlug follows orange arrow";
            } else {
                text += " (Deformation disabled)";
                discColor = Color.red;
            }

            var worldForward = worldRot * Vector3.forward;
            GizmoUtils.DrawDisc(worldPos, worldForward, 0.02f, discColor);
            GizmoUtils.DrawDisc(worldPos, worldForward, 0.04f, discColor);
            if (type == SeloreRole.Ring) {
                GizmoUtils.DrawArrow(
                    worldPos + worldForward * 0.05f,
                    worldPos + worldForward * -0.05f,
                    orange
                );
            } else if (type == SeloreRole.ReversibleRing) {
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
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();

            // Properties
            var targetObjectProp = serializedObject.FindProperty("targetObject");

            var depthParameterNameProp = serializedObject.FindProperty("depthParameterName");
            var penetratorWidthParameterNameProp = serializedObject.FindProperty("penetratorWidthParameterName");
            var penetratorLengthParameterNameProp = serializedObject.FindProperty("penetratorLengthParameterName");

            var enableDeformationProp = serializedObject.FindProperty("enableDeformation");
            var enableContactSendersProp = serializedObject.FindProperty("enableContactSenders");
            var enableToyContactsProp = serializedObject.FindProperty("enableToyContacts");
            var channelProp = serializedObject.FindProperty("channel");
            var roleProp = serializedObject.FindProperty("role");

            var featureLightsProp = serializedObject.FindProperty("featureLights");
            var featureContactSendersProp = serializedObject.FindProperty("featureContactSenders");
            var featureToyContactReceiversProp = serializedObject.FindProperty("featureToyContactReceivers");
            var featurePlugReceiversProp = serializedObject.FindProperty("featurePlugReceivers");
            var featureTouchReceiversProp = serializedObject.FindProperty("featureTouchReceivers");
            var featureFrotReceiverProp = serializedObject.FindProperty("featureFrotReceiver");

            // Fields
            var targetObjectField = new PropertyField(targetObjectProp);

            var depthParameterNameField = new PropertyField(depthParameterNameProp);
            var penetratorWidthParameterNameField = new PropertyField(penetratorWidthParameterNameProp);
            var penetratorLengthParameterNameField = new PropertyField(penetratorLengthParameterNameProp);

            var enableDeformationField = new PropertyField(enableDeformationProp);
            var enableContactSendersField = new PropertyField(enableContactSendersProp);
            var enableToyContactsField = new PropertyField(enableToyContactsProp);
            var channelField = new PropertyField(channelProp);
            var roleField = new PropertyField(roleProp);

            var featureLightsField = new PropertyField(featureLightsProp);
            var featureContactSendersField = new PropertyField(featureContactSendersProp);
            var featureToyContactReceiversField = new PropertyField(featureToyContactReceiversProp);
            var featurePlugReceiversField = new PropertyField(featurePlugReceiversProp, "Plugs");
            var featureTouchReceiversField = new PropertyField(featureTouchReceiversProp, "Touch (Finger, Hand)");
            var featureFrotReceiverField = new PropertyField(featureFrotReceiverProp, "Frotting");


            root.Add(targetObjectField);

            root.Add(Utils.Header("Parameter Names"));
            root.Add(depthParameterNameField);
            root.Add(penetratorWidthParameterNameField);
            root.Add(penetratorLengthParameterNameField);

            root.Add(Utils.Header("Properties (animatable)"));
            root.Add(enableDeformationField);
            root.Add(enableContactSendersField);
            root.Add(enableToyContactsField);
            root.Add(channelField);
            root.Add(roleField);
            root.Add(Utils.InfoBox(
                "these properties will be repathed on build to point to lights, contacts, etc. and animate the hole accordingly."));

            var advancedContainer = new VisualElement();
            advancedContainer.Add(Utils.InfoBox(
                "You Probably don't want to disable these, unless you know what you are doing.\n" +
                "Disabling Lights or contacts will break deformation."));
            advancedContainer.Add(featureLightsField);
            advancedContainer.Add(featureContactSendersField);
            Utils.AddOverrideRow(advancedContainer, featureToyContactReceiversProp, featureToyContactReceiversField,
                new[] {
                    featurePlugReceiversField,
                    featureTouchReceiversField,
                    featureFrotReceiverField,
                });

            var advancedFoldout = new Foldout {
                text = "Advanced",
            };
            advancedFoldout.contentContainer.Add(advancedContainer);

            root.Add(advancedFoldout);

            // --- SPS Cell Preview ---
            var spsCellProp = serializedObject.FindProperty("featureSpsCell");
            var spsCellField = new PropertyField(spsCellProp, "Write SPS Cell");

            var spsFoldout = new Foldout { text = "SPS Cell Preview" };
            spsFoldout.style.display = Target.featureSpsCell ? DisplayStyle.Flex : DisplayStyle.None;
            root.TrackPropertyValue(spsCellProp, prop => {
                spsFoldout.style.display = prop.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            var zoomSlider = new SliderInt("Zoom", 5, 25) { value = 10 };
            zoomSlider.style.flexGrow = 0;
            zoomSlider.style.marginLeft = 4;
            zoomSlider.style.marginRight = 4;

            var spsInfoLabel = new Label("Hover over a pixel to inspect cell contents");
            spsInfoLabel.style.whiteSpace = WhiteSpace.Normal;
            spsInfoLabel.style.paddingLeft = 4;
            spsInfoLabel.style.paddingTop = 2;
            spsInfoLabel.style.fontSize = 10;

            var previewContainer = new IMGUIContainer();
            previewContainer.style.flexShrink = 0;
            previewContainer.style.alignSelf = Align.FlexStart;
            previewContainer.style.marginLeft = 4;

            SeloreCellData currentData = default;
            Texture2D cellTex = null;

            Action rebuildCellPreview = () => {
                currentData = EditorSeloreSps.SpsCellPreview.BuildPreviewData(Target);
                if (cellTex != null) UnityEngine.Object.DestroyImmediate(cellTex);
                cellTex = EditorSeloreSps.SpsCellPreview.RenderCell(currentData);
                previewContainer.MarkDirtyRepaint();
            };

            Action updatePreviewSize = () => {
                float size = SpsCellPreview.CELL_WIDTH * zoomSlider.value;
                previewContainer.style.width = size;
                previewContainer.style.maxWidth = size;
                previewContainer.style.height = size;
                previewContainer.style.maxHeight = size;
                previewContainer.MarkDirtyRepaint();
            };
            updatePreviewSize();

            zoomSlider.RegisterValueChangedCallback(_ => updatePreviewSize());

            int hoveredIndex = -1;

            previewContainer.onGUIHandler = () => {
                float zoom = zoomSlider.value;
                float displaySize = SpsCellPreview.CELL_WIDTH * zoom;

                Rect rect = GUILayoutUtility.GetRect(displaySize, displaySize);
                rect = EditorGUI.IndentedRect(rect);
                if (rect.width < 16 || rect.height < 16) return;

                if (cellTex != null) {
                    EditorGUI.DrawTextureTransparent(rect, cellTex, ScaleMode.StretchToFill);

                    // Grid overlay
                    Handles.BeginGUI();
                    Color gridColor = new Color(0, 0, 0, 0.25f);
                    Handles.color = gridColor;
                    for (int i = 0; i <= SpsCellPreview.CELL_WIDTH; i++) {
                        float x = rect.x + i * zoom;
                        Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.y + displaySize));
                    }
                    for (int i = 0; i <= SpsCellPreview.CELL_HEIGHT; i++) {
                        float y = rect.y + i * zoom;
                        Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.x + displaySize, y));
                    }
                    Handles.EndGUI();

                    // Hover detection
                    Vector2 mousePos = Event.current.mousePosition - new Vector2(rect.x, rect.y);
                    int px = Mathf.FloorToInt(mousePos.x / zoom);
                    int py = Mathf.FloorToInt(mousePos.y / zoom);

                    if (px >= 0 && px < SpsCellPreview.CELL_WIDTH
                        && py >= 0 && py < SpsCellPreview.CELL_HEIGHT
                        && rect.Contains(Event.current.mousePosition)) {
                        hoveredIndex = py * SpsCellPreview.CELL_WIDTH + px;

                        EditorGUI.DrawRect(
                            new Rect(rect.x + px * zoom, rect.y + py * zoom, zoom, zoom),
                            new Color(1, 1, 0, 0.2f));

                        string decoded = EditorSeloreSps.SpsCellPreview.DecodePixel(hoveredIndex, currentData);
                        string sectionName = "Unknown";
                        if (hoveredIndex == 0 || hoveredIndex == SpsCellPreview.CELL_WIDTH - 1
                            || hoveredIndex == (SpsCellPreview.CELL_HEIGHT - 1) * SpsCellPreview.CELL_WIDTH
                            || hoveredIndex == SpsCellPreview.CELL_HEIGHT * SpsCellPreview.CELL_WIDTH - 1)
                            sectionName = "Magic Corner";
                        else if (hoveredIndex < SpsCellPreview.CELL_PAYLOAD_START) sectionName = "Header (top row)";
                        else if (hoveredIndex >= 240) sectionName = "Header (bottom row)";
                        else sectionName = "Payload";

                        spsInfoLabel.text =
                            $"Pixel: ({px}, {py})  Index: {hoveredIndex}\n" +
                            $"Section: {sectionName}\n" +
                            $"Content: {decoded}";

                        var tipRect = new Rect(
                            Event.current.mousePosition + new Vector2(14, -10),
                            new Vector2(320, 60));
                        GUI.Box(tipRect, GUIContent.none);
                        EditorGUI.DropShadowLabel(tipRect, decoded);

                        previewContainer.MarkDirtyRepaint();
                    } else {
                        hoveredIndex = -1;
                    }

                    if (Event.current.type == EventType.MouseMove)
                        Event.current.Use();
                }
            };

            spsFoldout.Add(zoomSlider);
            spsFoldout.Add(previewContainer);
            spsFoldout.Add(spsInfoLabel);

            root.TrackPropertyValue(spsCellProp, _ => { if (spsCellProp.boolValue) rebuildCellPreview(); });
            root.TrackPropertyValue(targetObjectProp, _ => rebuildCellPreview());
            root.TrackPropertyValue(roleProp, _ => rebuildCellPreview());

            // --- SPS toggle (inside advanced, after frot) ---
            advancedContainer.Add(spsCellField);

            root.Add(spsFoldout);

            // --- Build Summary ---
            var summaryBox = Utils.InfoBox();

            var sRole = new Label();
            var sLights = new Label();
            var sSenders = new Label();
            var sReceivers = new Label();
            var sSpsCell = new Label();

            summaryBox.Add(Utils.Header("Build Summary"));
            summaryBox.Add(sRole);
            summaryBox.Add(sLights);
            summaryBox.Add(sSenders);
            summaryBox.Add(sReceivers);
            summaryBox.Add(sSpsCell);
            Utils.CreateToySupportRow(summaryBox, out var overall, out var toyPlug, out var toyTouch, out var toyFrot);

            Action updateSummary = () => {
                var h = Target;
                sRole.text = $"Role: {h.role}";
                sLights.text = h.featureLights ? "Generating Lights: 2" : "Generating Lights: 0";
                sSenders.text = h.featureContactSenders
                    ? "Generating Contact Senders: 2"
                    : "Generating Contact Senders: 0";
                sReceivers.text = Utils.BuildReceiverCountString(
                    h.featureToyContactReceivers, h.featurePlugReceivers, h.featureTouchReceivers,
                    h.featureFrotReceiver);
                sSpsCell.text = h.featureSpsCell ? "Generating SPS Cell: 1" : "Generating SPS Cell: 0";
                toyPlug.style.color = h.featurePlugReceivers ? Color.green : Color.red;
                toyTouch.style.color = h.featureTouchReceivers ? Color.green : Color.red;
                toyFrot.style.color = h.featureFrotReceiver ? Color.green : Color.red;
            };
            updateSummary();

            foreach (var p in new[] {
                         featureLightsProp, 
                         featureContactSendersProp, 
                         featureToyContactReceiversProp,
                         featurePlugReceiversProp, 
                         featureTouchReceiversProp, 
                         featureFrotReceiverProp, 
                         roleProp,
                         spsCellProp
                     }) {
                summaryBox.TrackPropertyValue(p, _ => updateSummary());
            }

            root.Add(summaryBox);

            return root;
        }
    }
}