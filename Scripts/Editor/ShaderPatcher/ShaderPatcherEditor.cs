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
using UnityEngine;
using UnityEngine.UIElements;
using org.Tayou.AmityEdits;

namespace org.Tayou.AmityEdits {

[CustomEditor(typeof(SPSPlug))]
public class ShaderPatcherEditor : Editor {
    
    public override VisualElement CreateInspectorGUI() {
        var root = new VisualElement();
        var target = serializedObject.targetObject as SPSPlug;

        var rendererField = new ObjectField("Renderer");
        rendererField.objectType = typeof(Renderer);
        rendererField.BindProperty(serializedObject.FindProperty("renderer"));
        root.Add(rendererField);
        
        

        /* Whether to patch the shader to deform the mesh */
        var featureDeformationEnabled = new Toggle("Enable Deformation");
        root.Add(featureDeformationEnabled);
        
        var shaderPatchContainer = new VisualElement();
        
        var shaderSelectionField = new EnumField("Shader", ShaderPatchSelection.AmitySPS);
        rendererField.BindProperty(serializedObject.FindProperty("shaderToPatch"));
        shaderPatchContainer.Add(shaderSelectionField);
        
        /* automatically rig non-rigged meshes based on plug orientation and length for physics */
        bool featureAutoRigging;
        /* automatically configure the renderer bounds to well working values for deformation */
        bool autoConfigureBounds;
        
        
        root.Add(shaderPatchContainer);

        /* If contact senders for triggering socket actions */
        bool featureContactSenders;
        /* If contact receivers for interpreting by OSCGoesBrr should be added */
        bool featureToyContactReceivers;
        /* if contacts to expose depth and width to the animator should be generated */
        bool featureDepthContactReceivers;
            
        /* animate shader deformation, treated as bool - 0 or 1 */
        float deformationEnabled;

        bool spsOverrun;
        bool spsKeepImports;
        
        
        // fallback: base.CreateInspectorGUI()
        return root;
    }
    
    
}
}