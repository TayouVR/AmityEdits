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
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using org.Tayou.AmityEdits;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {

[CustomEditor(typeof(SeloreShaderPatcher))]
public class SeloreShaderPatcherEditor : UnityEditor.Editor {

    private Renderer _autoDiscoveredRenderer;

    public override VisualElement CreateInspectorGUI() {
        var root = new VisualElement();
        var target = serializedObject.targetObject as SeloreShaderPatcher;

        // --- Renderer selection --------------------------------------------
        var rendererFieldWrapper = new VisualElement();
        var findRendererProp = serializedObject.FindProperty("findRenderer");
        rendererFieldWrapper.Add(new PropertyField(findRendererProp, "Automatically find Renderer"));

        var rendererField = new PropertyField(serializedObject.FindProperty("renderer"), "Renderer");
        var autoRendererField = new ObjectField("Renderer") {
            objectType = typeof(Renderer),
        };
        autoRendererField.SetEnabled(false);
        autoRendererField.SetValueWithoutNotify(_autoDiscoveredRenderer);
        
        rendererFieldWrapper.Add(rendererField);
        rendererFieldWrapper.Add(autoRendererField);
        
        void UpdateRendererFields(SerializedProperty property) {
            rendererField.style.display     = property.boolValue ? DisplayStyle.None : DisplayStyle.Flex;
            autoRendererField.style.display = property.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        UpdateRendererFields(findRendererProp);

        rendererFieldWrapper.TrackPropertyValue(findRendererProp, UpdateRendererFields);
        root.Add(rendererFieldWrapper);

        // --- Other Renderer Settings --------------------------------------
        root.Add(new PropertyField(serializedObject.FindProperty("shaderToPatch"), "Shader to Patch"));
        root.Add(new PropertyField(serializedObject.FindProperty("featureAutoRigging"), "Auto Rig"));
        root.Add(new PropertyField(serializedObject.FindProperty("autoConfigureBounds"), "Auto Configure Bounds"));

        // --- Penetrator parameters -----------------------------------------
        root.Add(Utils.Header("Penetrator Parameters"));
        root.Add(new PropertyField(serializedObject.FindProperty("deformStrength"), "Deform Strength"));
        root.Add(new PropertyField(serializedObject.FindProperty("channel"), "Channel"));
        root.Add(new PropertyField(serializedObject.FindProperty("allTheWayThrough"), "All The Way Through"));

        Utils.AddOverrideRow(root, serializedObject, "overrideStartPosition", "startPosition", "Start Position");
        Utils.AddOverrideRow(root, serializedObject, "overrideStartRotation", "startRotation", "Start Rotation");
        Utils.AddOverrideRow(root, serializedObject, "overrideLength",         "length",        "Length");

        // --- Advanced / feature toggles ------------------------------------
        root.Add(Utils.Header("Properties (animatable)"));
        root.Add(new PropertyField(serializedObject.FindProperty("deformationEnabled"), "Deformation Enabled (0/1)"));

        var advancedContainer = new VisualElement();
        advancedContainer.Add(Utils.Header("Features"));
        advancedContainer.Add(Utils.InfoBox(
            "You probably don't want to change these unless you know what you are doing.\n" +
            "Enabling the Tip Light or disabling contacts may break features."));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureDeformationEnabled"), "Enable Deformation"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureTipLight"), "Enable Tip Light"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureContactSenders"), "Enable Contact Senders"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureToyContactReceivers"), "Enable Toy Contact Receivers"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("keepImports"), "Keep #include directives in patched shader (debug)"));

        var advancedFoldout = new Foldout {
            text = "Advanced",
        };
        advancedFoldout.contentContainer.Add(advancedContainer);
        root.Add(advancedFoldout);

        // --- Build Summary ---
        var summaryBox = Utils.InfoBox();

        var sPatching = new Label();
        var sLength = new Label();
        var sTipLight = new Label();
        var sSenders = new Label();
        var sReceivers = new Label();

        summaryBox.Add(Utils.Header("Build Summary"));
        summaryBox.Add(sPatching);
        summaryBox.Add(sLength);
        summaryBox.Add(sTipLight);
        summaryBox.Add(sSenders);
        summaryBox.Add(sReceivers);
        Utils.CreateToySupportRow(summaryBox, out var overallToySupport, out var toyPlug, out var toyTouch, out var toyFrot);

        var sTarget = (SeloreShaderPatcher)serializedObject.targetObject;

        int CountUniqueShaders() {
            var r = sTarget.findRenderer
                ? _autoDiscoveredRenderer
                : sTarget.renderer;
            if (r == null) return 0;
            return r.sharedMaterials
                .Where(m => m != null)
                .Select(m => m.shader)
                .Distinct()
                .Count();
        }

        Action updateSummary = () => {
            var t = sTarget;
            var shaderTypeName = t.shaderToPatch switch {
                ShaderPatchSelection.AmitySelore => "Amity",
                ShaderPatchSelection.VRCFurySPS => "VRCFury",
                ShaderPatchSelection.RalivDPS => "Raliv",
                ShaderPatchSelection.PoiTPS => "Poiyomi",
                _ => "Unknown"
            };
            int shaderCount = CountUniqueShaders();
            sPatching.text = t.featureDeformationEnabled
                ? $"Patching Shaders [{shaderTypeName}]: {shaderCount}"
                : $"Patching Shaders [{shaderTypeName}]: DISABLED";
            sPatching.style.color = t.featureDeformationEnabled ? Color.white : Color.red;
            sLength.text = $"Length: {t.length:F2}m";
            sLength.style.color = Color.white;
            sTipLight.text = t.featureTipLight ? "Tip Light: ENABLED" : "Tip Light: DISABLED";
            sTipLight.style.color = t.featureTipLight ? Color.green : Color.red;
            sSenders.text = t.featureContactSenders
                ? "Generating Contact Senders: 2"
                : "Generating Contact Senders: 0";
            sReceivers.text = Utils.BuildReceiverCountString(
                t.featureToyContactReceivers,
                t.featureToyContactReceivers,
                t.featureToyContactReceivers,
                t.featureToyContactReceivers
            );
            overallToySupport.style.color = t.featureToyContactReceivers ? Color.green : Color.red;
            toyPlug.style.color = t.featureToyContactReceivers ? Color.green : Color.red;
            toyTouch.style.color = t.featureToyContactReceivers ? Color.green : Color.red;
            toyFrot.style.color = t.featureToyContactReceivers ? Color.green : Color.red;
        };
        updateSummary();

        foreach (var p in new[] {
                     serializedObject.FindProperty("featureDeformationEnabled"),
                     serializedObject.FindProperty("featureTipLight"),
                     serializedObject.FindProperty("featureContactSenders"),
                     serializedObject.FindProperty("featureToyContactReceivers"),
                     serializedObject.FindProperty("overrideLength"),
                     serializedObject.FindProperty("length"),
                     serializedObject.FindProperty("shaderToPatch"),
                     
                 }) {
            summaryBox.TrackPropertyValue(p, _ => updateSummary());
        }

        root.Add(summaryBox);

        return root;
    }

}
}
