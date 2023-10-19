using System;
using System.Collections.Generic;
using UnityEngine;
using VF.Component;
#if UNITY_EDITOR
using VF.Utils.Controller;
#endif

namespace org.Tayou.AmityEdits {
    [Serializable]
    [AddComponentMenu("Tayou Tools/ClothingManager Item")]
    public class ClothingItem : MonoBehaviour {
        public string name;
        //public VF.Model.State action; // TODO: implement UI for it somehow, or re-implement entire state/action
        //public GameObject gameObject;
        public AnimationClip animation;
        public List<ClothingItem> incompatibilities;
#if UNITY_EDITOR
        public VFABool parameter;
#endif
    }
}