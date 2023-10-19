using System;
using System.Collections.Generic;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    
    [AddComponentMenu("Tayou Tools/Item Setup")]
    public class ItemSetup : AmityBaseComponent {
        
        public List<ItemData> targets = new List<ItemData>();
        public bool itemDefaultActiveState;
        public int itemPreviewIndex = -1;

        public Vector3 restPosition;
        public Quaternion restRotation;

        public Vector3 EulerAngles {
            get => restRotation.eulerAngles;
            set => restRotation = Quaternion.Euler(value);
        }

        private void Awake() {
            if (itemPreviewIndex == -1) {
                restPosition = transform.position;
                restRotation = transform.rotation;
            }
        }
    }
}