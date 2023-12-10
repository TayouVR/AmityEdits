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
using UnityEditor;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(HierarchyTransform))]
    public class HierarchyTransformEditor : Editor {
        private HierarchyTransform Target => (HierarchyTransform) target;
        
        private bool isTransform;
        private bool isInitialized;

        public override void OnInspectorGUI() {
            if (!isInitialized) {
                isInitialized = true;
                isTransform = Target.transform != null;
            }
            serializedObject.Update();
            
            EditorGUILayout.BeginHorizontal();
            {
                isTransform = EditorGUILayout.ToggleLeft("", isTransform);
                if (isTransform) {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("transform"));
                } else {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("humanBone"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transformPath"));
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}