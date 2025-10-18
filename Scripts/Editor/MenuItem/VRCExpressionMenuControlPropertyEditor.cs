using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3A.Editor.Elements;

namespace org.Tayou.AmityEdits.MenuItem {
    
    // [CustomPropertyDrawer(typeof(VRCExpressionsMenu.Control), false)]
    // public class VRCExpressionMenuControlPropertyEditor : PropertyDrawer {
    //     public VisualTreeAsset Uxml;
    //     [HideInInspector] public VRCExpressionsMenu Menu;
    //     private VisualElement root;
    //     private VisualElement ControlOptionsContainer;
    //     
    //     
    //     public override VisualElement CreatePropertyGUI(SerializedProperty property) {
    //         root = new VisualElement();
    //         if (Uxml == null)
    //         {
    //             Uxml = Resources.Load<VisualTreeAsset>("VRCExpressionsMenu");
    //         }
    //         Uxml.CloneTree(root);
    //         
    //         Menu = (property.serializedObject.targetObject as MenuItem).gameObject.GetComponentInParent<VRCAvatarDescriptor>().expressionsMenu;
    //         ControlOptionsContainer = root.Q<VisualElement>("ControlOptionsContainer");
    //         var controlOptions = new ExpressionsControlOptions(property, new VRCExpressionsMenu());
    //         ControlOptionsContainer.Add(controlOptions);
    //         root.Add(ControlOptionsContainer);
    //
    //
    //         return root;
    //     }
    // }
}