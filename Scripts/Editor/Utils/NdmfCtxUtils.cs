// SPDX-License-Identifier: GPL-3.0-only

using AnimatorAsCode.V1;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.EditorUtils {
    /// <summary>
    /// Helper utilities to extract commonly used data from NDMF BuildContext
    /// so action builders don't need a separate context type.
    /// </summary>
    public static class NdmfCtxUtils {
        public static VRCAvatarDescriptor Descriptor(BuildContext ctx) => ctx.VRChatAvatarDescriptor();
        public static Transform AvatarRoot(BuildContext ctx) => ctx.AvatarRootObject.transform;
        public static Object AssetContainer(BuildContext ctx) => ctx.AssetContainer;
        public static AnimatorController Fx(BuildContext ctx) => AmityMenuUtils.EnsureFxController(Descriptor(ctx));
        public static VRCExpressionParameters Parameters(BuildContext ctx) => Descriptor(ctx).expressionParameters;
    }
    
    // (For AAC 1.2.0 and above) This is recommended starting from NDMF 1.6.0. You only need to define this class once.
    internal class NDMFContainerProvider : IAacAssetContainerProvider
    {
        private readonly BuildContext _ctx;
        public NDMFContainerProvider(BuildContext ctx) => _ctx = ctx;
        public void SaveAsPersistenceRequired(Object objectToAdd) => _ctx.AssetSaver.SaveAsset(objectToAdd);
        public void SaveAsRegular(Object objectToAdd) { } // Let NDMF crawl our assets when it finishes
        public void ClearPreviousAssets() { } // ClearPreviousAssets is never used in non-destructive contexts
    }
}
