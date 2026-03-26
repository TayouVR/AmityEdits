// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions {
    public enum MaterialPropertyType {
        Float,
        Color,
        Vector,
        Texture
    }
    
    [Serializable]
    public class MaterialPropertyAction : BaseAmityAction {
        public string propertyName;
        public MaterialPropertyType propertyType;
        
        public float floatValue;
        public float defaultFloatValue = 0f;
        
        public Color colorValue = Color.white;
        public Color defaultColorValue = Color.white;
        
        public Vector4 vectorValue = Vector4.zero;
        public Vector4 defaultVectorValue = Vector4.zero;
        
        public Texture textureValue;
        
        public bool useCurve = false;
        public AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        public Renderer targetRenderer;
        public bool applyToAllRenderers;
    }
}
