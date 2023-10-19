using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEngine.Animations;

namespace org.Tayou.AmityEdits {
    
    public class ItemSetupPass {
        
        private readonly BuildContext _buildContext;

        public ItemSetupPass(BuildContext context) {
            _buildContext = context;
        }
        
        public void Process() {
            var avatarDescriptor = _buildContext.AvatarDescriptor;
            var itemSetupComponents =
                avatarDescriptor.GetComponentsInChildren<ItemSetup>(true);

            if (itemSetupComponents.Length == 0) return;

            foreach (var itemSetup in itemSetupComponents) {
                // Create Dummy Objects at targets in Hierarchy and Position using saved Positions
                List<GameObject> targetObjects = new List<GameObject>();
                foreach (var target in itemSetup.targets) {
                    GameObject dummyObject = new GameObject();
                    dummyObject.transform.position = target.position;
                    dummyObject.transform.rotation = target.rotation;
                    targetObjects.Add(dummyObject);
                }
                
                // Create ParentConstraint and assign targets.
                ParentConstraint parentConstraint = itemSetup.gameObject.AddComponent<ParentConstraint>();
                parentConstraint.SetSources(targetObjects.Select(target => new ConstraintSource {
                    sourceTransform = target.transform,
                    weight = 0
                }).ToList());
                // Set Offsets all to 0, not strictly needed as they will be 0 by default
                parentConstraint.rotationOffsets = new Vector3[targetObjects.Count];
                parentConstraint.translationOffsets = new Vector3[targetObjects.Count];
                // lock and activate
                parentConstraint.locked = true;
                parentConstraint.constraintActive = true;

            }
        }


    }
}