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

using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    /**
     * This component represents a DPS/TPS orifice or SPS Socket
     */
    [AddComponentMenu("Amity Edits/Selore Hole")]
    public class SeloreHole : AmityBaseComponent {

        /// The object, where the orifice should be placed
        public Transform targetObject;
        
        public string depthParameterName;
        public string penetratorWidthParameterName;
        public string penetratorLengthParameterName;
        
        // features to generate
        public bool featureLights = true;
        public bool featureContactSenders = true;
        public bool featureToyContactReceivers = true;

        // animation - these properties may be animated.
        public bool enableDeformation;
        public bool enableContactSenders;
        public bool enableToyContacts;
        public SeloreChannel channel;
        public SeloreRole role;
    }

    public enum SeloreChannel {
        DpsChannel0 = 0,
        DpsChannel1 = 1,
    }

    public enum SeloreRole {
        Hole,
        Ring,
        ReversibleRing,
    }
}