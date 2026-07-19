// SPDX-License-Identifier: GPL-3.0-only
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    public class SpsGridDebugPass : Pass<SpsGridDebugPass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.SpsGridDebug";
        public override string DisplayName => "SPS Grid Debug Overlay";

        protected override void Execute(BuildContext ctx) {
            var components = ctx.AvatarRootObject.GetComponentsInChildren<SpsGridDebug>(true);
            if (components.Length == 0) return;

            foreach (var debug in components) {
                if (!debug.showOverlay) continue;
                CreateOverlay(debug, ctx);
            }
        }

        private static void CreateOverlay(SpsGridDebug debug, BuildContext ctx) {
            var shader = Shader.Find("Hidden/Amity/SpsDebugOverlay");
            if (shader == null) {
                Debug.LogWarning("[SpsGridDebug] SPS debug overlay shader not found. " +
                                 "Make sure Amity SPS is installed.");
                return;
            }

            var obj = new GameObject("SPS Debug Overlay");

            // Attach to the component's transform (under the avatar)
            obj.transform.SetParent(debug.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            // One point per possible SPS slot. The geometry shader decodes the
            // slot and emits a gizmo only when it contains a unique valid cell.
            var mesh = SpsGridDebugRendererBuilder.CreateDispatchMesh("SpsDebugDispatchMesh");
            AssetDatabase.AddObjectToAsset(mesh, ctx.AssetContainer);

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // Material
            var mat = new Material(shader) {
                name = "SpsDebugOverlay_Generated"
            };
            AssetDatabase.AddObjectToAsset(mat, ctx.AssetContainer);

            SpsGridDebugRendererBuilder.ApplyMaterialProperties(mat, debug);

            // Prevent property strippers from removing our properties
            if (mat.shader != null) {
                var count = ShaderUtil.GetPropertyCount(mat.shader);
                for (var i = 0; i < count; i++) {
                    var propName = ShaderUtil.GetPropertyName(mat.shader, i);
                    mat.SetOverrideTag(propName + "Animated", "1");
                }
            }

            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }
    }
}
