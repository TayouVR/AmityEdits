// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using org.Tayou.AmityEdits.EditorUtils;
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
}
