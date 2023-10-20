using nadena.dev.ndmf.util;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(Outfit))]
    public class OutfitEditor : AmityBaseEditor {
        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();

            root.Add(new PropertyField(serializedObject.FindProperty("name")));
            root.Add(new PropertyField(serializedObject.FindProperty("ClothingItems")));

            return root;
        }
    }
}