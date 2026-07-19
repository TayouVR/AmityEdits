// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;
using UnityEngine.Rendering;

namespace org.Tayou.AmityEdits {
    internal static class SpsGridDebugRendererBuilder {
        private const int MaxSlotCount = 4096;
        // Must match SPS_DEBUG_PART_COUNT in SpsDebugOverlay.shader. Each slot
        // gizmo is split across this many geometry shader invocations so each
        // one stays within the D3D11 1024-scalar output limit.
        private const int PartCount = 4;

        internal static Mesh CreateDispatchMesh(string name, HideFlags hideFlags = HideFlags.None) {
            const int pointCount = MaxSlotCount * PartCount;
            var vertices = new Vector3[pointCount];
            var slotIndices = new Vector2[pointCount];
            var indices = new int[pointCount];

            for (var pointIndex = 0; pointIndex < pointCount; pointIndex++) {
                slotIndices[pointIndex] = new Vector2(pointIndex / PartCount, pointIndex % PartCount);
                indices[pointIndex] = pointIndex;
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
