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
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEngine.Animations;

namespace org.Tayou.AmityEdits {
    
    public class ItemSetupPass {
        
        private readonly BuildContext _buildContext;

        public ItemSetupPass(BuildContext context) {
            _buildContext = context;
        }
        
        public void Process() {
            var avatarDescriptor = _buildContext.AvatarDescriptor;
            var itemSetupComponents =
                avatarDescriptor.GetComponentsInChildren<ItemSetup>(true);

            if (itemSetupComponents.Length == 0) return;

            foreach (var itemSetup in itemSetupComponents) {
                // Create Dummy Objects at targets in Hierarchy and Position using saved Positions
                List<GameObject> targetObjects = new List<GameObject>();
                foreach (var target in itemSetup.targets) {
                    GameObject dummyObject = new GameObject();
                    dummyObject.transform.position = target.position;
                    dummyObject.transform.rotation = target.rotation;
                    targetObjects.Add(dummyObject);
                }
                
                // Create ParentConstraint and assign targets.
                ParentConstraint parentConstraint = itemSetup.gameObject.AddComponent<ParentConstraint>();
                parentConstraint.SetSources(targetObjects.Select(target => new ConstraintSource {
                    sourceTransform = target.transform,
                    weight = 0
                }).ToList());
                // Set Offsets all to 0, not strictly needed as they will be 0 by default
                parentConstraint.rotationOffsets = new Vector3[targetObjects.Count];
                parentConstraint.translationOffsets = new Vector3[targetObjects.Count];
                // lock and activate
                parentConstraint.locked = true;
                parentConstraint.constraintActive = true;

            }
        }


    }
}