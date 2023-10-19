using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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

        public override void OnInspectorGUI() {
            //DrawHeader(new Rect());

            DrawInspector();
        }

        public abstract void DrawInspector();
    }
}