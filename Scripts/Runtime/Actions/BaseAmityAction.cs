using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits.Actions {
    public enum ParameterSelection {
        Main,
        Sub1,
        Sub2,
        Sub3,
        Sub4
    }

    [Serializable]
    public class BaseAmityAction {
        public string name;
        public ParameterSelection parameterSelection = ParameterSelection.Main;
        
        public virtual void Execute() {
            Debug.Log("This is a base action, it should not be executed.");
        }

        public virtual VisualElement CreateInspector() {
            return new Label("This is a base action, it should not be inspected.");
        }
    }
}