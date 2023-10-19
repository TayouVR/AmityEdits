using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEngine.Animations;
using VF.Builder;
using VF.Utils;
using VF.Utils.Controller;

namespace org.Tayou.AmityEdits {
    
    public class ClothingManagerPass {

        private static string MENU_NAME;
        private static string PARAMETER_PREFIX;
        
        private readonly BuildContext _buildContext;

        public ClothingManagerPass(BuildContext context) {
            _buildContext = context;
        }
        
        public void Process() {
            /*var avatarDescriptor = _buildContext.AvatarDescriptor;
            ClothingItem[] clothingItemComponents = avatarDescriptor.GetComponentsInChildren<ClothingItem>(true);
            Outfit[] outfitComponents = avatarDescriptor.GetComponentsInChildren<Outfit>(true);

            if (clothingItemComponents.Length == 0 || outfitComponents.Length == 0) return;

            ControllerManager fx = new AvatarManager().GetFx();
            VFLayer driverLayer = fx.NewLayer("Clothing Driver");
            VFLayer toggleLayer = fx.NewLayer("Clothing Toggles");

            List<VFABool> parameters = new List<VFABool>();
            
            // add parameters for each clothing item
            foreach (var clothingItem in clothingItemComponents) {
                clothingItem.parameter = fx.NewBool($"{PARAMETER_PREFIX}/{clothingItem.name}", true, true, false, true);
            }

            // generate animations for each clothing item
            // this is currently not necessary as I am just slotting them in the component.
            // It would be necessary if I used VRCFurys actions or something alike
            foreach (var clothingItem in clothingItemComponents) {
                continue;
            }
            
            // generate Clothing Item States and Parameter Drivers
            foreach (var clothingItem in clothingItemComponents) {
                var state = driverLayer.NewState(clothingItem.name).Drives(clothingItem.parameter, true);
                state.TransitionsFromAny();

                foreach (var incompatibleItem in clothingItem.incompatibilities) {
                    state.Drives(incompatibleItem.parameter, false);
                }
            }
            
            // generate Direct BlendTree
            foreach (var clothingItem in clothingItemComponents) {
                // this all needs to be in a big Direct BlendTree, not sure how to do that with VF logic
                toggleLayer.NewState(clothingItem.name).WithAnimation(clothingItem.animation).TransitionsFromAny();
            }
            
            // generate Outfit States
            foreach (var outfit in outfitComponents) {
                var state = driverLayer.NewState(outfit.name);

                foreach (var clothingItem in outfit.ClothingItems) {
                    state.Drives(clothingItem.parameter, true);
                }
            }*/
        }
    }
}