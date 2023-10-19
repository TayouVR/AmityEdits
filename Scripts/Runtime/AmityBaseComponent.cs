using UnityEngine;
using VRC.SDKBase;

namespace org.Tayou.AmityEdits {
    public abstract class AmityBaseComponent : MonoBehaviour, IEditorOnly {

        private void OnEnable() {
            // Do nothing, this is just for unity to give me the toggle
        }
        
    }
}