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
using JetBrains.Annotations;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    /**
     * This component moves the transform in `objectToMove` into the transform `targetObject`
     */
    public class ReorderMenus : AmityBaseComponent {

        public List<MenuOperation> MenuOperations = new();
    }

    public class MenuOperation {
        [CanBeNull] public MenuLocation SourceMenu;
        [CanBeNull] public MenuLocation TargetMenu;
    }

    public class MenuLocation {
        public string Path;
        public string AssetID;
        public AmityBaseComponent AmityMenuSource;
    }
}