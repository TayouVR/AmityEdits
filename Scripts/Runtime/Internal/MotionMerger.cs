using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace org.Tayou.AmityEdits.Internal {
    /**
     * Merges a Motion (AnimationClip or BlendTree) into the Avatars Animator(s) 
     */
    public class MotionMerger : MonoBehaviour, IVirtualizeMotion {
        public int LayerPriority { get; set; }
        public VRCAvatarDescriptor.AnimLayerType LayerType { get; set; }
        public Motion Motion { get; set; }
        public string GetMotionBasePath(object ndmfBuildContext, bool clearPath = true) {
            return "";
        }
    }
}