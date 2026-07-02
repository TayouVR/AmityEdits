// SPDX-License-Identifier: GPL-3.0-only
using System;

namespace org.Tayou.AmityEdits {
    [Serializable]
    public class SpsTagRule {
        public string tag;
        public bool allowSelf = true;
        public bool allowOthers = true;
    }
}
