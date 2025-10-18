// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions {
    [Serializable]
    public class MaterialSwapAction : BaseAmityAction {
        public Renderer targetRenderer;
        public bool applyToAllRenderers;
        public Material fromMaterial;
        public Material toMaterial;
    }
}
