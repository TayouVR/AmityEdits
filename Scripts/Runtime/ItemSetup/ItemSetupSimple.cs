using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    public class ItemSetupSimple : MonoBehaviour {
        
        public bool itemDefaultActiveState;
        public ItemPreviewSelection itemPreviewSelection = ItemPreviewSelection.RestPosition;
        public HumanBodyBones leftHand = HumanBodyBones.LeftHand;
        public HumanBodyBones rightHand = HumanBodyBones.RightHand;
        public ItemData resetPos;

        public enum ItemPreviewSelection {
            RestPosition,
            LeftHand,
            RightHand
        }
    }
}