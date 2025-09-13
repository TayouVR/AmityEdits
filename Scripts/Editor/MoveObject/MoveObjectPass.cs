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
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEditor.Animations;

namespace org.Tayou.AmityEdits {
    
    public class MoveObjectPass {
        
        private readonly BuildContext _buildContext;

        public MoveObjectPass(BuildContext context) {
            _buildContext = context;
        }

        /**
         * This method updates the animation path for a given AnimationClip.
         * - 'oldPath' is the path we want to replace in the animation.
         * - 'newPath' is the path we want to replace it with.
         */
        private static void UpdateAnimationPath(AnimatorController ac, string oldPath, string newPath) {
            //Debug.Log(AssetDatabase.GetAssetPath(ac));
            var animationClips = ac.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(state => state.state.motion as AnimationClip)
                .Where(clip => clip != null);
        
            foreach (var clips in animationClips) {
                var bindings = AnimationUtility.GetCurveBindings(clips)
                    .Where(b => b.path == oldPath);
                    
                foreach (var binding in bindings) {
                    var curve = AnimationUtility.GetEditorCurve(clips, binding);
                    var modifiedBinding = binding;
                    modifiedBinding.path = newPath;
                    AnimationUtility.SetEditorCurve(clips, binding, null);
                    AnimationUtility.SetEditorCurve(clips, modifiedBinding, curve);
                }
            }
        }

        public void Process() {
            var avatarDescriptor = _buildContext.AvatarDescriptor;
            
            var animatorControllers = new List<AnimatorController>();
            animatorControllers.AddRange(avatarDescriptor.specialAnimationLayers
                .Where(customAnimLayer => customAnimLayer.animatorController != null)
                .Select(customAnimLayer => (AnimatorController)customAnimLayer.animatorController));
            animatorControllers.AddRange(avatarDescriptor.baseAnimationLayers
                .Where(customAnimLayer => customAnimLayer.animatorController != null)
                .Select(customAnimLayer => (AnimatorController)customAnimLayer.animatorController));

            var components =
                avatarDescriptor.GetComponentsInChildren<MoveObject>(true);

            if (components.Length == 0) return;
            
            foreach (var moveObject in components) {
                var oldPath =
                    AnimationUtility.CalculateTransformPath(moveObject.objectToMove, _buildContext.AvatarRootTransform);
                moveObject.objectToMove.SetParent(moveObject.targetObject);
                var newPath =
                    AnimationUtility.CalculateTransformPath(moveObject.objectToMove, _buildContext.AvatarRootTransform);

                foreach (var animatorController in animatorControllers.Where(animatorController => animatorController != null)) {
                    UpdateAnimationPath(animatorController, oldPath, newPath);
                }
            }
        }
    }
}