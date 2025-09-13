using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CreateAssetMenu(fileName = "en-EN", menuName = "Amity/Translation", order = 0)]
    public class TranslationLanguage : ScriptableObject {
        public string displayName;
        public string languageCode;
        
        /**
         * Translation data, encoded as JSON for better portability
         */
        public string translationData;

        [NonSerialized]
        private TranslationData _deserializedData;
        
        public TranslationData DeserializedData {
            get {
                return _deserializedData ?? (_deserializedData = Deserialize());
            }
            set {
                _deserializedData = value;
            }
        }
        
        public Dictionary<string, string> Translations {
            get {
                return DeserializedData.translations;
            }
            set {
                DeserializedData.translations = value;
            }
        }

        private TranslationData Deserialize() {
            return JsonUtility.FromJson<TranslationData>(translationData);
        }
    }

    public class TranslationData {
        public Dictionary<string, string> translations;
    }

    [CustomEditor(typeof(TranslationLanguage), true)]
    public class TranslationLanguageEditor : UnityEditor.Editor {
        public TranslationLanguage language;

        public TranslationLanguageEditor()
        {
            language = (TranslationLanguage) target;
        }

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            
            root.Add(new PropertyField(serializedObject.FindProperty("displayName")));
            root.Add(new PropertyField(serializedObject.FindProperty("languageCode")));
            root.Add(new PropertyField(serializedObject.FindProperty("Translations")));

            return root; //base.CreateInspectorGUI();
        }

        private VisualElement BuildTranslationList() {
            var root = new ListView();
            
            // hook dictionary into list somehow
            
            return root;
        }
    }
}