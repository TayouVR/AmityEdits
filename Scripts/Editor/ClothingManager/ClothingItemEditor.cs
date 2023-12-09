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