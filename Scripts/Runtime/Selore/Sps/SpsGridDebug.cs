// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;

namespace org.Tayou.AmityEdits {
    [AddComponentMenu("Amity Edits/Debug/SPS Grid Debug Overlay")]
    public class SpsGridDebug : AmityBaseComponent {
        [Header("Visibility")]
        public bool showOverlay = true;
        public bool showSockets = true;
        public bool showPlugs;
        public bool showRing = true;
        public bool showArrow = true;
        public bool showTags = true;
        public bool showChainLinks = true;

        [Header("Appearance")]
        [Range(0.1f, 10f)]
        public float gizmoScale = 1f;
        [Range(0.5f, 8f)]
        public float lineWidthPixels = 2f;
        public bool depthTested;
        public float radius = 5f;
        public float fadeWidth = 0.3f;

        [Header("Colors")]
        public Color holeColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        public Color ringColor = new Color(0.2f, 0.5f, 1f, 0.9f);
        public Color reversibleColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        public Color plugColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        public Color chainColor = new Color(1f, 1f, 1f, 0.6f);
    }
}
