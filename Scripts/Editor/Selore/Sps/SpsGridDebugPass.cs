// SPDX-License-Identifier: GPL-3.0-only
using System.Linq;
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

            // Full-screen quad mesh
            var mesh = new Mesh { name = "SpsDebugOverlayQuad" };
            mesh.vertices = new Vector3[] {
                new Vector3(-1, -1, 0),
                new Vector3( 1, -1, 0),
                new Vector3(-1,  1, 0),
                new Vector3( 1,  1, 0)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
            AssetDatabase.AddObjectToAsset(mesh, ctx.AssetContainer);

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // Material
            var mat = new Material(shader) {
                name = "SpsDebugOverlay_Generated"
            };
            AssetDatabase.AddObjectToAsset(mat, ctx.AssetContainer);

            // Copy component properties to material
            mat.SetFloat("_ShowRing", debug.showRing ? 1f : 0f);
            mat.SetFloat("_ShowArrow", debug.showArrow ? 1f : 0f);
            mat.SetFloat("_ShowTags", debug.showTags ? 1f : 0f);
            mat.SetFloat("_ShowChain", debug.showChainLinks ? 1f : 0f);
            mat.SetColor("_HoleColor", debug.holeColor);
            mat.SetColor("_RingColor", debug.ringColor);
            mat.SetColor("_ReversibleColor", debug.reversibleColor);
            mat.SetColor("_PlugColor", debug.plugColor);
            mat.SetColor("_ChainColor", debug.chainColor);

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
