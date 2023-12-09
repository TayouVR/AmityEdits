using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEditor.Animations;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace org.Tayou.AmityEdits {
    
    public class ClothingManagerPass {

        private static string MENU_NAME;
        private static string PARAMETER_PREFIX;
        
        private readonly BuildContext _buildContext;

        public ClothingManagerPass(BuildContext context) {
            _buildContext = context;
        }
        
        public void Process() {
            Debug.Log("The Clothing Manager pass is running");
            var avatarDescriptor = _buildContext.AvatarDescriptor;
            ClothingItem[] clothingItemComponents = avatarDescriptor.GetComponentsInChildren<ClothingItem>(true);
            Outfit[] outfitComponents = avatarDescriptor.GetComponentsInChildren<Outfit>(true);

            if (clothingItemComponents.Length == 0 || outfitComponents.Length == 0) {
                Debug.Log("The Clothing Manager didn't find any components and is returning");
                return;
            }
            
            var fxController = (AnimatorController)avatarDescriptor.baseAnimationLayers.FirstOrDefault(animatorLayer =>
                animatorLayer.type == VRCAvatarDescriptor.AnimLayerType.FX).animatorController;

            AnimatorControllerLayer driverLayer = fxController.NewLayer("Clothing Driver");
            AnimatorControllerLayer toggleLayer = fxController.NewLayer("Clothing Toggles");

            List<AnimatorControllerParameter> parameters = new List<AnimatorControllerParameter>();
            
            // add parameters for each clothing item
            foreach (var clothingItem in clothingItemComponents) {
                clothingItem.ParameterReference = fxController.NewParameter($"{PARAMETER_PREFIX}/{clothingItem.name}", AnimatorControllerParameterType.Float);
                parameters.Add(clothingItem.ParameterReference);
            }

            // generate animations for each clothing item
            // this is currently not necessary as I am just slotting them in the component.
            // It would be necessary if I used VRCFurys actions or something alike
            foreach (var clothingItem in clothingItemComponents) {
                continue;
            }
            
            // generate Clothing Item States and Parameter Drivers
            foreach (var clothingItem in clothingItemComponents) {
                AnimatorState state = driverLayer.NewState(clothingItem.name);
                state.Drives(clothingItem.ParameterReference, 1);
                //state.TransitionsFromAny();

                foreach (var incompatibleItem in clothingItem.incompatibilities) {
                    state.Drives(incompatibleItem.ParameterReference, 0);
                }
            }
            
            // generate Direct BlendTree
            var directTreeState = toggleLayer.NewDirectTreeState(out var directBlendTree, fxController, "Clothing Toggles");

            foreach (var clothingItem in clothingItemComponents) 
            {
                // Add motion based on clothing item's animation
                var motion = new BlendTree {
                    name = clothingItem.name
                };
                motion.AddChild(clothingItem.animation);
                AssetDatabase.AddObjectToAsset(motion, directBlendTree);
                directBlendTree.AddChild(motion, clothingItem.ParameterReference.defaultFloat); 
            }
            AssetDatabase.SaveAssets();
            
            // generate Outfit States
            foreach (var outfit in outfitComponents) {
                var state = driverLayer.NewState(outfit.name);

                foreach (var clothingItem in outfit.ClothingItems) {
                    state.Drives(clothingItem.ParameterReference, 1);
                }
            }
            
            Debug.Log("The Clothing Manager pass has finished. \n" +
                      $"Created {parameters.Count} parameters for clothing items");
        }
    }
}