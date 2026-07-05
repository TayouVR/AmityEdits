// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;

namespace org.Tayou.AmityEdits {
    [AddComponentMenu("Amity Edits/Debug/SPS Grid Debug Overlay")]
    public class SpsGridDebug : AmityBaseComponent {
        [Header("Visibility")]
        public bool showOverlay = true;
        public bool showRing = true;
        public bool showArrow = true;
        public bool showTags = true;
        public bool showChainLinks = true;

        [Header("Colors")]
        public Color holeColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        public Color ringColor = new Color(0.2f, 0.5f, 1f, 0.9f);
        public Color reversibleColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        public Color plugColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        public Color chainColor = new Color(1f, 1f, 1f, 0.6f);
    }
}
