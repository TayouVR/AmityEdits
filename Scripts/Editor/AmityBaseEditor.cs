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
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    
    public static class GUIStyles
    {
        private static Texture2D _blackTexture;
        public static Texture2D BlackTexture {
            get {
                if (_blackTexture == null) {
                    _blackTexture = new Texture2D(1, 1);
                    _blackTexture.SetPixel(0, 0, Color.black);
                    _blackTexture.Apply();
                }
                return _blackTexture;
            }
        }
        
        private static Texture2D _backgroundTexture;
        public static Texture2D BackgroundTexture {
            get {
                if (_backgroundTexture == null) {
                    _backgroundTexture = new Texture2D(1, 1);
                    //_backgroundTexture.SetPixel(0, 0, GUI.backgroundColor);
                    _backgroundTexture.SetPixel(0, 0, new Color(0.24f, 0.24f, 0.24f));
                    _backgroundTexture.Apply();
                }
                return _backgroundTexture;
            }
        }
        
        private static GUIStyle _headerFrontStyle;
        public static GUIStyle HeaderFrontStyle {
            get {
                if (_headerFrontStyle == null) {
                    _headerFrontStyle = new GUIStyle(EditorStyles.boldLabel) {
                        normal = {textColor = new Color(0, 127, 220), background = BlackTexture},
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _headerFrontStyle;
            }
        }
        
        private static GUIStyle _headerStyle;
        public static GUIStyle HeaderStyle {
            get {
                if (_headerStyle == null) {
                    _headerStyle = new GUIStyle(EditorStyles.boldLabel) {
                        normal = {textColor = GUI.color, background = BackgroundTexture},
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 0, 0, 0)
                    };
                }
                return _headerStyle;
            }
        }
    }
    public abstract class AmityBaseEditor : Editor {
        private VisualElement _root;
        private string _currentSelection = "en_US";

        protected void DrawHeader(Rect headerShape) {
            float currentInspectorWidth = this.GetInstanceID() != 0 ? EditorWindow.focusedWindow.position.width : 0;
            Rect headerViewRect = new Rect(20, -21, currentInspectorWidth, 20);
            float usableHeaderWidth = currentInspectorWidth - 100;
            float leftOffset = 20;
        
            GUILayout.BeginHorizontal();
            {
                Rect _rect = new Rect(headerViewRect);
                _rect.width = 50;
                GUI.Box(_rect, new GUIContent("Amity"), GUIStyles.HeaderFrontStyle);
                
                _rect = new Rect(headerViewRect);
                _rect.x += 50;
                _rect.width -= 50;
                GUI.Box(_rect, new GUIContent(GetProperty("targetTitle", typeof(Editor), this) as string), GUIStyles.HeaderStyle);
                
                _rect = new Rect(headerViewRect);
                ((ItemSetup) target).enabled = GUI.Toggle(_rect, ((ItemSetup) target).enabled, "");
            }
            GUILayout.EndVertical();
            // End of custom section
            //InvokeMethod(this, "OnHeaderGUI", this, "Simple Item Setup", 200);
        }
        
        private static object InvokeMethod(object targetObject, string methodName, params object[] parameters) {
            return targetObject.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(targetObject, parameters);
        }

        private static object GetProperty(string name, Type type, object instance) {
            return type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
        }
        
        public override VisualElement CreateInspectorGUI() {
            // Each editor window contains a root VisualElement object
            _root = new VisualElement();

            // Import UXML
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("3d86061ada3c4494f9c0a20d4cadba02"));
            if (visualTree) {
                _root = visualTree.CloneTree();

                Toolbar toolbar = _root.Children().First() as Toolbar;

                if (toolbar?.Children().FirstOrDefault(e => e.name == "ToolName") is Label nameLabel) {
                    nameLabel.text = $"mity v{AmityEditsPlugin.Version}";
                }

                ToolbarSearchField searchField = toolbar?.GetFirstAncestorOfType<ToolbarSearchField>();
                ToolbarMenu languageSelector = toolbar?.GetFirstAncestorOfType<ToolbarMenu>();
                
                CreateCustomInspectorInternal();
                
                if (languageSelector != null) {
                    // TODO populate language menu from files
                

                    // TODO: Actually figure out a way to get a selection from this
                    if (languageSelector.name != _currentSelection) {
                        TranslateUI();
                    }
                }

                if (searchField != null) {
                    if (!string.IsNullOrEmpty(searchField?.value)) {
                        SetVisibilityBasedOnName(_root, searchField.value);
                    }
                }
            } else {
                _root.Add(new Label($"Something went wrong, {AmityEditsPlugin.Name} couldn't be fully loaded, please make sure you have it correctly installed."));
                CreateCustomInspectorInternal();
            }
            
            _root.RegisterCallback<AttachToPanelEvent>(e => {
                RunGUIAfterAttach();
            });
            
            return _root;
        }

        /// <summary>
        /// Initialize UI of actual Component
        /// </summary>
        private void CreateCustomInspectorInternal() {
            VisualElement customInspector = CreateInspector();
            customInspector.name = "InternalInspector";
            _root.Add(customInspector);
        }

        private void RunGUIAfterAttach() {
            if (_root.parent == null) return;
            try {
                // remove padding from parent element to make use of all the space in element
                _root.parent.style.paddingLeft = 0;
                _root.parent.style.paddingRight = 0;
                _root.parent.style.paddingTop = 0;
                _root.parent.style.paddingBottom = 0;

                // add padding back to own element inside
                VisualElement customInspector = _root.Children().First(e => e.name == "InternalInspector");
                customInspector.style.paddingLeft = new StyleLength(15);
                customInspector.style.paddingRight = new StyleLength(6);
                customInspector.style.paddingTop = new StyleLength(2);
                customInspector.style.paddingBottom = new StyleLength(2);
                
                // mess with header
                foreach (var child in _root.parent.parent.Children()) {
                    if (child.name.Contains("Header")) {
                        // this is the header element, we could do something fancy with it... or leave it alone..
                        // sadly its a IMGUI Container, so we can't really edit it from here. Not easily at least
                        //child.visible = false;
                        break;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Translates the entire inspector
        /// </summary>
        private void TranslateUI() {
            TranslateUIInternal(_root);
        }

        
        private void TranslateUIInternal(VisualElement translationRoot) {
            foreach (var child in translationRoot.Children()) {
                // TODO: do translation lookup based on element name
                // need classes to store translation keys and such,
                // maybe as scriptableObject, with a inspector that allows easy in unity editing?
                if (child.childCount > 0) {
                    TranslateUIInternal(child);
                }
            }
        }

        private void SetVisibilityBasedOnName(VisualElement root, string name) {
            foreach (var child in root.Children()) {
                bool isVisible = !child.name.Contains(name);
                child.visible = isVisible;
                if (!isVisible && child.childCount > 0) {
                    SetVisibilityBasedOnName(child, name);
                }
            }
        }

        public virtual VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            
            root.Add(new IMGUIContainer(() => DrawDefaultInspector()));

            return root;
        }
    }
}