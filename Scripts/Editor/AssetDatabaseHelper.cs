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
using UnityEditor;

namespace org.Tayou.AmityEdits {
    public class AssetDatabaseHelper {
        
        private static bool assetEditing = false;
        public static void WithAssetEditing(Action go) {
            if (!assetEditing) {
                AssetDatabase.StartAssetEditing();
                assetEditing = true;
                try {
                    go();
                } finally {
                    AssetDatabase.StopAssetEditing();
                    assetEditing = false;
                }
            } else {
                go();
            }
        }

        public static void WithoutAssetEditing(Action go) {
            if (assetEditing) {
                AssetDatabase.StopAssetEditing();
                assetEditing = false;
                try {
                    go();
                } finally {
                    AssetDatabase.StartAssetEditing();
                    assetEditing = true;
                }
            } else {
                go();
            }
        }
    }
}