// SPDX-License-Identifier: GPL-3.0-only
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits.Actions.Editor {
    [CustomPropertyDrawer(typeof(MaterialPropertyAction))]
    public class MaterialPropertyActionDrawer : BaseAmityActionDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            // Let base build header and children, then tweak visibility per type
            var root = base.CreatePropertyGUI(property);

            void UpdateVisibility() {
                var body = root.Q("ActionBodyContainer");
                if (body == null) return;
                var typeProp = property.FindPropertyRelative("propertyType");
                var floatField = FindField(body, "floatValue");
                var defaultFloatField = FindField(body, "defaultFloatValue");
                
                var colorField = FindField(body, "colorValue");
                var defaultColorField = FindField(body, "defaultColorValue");
                
                var vectorField = FindField(body, "vectorValue");
                var defaultVectorField = FindField(body, "defaultVectorValue");
                
                var textureField = FindField(body, "textureValue");

                if (floatField != null) floatField.style.display = DisplayStyle.None;
                if (defaultFloatField != null) defaultFloatField.style.display = DisplayStyle.None;
                
                if (colorField != null) colorField.style.display = DisplayStyle.None;
                if (defaultColorField != null) defaultColorField.style.display = DisplayStyle.None;
                
                if (vectorField != null) vectorField.style.display = DisplayStyle.None;
                if (defaultVectorField != null) defaultVectorField.style.display = DisplayStyle.None;
                
                if (textureField != null) textureField.style.display = DisplayStyle.None;

                switch ((MaterialPropertyType)typeProp.enumValueIndex) {
                    case MaterialPropertyType.Float:
                        if (floatField != null) floatField.style.display = DisplayStyle.Flex;
                        if (defaultFloatField != null) defaultFloatField.style.display = DisplayStyle.Flex;
                        break;
                    case MaterialPropertyType.Color:
                        if (colorField != null) colorField.style.display = DisplayStyle.Flex;
                        if (defaultColorField != null) defaultColorField.style.display = DisplayStyle.Flex;
                        break;
                    case MaterialPropertyType.Vector:
                        if (vectorField != null) vectorField.style.display = DisplayStyle.Flex;
                        if (defaultVectorField != null) defaultVectorField.style.display = DisplayStyle.Flex;
                        break;
                    case MaterialPropertyType.Texture:
                        if (textureField != null) textureField.style.display = DisplayStyle.Flex;
                        break;
                }
            }

            root.TrackPropertyValue(property.FindPropertyRelative("propertyType"), _ => UpdateVisibility());
            UpdateVisibility();
            return root;
        }
    }
}
