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
        
        var findRendererProp = serializedObject.FindProperty("findRenderer");
        root.Add(new PropertyField(findRendererProp, "Automatically find Renderer"));
        var rendererFieldWrapper = new VisualElement();
        root.Add(rendererFieldWrapper);
        
        var rendererField = new PropertyField(serializedObject.FindProperty("renderer"), "Renderer");
        var autoRendererField = new ObjectField("Renderer") {
            objectType = typeof(Renderer),
        };
        autoRendererField.SetEnabled(false);
        autoRendererField.SetValueWithoutNotify(_autoDiscoveredRenderer);
        rendererFieldWrapper.Add(findRendererProp.boolValue ? autoRendererField : rendererField);
        
        root.Add(new PropertyField(serializedObject.FindProperty("shaderToPatch"), "Shader to Patch"));
        root.Add(new PropertyField(serializedObject.FindProperty("featureAutoRigging"), "Auto Rig"));
        root.Add(new PropertyField(serializedObject.FindProperty("autoConfigureBounds"), "Auto Configure Bounds"));
        
        
        root.Add(Utils.Header("Properties (animatable)"));
            
        var advancedContainer = new VisualElement();
        advancedContainer.Add(Utils.Header("Features"));
        advancedContainer.Add(Utils.InfoBox("You Probably don't want to change these, unless you know what you are doing.\n" +
                                            "Enabling the Tip Light or disabling contacts may break features."));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureDeformationEnabled"), "Enable Deformation"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureTipLight"), "Enable Tip Light"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureContactSenders"), "Enable Contact Senders"));
        advancedContainer.Add(new PropertyField(serializedObject.FindProperty("featureToyContactReceivers"), "Enable Toy Contact Receivers"));

        var advancedFoldout = new Foldout {
            text = "Advanced",
        };
        advancedFoldout.contentContainer.Add(advancedContainer);
        root.Add(advancedFoldout);

        bool spsOverrun;
        bool spsKeepImports;
        
        root.TrackPropertyValue(findRendererProp, _ => {
            if (findRendererProp.boolValue) {
                rendererFieldWrapper.Remove(rendererField);
                rendererFieldWrapper.Add(autoRendererField);
            } else {
                rendererFieldWrapper.Remove(autoRendererField);
                rendererFieldWrapper.Add(rendererField);
            }
        });
        
        // fallback: base.CreateInspectorGUI()
        return root;
    }
    
    
}
}