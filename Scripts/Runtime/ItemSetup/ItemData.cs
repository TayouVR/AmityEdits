using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace org.Tayou.AmityEdits {
    [Serializable]
    public class ItemData {

        // these are all used for the same purpose, some of these may be easier to deal with than others
        public HumanBodyBones humanBone;
        public string transformPath;
        public Transform transform;
        
        [SerializeField]
        public HierarchyTransform path = new HierarchyTransform();
        
        public Vector3 position;
        public Quaternion rotation;

        public Vector3 EulerAngles {
            get => rotation.eulerAngles;
            set => rotation = Quaternion.Euler(value);
        }
    }
}