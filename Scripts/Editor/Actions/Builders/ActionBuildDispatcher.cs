// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    public static class ActionBuildDispatcher {
        public static void BuildFor(this BaseAmityAction action, BuildContext ctx, VRCExpressionsMenu.Control menuControl) {
            switch (action) {
                case GameObjectToggleAction a:
                    GameObjectToggleActionBuilder.Build(a, ctx, menuControl);
                    break;
                case ComponentToggleAction a:
                    ComponentToggleActionBuilder.Build(a, ctx, menuControl);
                    break;
                case MaterialPropertyAction a:
                    MaterialPropertyActionBuilder.Build(a, ctx, menuControl);
                    break;
                case MaterialSwapAction a:
                    MaterialSwapActionBuilder.Build(a, ctx, menuControl);
                    break;
                case AnimationSlotAction a:
                    AnimationSlotActionBuilder.Build(a, ctx, menuControl);
                    break;
                default:
                    Debug.LogWarning($"[Amity] No builder for action type {action?.GetType().Name}");
                    // no-op
                    break;
            }
        }
    }
}
