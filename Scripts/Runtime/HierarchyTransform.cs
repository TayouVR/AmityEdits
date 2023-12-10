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
using UnityEngine;
using Object = UnityEngine.Object;

namespace org.Tayou.AmityEdits {
    
    /// <summary>
    /// Stores three ways to get to a transform
    /// It will try to access them in the following order:
    /// - Human Bone
    /// - Transform
    /// - Transform Path
    /// </summary>
    [Serializable]
    public class HierarchyTransform : Object {
        public HumanBodyBones humanBone;
        public string transformPath;
        public Transform transform;
        public bool useTransform;
        
#if UNITY_EDITOR
        public static void OnInspectorGUI(SerializedObject serializedObject) {
            if (serializedObject.targetObject.GetType() != typeof(HierarchyTransform)) {
                Debug.LogWarning("Tried drawing editor for invalid object type");
                return;
            }
            
            SerializedProperty useTransformProp = serializedObject.FindProperty("useTransform");
            
            serializedObject.Update();
            
            EditorGUILayout.BeginHorizontal();
            {
                useTransformProp.boolValue = EditorGUILayout.ToggleLeft("", useTransformProp.boolValue);
                if (useTransformProp.boolValue) {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("transform"));
                } else {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("humanBone"));
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transformPath"));
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}