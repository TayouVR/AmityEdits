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
using UnityEngine;
using UnityEngine.Serialization;

namespace org.Tayou.AmityEdits {
    [Serializable]
    public class ItemData {

        // these are all used for the same purpose, some of these may be easier to deal with than others
        public HumanBodyBones humanBone;
        public string transformPath;
        public Transform transform;
        
        [SerializeField]
        public HierarchyTransform path = new HierarchyTransform();
        
        public Vector3 position;
        public Quaternion rotation;

        public Vector3 EulerAngles {
            get => rotation.eulerAngles;
            set => rotation = Quaternion.Euler(value);
        }
    }
}