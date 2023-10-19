using System;
using System.Collections.Generic;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    [Serializable]
    [AddComponentMenu("Tayou Tools/ClothingManager Outfit")]
    public class Outfit : MonoBehaviour {
        public string name;
        public List<ClothingItem> ClothingItems;
    }
}