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
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.MenuItem {
    [Serializable]
    [AddComponentMenu("Amity Edits/Menu Item")]
    public class MenuItem : AmityBaseComponent {
        
        public PathMethod pathMethod;
        
        // parent
        public VRCExpressionsMenu parentMenu;
        
        // path
        public string menuPath;
        
        // menu options
        public VRCExpressionsMenu.Control vrcMenuControl;
        
        // actions
        public BaseAmityAction[] actions;
        
    }


    public enum PathMethod {
        Parent,
        Path,
    }
}