using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    
    [CustomEditor(typeof(ItemSetupSimple), true)]
    public class ItemSetupSimpleEditor : Editor {
        /*protected override void OnHeaderGUI() {
            ((VRCFuryItemSetupSimple) target).enabled = GUILayout.Toggle(((VRCFuryItemSetupSimple) target).enabled, "");
            GUI.Box(new Rect(0, 0, 200, EditorGUIUtility.singleLineHeight), new GUIContent("Tayou"));
            InvokeMethod(this, "OnHeaderGUI", this, "Simple Item Setup", 200);
        }
        
        private static object InvokeMethod(object targetObject, string methodName, params object[] parameters) {
            return targetObject.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(targetObject, parameters);
        }*/

        public override VisualElement CreateInspectorGUI() {
            
            // Each editor window contains a root VisualElement object
            VisualElement root = new VisualElement();

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            //VisualElement label = new Label("Hello World! From C#");
            //root.Add(label);

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/org.tayou.vrcfury-extensions/Scripts/Editor/ItemSetupSimpleEditor.uxml");
            VisualElement uxmlFileContents = visualTree.CloneTree();
            root.Add(uxmlFileContents);
            switch (((ItemSetupSimple)target).itemPreviewSelection) {
                case ItemSetupSimple.ItemPreviewSelection.RestPosition:
                    break;
                case ItemSetupSimple.ItemPreviewSelection.LeftHand:
                    break;
                case ItemSetupSimple.ItemPreviewSelection.RightHand:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/org.tayou.vrcfury-extensions/Scripts/Editor/ItemSetupSimpleEditor.uss");
            //VisualElement labelWithStyle = new Label("Hello World! With Style");
            //labelWithStyle.styleSheets.Add(styleSheet);
            //root.Add(labelWithStyle);
            
            return root;
        }
    }
}