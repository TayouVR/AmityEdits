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
using UnityEngine;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.ShaderPatcher;
using UnityEditor.Animations;

namespace org.Tayou.AmityEdits {
    
    public class SPSPlugPass {
        
        private readonly BuildContext _buildContext;

        public SPSPlugPass(BuildContext context) {
            _buildContext = context;
        }

        public void Process() {
            var avatarDescriptor = _buildContext.AvatarDescriptor;

            var components = avatarDescriptor.GetComponentsInChildren<SPSPlug>(true);

            if (components.Length == 0) return;
            
            foreach (var plugObject in components) {
                if (plugObject.renderer == null) {
                    Debug.Log($"No Renderer set for SPS plug object at {plugObject.transform}");
                    continue;
                }

                foreach (var material in plugObject.renderer.sharedMaterials) {
                    SpsConfigurer.ConfigureSpsMaterial(
                        _buildContext, 
                        plugObject.renderer, 
                        material,
                        1, 
                        new Texture2D(1, 1), 
                        plugObject, 
                        plugObject.gameObject, 
                        new List<string>()
                    );
                    SpsPatcher.Patch(material, _buildContext, true, plugObject.shaderToPatch);
                }
            }
        }
    }
}