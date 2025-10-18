// SPDX-License-Identifier: GPL-3.0-only
using System;
using UnityEngine;

namespace org.Tayou.AmityEdits.Actions {
    [Serializable]
    public class AnimationSlotAction : BaseAmityAction {
        public string slotName;
        public AnimationClip clip;
    }
}
