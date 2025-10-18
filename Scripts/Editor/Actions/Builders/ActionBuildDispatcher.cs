// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.Actions;

namespace org.Tayou.AmityEdits.Actions.Editor.Builders {
    public static class ActionBuildDispatcher {
        public static void BuildFor(this BaseAmityAction action, BuildContext ctx, string menuParameterName) {
            switch (action) {
                case GameObjectToggleAction a:
                    GameObjectToggleActionBuilder.Build(a, ctx, menuParameterName);
                    break;
                case ComponentToggleAction a:
                    ComponentToggleActionBuilder.Build(a, ctx, menuParameterName);
                    break;
                case MaterialPropertyAction a:
                    MaterialPropertyActionBuilder.Build(a, ctx, menuParameterName);
                    break;
                case MaterialSwapAction a:
                    MaterialSwapActionBuilder.Build(a, ctx, menuParameterName);
                    break;
                case AnimationSlotAction a:
                    AnimationSlotActionBuilder.Build(a, ctx, menuParameterName);
                    break;
                default:
                    // no-op
                    break;
            }
        }
    }
}
