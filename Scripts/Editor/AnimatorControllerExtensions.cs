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
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace org.Tayou.AmityEdits {
    public static class AnimatorControllerExtensions {
        public static AnimatorControllerLayer NewLayer(this AnimatorController controller, 
            string name = "") {
            AnimatorControllerLayer layer = new AnimatorControllerLayer {
                name = name
            };
            layer.stateMachine = new AnimatorStateMachine { name = name};
            
            controller.AddLayer(layer);
            return layer;
        }
        
        public static AnimatorControllerParameter NewParameter(this AnimatorController controller, 
            string name = "", 
            AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool) {
            AnimatorControllerParameter parameter = new AnimatorControllerParameter {
                name = name,
                type = type
            };
            
            controller.AddParameter(parameter);
            return parameter;
        }
        
        public static AnimatorState NewState(this AnimatorControllerLayer layer, 
            string name = "") {
            AnimatorState state = layer.stateMachine.AddState(name);
            return state;
        }
        
        public static AnimatorState NewDirectTreeState(this AnimatorControllerLayer layer, 
            out BlendTree blendTree, 
            AnimatorController controller,
            string name = "") {
            blendTree = new BlendTree();
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            blendTree.blendType = BlendTreeType.Direct;

            var state = layer.NewState("Clothing Toggles");
            state.motion = blendTree;
            return state;
        }
        
        public static AnimatorState Drives(this AnimatorState state, 
            AnimatorControllerParameter parameter,
            float value) {
            VRCAvatarParameterDriver driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter() {
                name = parameter.name,
                destParam = parameter, 
                value = value,
            });
            return state;
        }
        
        public static bool HasBlendshape(this Renderer renderer, string name) {
            return renderer.GetBlendShapeIndex(name) >= 0 ;
        }
        
        public static int GetBlendShapeIndex(this Renderer renderer, string name) {
            return renderer.GetMesh()?.GetBlendShapeIndex(name) ?? -1;
        }

        [CanBeNull]
        public static Mesh GetMesh(this Renderer renderer) {
            if (renderer is SkinnedMeshRenderer skin) {
                if (skin.sharedMesh == null) return null;
                return skin.sharedMesh;
            }

            if (renderer is MeshRenderer) {
                var owner = renderer.gameObject;
                var filter = owner.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) return null;
                return filter.sharedMesh;
            }

            return null;
        }

        public static int GetVertexCount(this Renderer renderer) {
            var mesh = renderer.GetMesh();
            if (mesh == null) return 0;
            return mesh.vertexCount;
        }
        
        public static float GetBlendShapeWeight(this SkinnedMeshRenderer skin, string name) {
            var id = skin.GetBlendShapeIndex(name);
            if (id < 0) throw new Exception("Blendshape does not exist");
            return skin.GetBlendShapeWeight(id);
        }
        
        public static void SetBlendShapeWeight(this SkinnedMeshRenderer skin, string name, float value) {
            var id = skin.GetBlendShapeIndex(name);
            if (id < 0) throw new Exception("Blendshape does not exist");
            skin.SetBlendShapeWeight(id, value);
        }
    }
}