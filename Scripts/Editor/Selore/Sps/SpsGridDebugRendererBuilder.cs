// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;
using UnityEngine.Rendering;

namespace org.Tayou.AmityEdits {
    internal static class SpsGridDebugRendererBuilder {
        private const int MaxSlotCount = 4096;

        internal static Mesh CreateDispatchMesh(string name, HideFlags hideFlags = HideFlags.None) {
            var vertices = new Vector3[MaxSlotCount];
            var slotIndices = new Vector2[MaxSlotCount];
            var indices = new int[MaxSlotCount];

            for (var slotIndex = 0; slotIndex < MaxSlotCount; slotIndex++) {
                slotIndices[slotIndex] = new Vector2(slotIndex, 0);
                indices[slotIndex] = slotIndex;
            }

            var mesh = new Mesh {
                name = name,
                hideFlags = hideFlags,
                vertices = vertices,
                uv = slotIndices,
                bounds = new Bounds(Vector3.zero, Vector3.one * 1000000f)
            };
            mesh.SetIndices(indices, MeshTopology.Points, 0, false);
            return mesh;
        }

        internal static void ApplyMaterialProperties(Material material, SpsGridDebug debug) {
            material.SetFloat("_ShowSockets", debug.showSockets ? 1f : 0f);
            material.SetFloat("_ShowPlugs", debug.showPlugs ? 1f : 0f);
            material.SetFloat("_ShowRing", debug.showRing ? 1f : 0f);
            material.SetFloat("_ShowArrow", debug.showArrow ? 1f : 0f);
            material.SetFloat("_ShowTags", debug.showTags ? 1f : 0f);
            material.SetFloat("_ShowChain", debug.showChainLinks ? 1f : 0f);
            material.SetFloat("_GizmoScale", debug.gizmoScale);
            material.SetFloat("_LineWidthPx", debug.lineWidthPixels);
            material.SetFloat(
                "_ZTest",
                (float)(debug.depthTested ? CompareFunction.LessEqual : CompareFunction.Always)
            );
            material.SetColor("_HoleColor", debug.holeColor);
            material.SetColor("_RingColor", debug.ringColor);
            material.SetColor("_ReversibleColor", debug.reversibleColor);
            material.SetColor("_PlugColor", debug.plugColor);
            material.SetColor("_ChainColor", debug.chainColor);
        }
    }
}
