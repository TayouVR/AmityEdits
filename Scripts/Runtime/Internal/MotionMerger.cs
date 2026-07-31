using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace org.Tayou.AmityEdits.Internal {
    /**
     * Merges a Motion (AnimationClip or BlendTree) into the Avatars Animator(s) 
     */
    public class MotionMerger : AmityBaseComponent, IVirtualizeMotion {
        [SerializeField] private int _layerPriority;
        [SerializeField] private VRCAvatarDescriptor.AnimLayerType _layerType = VRCAvatarDescriptor.AnimLayerType.FX;
        [SerializeField] private Motion _motion;
        [SerializeField] private Locality _locality = Locality.Both;

        public int LayerPriority { get => _layerPriority; set => _layerPriority = value; }
        public VRCAvatarDescriptor.AnimLayerType LayerType { get => _layerType; set => _layerType = value; }
        public Motion Motion { get => _motion; set => _motion = value; }
        public Locality Locality { get => _locality; set => _locality = value; }

        public string GetMotionBasePath(object ndmfBuildContext, bool clearPath = true) {
            return "";
        }
    }
}