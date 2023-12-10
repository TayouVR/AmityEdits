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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(ClothingItem))]
    public class ClothingItemEditor : AmityBaseEditor {
        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            

            root.Add(new PropertyField(serializedObject.FindProperty("name")));
            root.Add(new PropertyField(serializedObject.FindProperty("animation")));
            root.Add(new PropertyField(serializedObject.FindProperty("incompatibilities")));
            root.Add(new PropertyField(serializedObject.FindProperty("parameter")));

            return root;
        }
    }
}