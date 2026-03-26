// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits.Actions.Editor {
    [CustomPropertyDrawer(typeof(BaseAmityAction), true)]
    public class BaseAmityActionDrawer : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var root = new VisualElement();
            root.style.marginBottom = 6;
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 6;
            // root.style.borderTopWidth = 1;
            // root.style.borderBottomWidth = 1;
            // root.style.borderLeftWidth = 1;
            // root.style.borderRightWidth = 1;
            // root.style.borderTopColor = new Color(0,0,0,0.3f);
            // root.style.borderBottomColor = new Color(0,0,0,0.3f);
            // root.style.borderLeftColor = new Color(0,0,0,0.3f);
            // root.style.borderRightColor = new Color(0,0,0,0.3f);

            var typeDropdown = new DropdownField("Action Type:");
            var types = TypeCache.GetTypesDerivedFrom<BaseAmityAction>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.IsClass)
                .OrderBy(t => t.Name)
                .ToList();

            List<string> typeNames = types.Select(t => t.Name).ToList();
            typeDropdown.choices = typeNames;

            Type currentType = GetManagedReferenceType(property);
            if (currentType == null) {
                if (property.propertyType == SerializedPropertyType.ManagedReference) {
                    // If null, ensure a default instance exists
                    if (types.Count > 0) {
                        SetManagedReferenceToNewInstance(property, types[0]);
                        currentType = types[0];
                    }
                } else {
                    currentType = fieldInfo.FieldType;
                }
            }
            typeDropdown.value = currentType != null ? currentType.Name : (typeNames.FirstOrDefault() ?? "");

            typeDropdown.RegisterValueChangedCallback(evt => {
                int idx = typeNames.IndexOf(evt.newValue);
                if (idx >= 0) {
                    SetManagedReferenceToNewInstance(property, types[idx]);
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                    RefreshBody(root, property);
                }
            });

            if (property.propertyType == SerializedPropertyType.ManagedReference) {
                root.Add(typeDropdown);
            }

            // Body
            RefreshBody(root, property);

            var parameterSelectionProp = property.FindPropertyRelative("parameterSelection");
            if (parameterSelectionProp != null) {
                root.TrackPropertyValue(parameterSelectionProp, _ => RefreshBody(root, property));
            }

            return root;
        }

        protected VisualElement FindField(VisualElement body, string propName) {
            foreach (var child in body.Children()) {
                if (child is PropertyField pf && (pf.bindingPath == propName || pf.bindingPath.EndsWith($".{propName}"))) return pf;
            }
            return null;
        }

        private static void RefreshBody(VisualElement root, SerializedProperty property) {
            // Remove previous body if exists
            var old = root.Q("ActionBodyContainer");
            if (old != null) root.Remove(old);

            var body = new VisualElement { name = "ActionBodyContainer" };
            body.style.marginTop = 4;

            // Try to find the MenuItem parent to provide better parameter labels
            var menuItem = property.serializedObject.targetObject as org.Tayou.AmityEdits.MenuItem.MenuItem;
            string[] parameterLabels = new string[] { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };
            if (menuItem != null && menuItem.vrcMenuControl != null) {
                if (menuItem.vrcMenuControl.parameter != null && !string.IsNullOrEmpty(menuItem.vrcMenuControl.parameter.name)) {
                    parameterLabels[0] = $"Main ({menuItem.vrcMenuControl.parameter.name})";
                }
                if (menuItem.vrcMenuControl.subParameters != null) {
                    for (int i = 0; i < Math.Min(menuItem.vrcMenuControl.subParameters.Length, 4); i++) {
                        var sp = menuItem.vrcMenuControl.subParameters[i];
                        if (sp != null && !string.IsNullOrEmpty(sp.name)) {
                            parameterLabels[i+1] = $"Sub{i+1} ({sp.name})";
                        }
                    }
                }
            }

            foreach (var child in EnumerateChildren(property)) {
                if (child.name == "name") continue; // already drawn
                
                PropertyField field;
                if (child.type == typeof(ParameterSelection).ToString()) {
                    var dropdown = new PopupField<ParameterSelection>(
                        "Parameter Selection",
                        new List<ParameterSelection> { ParameterSelection.Main, ParameterSelection.Sub1, ParameterSelection.Sub2, ParameterSelection.Sub3, ParameterSelection.Sub4 },
                        (ParameterSelection)child.enumValueIndex,
                        (val) => parameterLabels[(int)val],
                        (val) => parameterLabels[(int)val]
                    );
                    dropdown.RegisterValueChangedCallback(evt => {
                        child.enumValueIndex = (int)evt.newValue;
                        child.serializedObject.ApplyModifiedProperties();
                    });
                    body.Add(dropdown);
                    continue;
                }
                
                field = new PropertyField(child);
                field.Bind(property.serializedObject);
                body.Add(field);
            }

            root.Add(body);
        }

        private static IEnumerable<SerializedProperty> EnumerateChildren(SerializedProperty property) {
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren)) {
                if (SerializedProperty.EqualContents(iterator, endProperty)) break;
                if (iterator.depth <= property.depth) break;
                if (iterator.depth == property.depth + 1) {
                    yield return iterator.Copy();
                }
                enterChildren = false;
            }
        }

        private static Type GetManagedReferenceType(SerializedProperty property) {
            if (property.propertyType != SerializedPropertyType.ManagedReference) return null;
            var fullTypeName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            // Format is: AssemblyName TypeFullName
            var split = fullTypeName.Split(' ');
            if (split.Length != 2) return null;
            var type = Type.GetType(split[1] + ", " + split[0]);
            return type;
        }

        private static void SetManagedReferenceToNewInstance(SerializedProperty property, Type type) {
            try {
                var instance = Activator.CreateInstance(type);
                property.managedReferenceValue = instance;
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}
