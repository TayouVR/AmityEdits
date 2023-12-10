﻿// SPDX-License-Identifier: GPL-3.0-only
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
using System.Collections.Generic;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    [AddComponentMenu("Tayou Tools/Item Setup")]
    public class ItemSetup : AmityBaseComponent {
        
        public List<ItemData> targets = new List<ItemData>();
        public bool itemDefaultActiveState;
        public int itemPreviewIndex = -1;

        public Vector3 restPosition;
        public Quaternion restRotation;

        public Vector3 EulerAngles {
            get => restRotation.eulerAngles;
            set => restRotation = Quaternion.Euler(value);
        }

        private void Awake() {
            if (itemPreviewIndex == -1) {
                restPosition = transform.position;
                restRotation = transform.rotation;
            }
        }
    }
}