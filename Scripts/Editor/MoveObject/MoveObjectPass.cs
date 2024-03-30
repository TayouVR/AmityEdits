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
using AnimatorAsCode.V1;
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
            foreach (var layer in ac.layers) {
                foreach (var state in layer.stateMachine.states) {
                    var clips = state.state.motion as AnimationClip;
                    if (clips == null) continue;

                    var bindings = AnimationUtility.GetCurveBindings(clips);
                    foreach (var binding in bindings.Where(b => b.path == oldPath)) {
                        var curve = AnimationUtility.GetEditorCurve(clips, binding);
                        // if needed, perform deep copy of keyframes - only potentially needed if altering values
                        //curve.keys = curve.keys.Select(k => new Keyframe {time = k.time,value = k.value}).ToArray();

                        // Save to local variable, modify and then add it back to the bindings
                        var modifiedBinding = binding;
                        modifiedBinding.path = newPath;
                        AnimationUtility.SetEditorCurve(clips, binding, null);
                        AnimationUtility.SetEditorCurve(clips, modifiedBinding, curve);
                    }
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