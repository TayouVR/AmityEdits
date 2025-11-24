// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2025 Tayou <git@tayou.org>
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
using org.Tayou.AmityEdits.Actions;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    [AddComponentMenu("Amity Edits/Orifice Depth Action")]
    public class OrificeDepthAction : AmityBaseComponent {
        
        /// Depth actions are using this orifice.
        public Orifice orifice;

        /// should be no less than -1 (1m outside)
        /// should be no more than 3 (3m inside)
        public Vector2 depth;
        /// should be 0 usually, or close to
        /// should be usually no more than 0.5
        public Vector2 penetrationWidth;

        /// Depth is measured as relative to the penetrator. depth min and max are ignored, 0 is not inserted, 1 is fully inserted. Scale should go slightly below 0
        public DepthActionUnits depthActionUnits = DepthActionUnits.Meters;
        
        public OrificeDepthActionState[] actions;
    }

    [Serializable]
    public class OrificeDepthActionState {
        public float depth;
        public float width;
        
        public BaseAmityAction action;
    }
    
    public enum DepthActionUnits {
        Meters,
        Plugs,
        Local
    }
}