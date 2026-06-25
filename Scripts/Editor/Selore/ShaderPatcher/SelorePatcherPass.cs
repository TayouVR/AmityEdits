// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2026 Tayou <git@tayou.org>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.Linq;
using UnityEngine;
using nadena.dev.ndmf;
using org.Tayou.AmityEdits.ShaderPatcher;

namespace org.Tayou.AmityEdits {

    public class SelorePatcherPass : Pass<SelorePatcherPass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.SeloreShaderPatcher";
        public override string DisplayName => "Selore Shader Patcher";

        protected override void Execute(BuildContext ctx) {
            var avatarDescriptor = ctx.AvatarDescriptor;
            var components = avatarDescriptor.GetComponentsInChildren<SeloreShaderPatcher>(true);
            if (components.Length == 0) return;

            foreach (var plug in components) {
                var renderer = ResolveRenderer(plug);
                if (renderer == null) {
                    Debug.LogWarning(
                        $"[Selore] No Renderer found for {plug.transform.GetHierarchyPath()} - skipping.");
                    continue;
                }

                var shared = renderer.sharedMaterials;
                Debug.Log($"[Selore] Processing {plug.transform.GetHierarchyPath()} -> renderer={renderer.gameObject.name}, material count={shared.Length}");

                var autoParams = ComputeAutoParams(plug, renderer);

                var patched = new Material[shared.Length];
                var anyPatched = false;
                for (var i = 0; i < shared.Length; i++) {
                    var mat = shared[i];
                    if (mat == null || mat.shader == null) {
                        patched[i] = mat;
                        continue;
                    }
                    try {
                        Debug.Log($"[Selore] Patching material '{mat.name}' on " +
                                  $"{plug.transform.GetHierarchyPath()}. autoParams: {JsonUtility.ToJson(autoParams)}");
                        patched[i] = SeloreConfigurer.ConfigureSeloreMaterial(
                            ctx, renderer, mat, plug, autoParams);
                        anyPatched = true;
                    } catch (System.Exception e) {
                        Debug.LogError(
                            $"[Selore] Failed to patch material '{mat.name}' on " +
                            $"{plug.transform.GetHierarchyPath()}: {e.Message}");
                        patched[i] = mat;
                    }
                }

                if (anyPatched) {
                    renderer.sharedMaterials = patched;
                    var patchedCount = patched.Count(m => m != null && m.shader != null && m.shader.name.Contains("Selore"));
                    Debug.Log($"[Selore] Assigned {patched.Length} materials to {renderer.gameObject.name} ({patchedCount} patched with Selore shader)");
                }

                if (plug.autoConfigureBounds) {
                    ConfigureBounds(renderer, autoParams);
                }
            }
        }

        // Resolve the renderer to patch: explicit reference, or nearest renderer
        // when findRenderer is enabled. Prefer a renderer on the same GameObject,
        // then walk up to the nearest parent renderer, then fall back to the
        // first child renderer.
        private static Renderer ResolveRenderer(SeloreShaderPatcher plug) {
            if (!plug.findRenderer && plug.renderer != null) {
                return plug.renderer;
            }
            if (plug.renderer != null) {
                return plug.renderer;
            }

            var local = plug.GetComponent<Renderer>();
            if (local != null) return local;

            var t = plug.transform.parent;
            while (t != null) {
                var r = t.GetComponent<Renderer>();
                if (r != null) return r;
                t = t.parent;
            }

            return plug.GetComponentInChildren<Renderer>(true);
        }

        // Compute the default Selore_StartPosition / Selore_StartRotation /
        // Selore_PenetratorLength from the renderer + plug transforms. These are
        // used as material defaults unless the component overrides them.
        private static SeloreAutoParams ComputeAutoParams(SeloreShaderPatcher plug, Renderer renderer) {
            var result = new SeloreAutoParams {
                startPositionOS = Vector3.zero,
                startRotationEuler = Vector3.zero,
                length = 0.2f,
            };

            var rendererTransform = renderer.transform;
            var plugTransform = plug.transform;

            // If the patcher component lives on a different GameObject than the
            // renderer, treat its transform as the penetrator origin in the
            // renderer's local space.
            if (plugTransform != rendererTransform) {
                result.startPositionOS = rendererTransform.InverseTransformPoint(plugTransform.position);
                var localRotation = Quaternion.Inverse(rendererTransform.rotation) * plugTransform.rotation;
                result.startRotationEuler = localRotation.eulerAngles;
            }

            // Length heuristic: extent of the mesh along the local Y axis (the
            // axis core.cginc treats as "up" / forward of the penetrator).
            var mesh = renderer.GetMesh();
            if (mesh != null) {
                var size = mesh.bounds.size;
                var lengthFromMesh = Mathf.Max(size.x, size.y, size.z);
                if (lengthFromMesh > 0.0001f) {
                    result.length = lengthFromMesh;
                }
            }

            return result;
        }

        // Expand the renderer's local bounds so the deformed mesh isn't frustum
        // culled when the penetrator bends well outside the original bounds.
        private static void ConfigureBounds(Renderer renderer, SeloreAutoParams auto) {
            var radius = Mathf.Max(auto.length, 0.1f) * 2f;
            var center = auto.startPositionOS;
            var expand = new Vector3(radius, radius, radius);
            var newBounds = new Bounds(center, expand * 2f);

            if (renderer is SkinnedMeshRenderer skin) {
                var existing = skin.localBounds;
                existing.Encapsulate(newBounds);
                skin.localBounds = existing;
            } else {
                // For non-skinned renderers Unity recomputes bounds from the
                // mesh automatically, so there is nothing useful to do here.
            }
        }
    }
}
