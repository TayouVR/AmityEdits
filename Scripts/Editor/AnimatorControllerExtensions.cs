using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace org.Tayou.AmityEdits {
    public static class AnimatorControllerExtensions {
        public static AnimatorControllerLayer NewLayer(this AnimatorController controller, 
            string name = "") {
            AnimatorControllerLayer layer = new AnimatorControllerLayer {
                name = name
            };
            layer.stateMachine = new AnimatorStateMachine { name = name};
            
            controller.AddLayer(layer);
            return layer;
        }
        
        public static AnimatorControllerParameter NewParameter(this AnimatorController controller, 
            string name = "", 
            AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool) {
            AnimatorControllerParameter parameter = new AnimatorControllerParameter {
                name = name,
                type = type
            };
            
            controller.AddParameter(parameter);
            return parameter;
        }
        
        public static AnimatorState NewState(this AnimatorControllerLayer layer, 
            string name = "") {
            AnimatorState state = layer.stateMachine.AddState(name);
            return state;
        }
        
        public static AnimatorState NewDirectTreeState(this AnimatorControllerLayer layer, 
            out BlendTree blendTree, 
            AnimatorController controller,
            string name = "") {
            blendTree = new BlendTree();
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            blendTree.blendType = BlendTreeType.Direct;

            var state = layer.NewState("Clothing Toggles");
            state.motion = blendTree;
            return state;
        }
        
        public static AnimatorState Drives(this AnimatorState state, 
            AnimatorControllerParameter parameter,
            float value) {
            VRCAvatarParameterDriver driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter() {
                name = parameter.name,
                destParam = parameter, 
                value = value,
            });
            return state;
        }
    }
}