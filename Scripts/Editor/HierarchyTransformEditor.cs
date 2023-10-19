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