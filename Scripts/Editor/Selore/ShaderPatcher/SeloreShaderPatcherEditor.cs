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

        AddOverrideRow(root, "overrideStartPosition", "startPosition", "Start Position");
        AddOverrideRow(root, "overrideStartRotation", "startRotation", "Start Rotation");
        AddOverrideRow(root, "overrideLength",         "length",        "Length");

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

        return root;
    }

    /// Render an "override?" toggle plus a value field that is only shown
    /// when the toggle is on.
    private void AddOverrideRow(VisualElement root, string overrideProp, string valueProp, string label) {
        var toggleProp = serializedObject.FindProperty(overrideProp);
        var valueSerialized = serializedObject.FindProperty(valueProp);

        var row = new VisualElement();
        row.Add(new PropertyField(toggleProp, $"Override {label}"));
        var valueField = new PropertyField(valueSerialized, label);
        row.Add(valueField);

        UpdateOverrideValueField(toggleProp, valueField);

        row.TrackPropertyValue(toggleProp, property => {
            UpdateOverrideValueField(property, valueField);
        });
        
        root.Add(row);
    }

    private static void UpdateOverrideValueField(SerializedProperty toggleProp, VisualElement valueField) {
        valueField.style.display = toggleProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
