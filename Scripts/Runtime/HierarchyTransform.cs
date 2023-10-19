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