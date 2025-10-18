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
        public Color colorValue = Color.white;
        public Vector4 vectorValue = Vector4.zero;
        public Texture textureValue;
        
        public Renderer targetRenderer;
        public bool applyToAllRenderers;
    }
}
