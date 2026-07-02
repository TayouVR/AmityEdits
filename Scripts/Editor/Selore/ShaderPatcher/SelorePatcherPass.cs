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
using org.Tayou.AmityEdits.EditorSeloreSps;
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

                // Create SPS resolver + DataGrabPass renderer if SPS is enabled
                if (plug.featureSpsEnabled) {
                    CreateSpsPlugRenderer(plug, renderer, ctx);
                }
            }
        }

        private static void CreateSpsPlugRenderer(SeloreShaderPatcher plug, Renderer renderer, BuildContext ctx) {
            // Resolver GameObject with two sub-materials: resolver + DataGrabPass
            var resolverObj = new GameObject("SPS Plug Resolver", typeof(MeshFilter), typeof(MeshRenderer));
            resolverObj.transform.SetParent(plug.transform, false);
            resolverObj.transform.localPosition = Vector3.zero;
            resolverObj.transform.localRotation = Quaternion.identity;
            resolverObj.transform.localScale = Vector3.one * 0.001f;

            // Trigger mesh (single triangle)
            var mesh = new Mesh { name = "SpsPlugResolverMesh" };
            mesh.vertices = new Vector3[] {
                new Vector3(-5, -5, 0),
                new Vector3(5, -5, 0),
                new Vector3(-5, 5, 0)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1)
            };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 5);
            var mf = resolverObj.GetComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // Compute unique ID
            uint id = plug.spsId != 0f
                ? (uint)Mathf.RoundToInt(plug.spsId)
                : SpsCellPreview.ComputeIdFromWorld(plug.transform.position);
            uint playerId = (uint)Mathf.RoundToInt(plug.spsPlayerId);

            // Build tag arrays from SpsTagRule lists
            var tagInclude = new uint[4];
            var tagExclude = new uint[4];
            for (int i = 0; i < 4; i++) {
                if (i < plug.spsIncludeTags.Count && !string.IsNullOrEmpty(plug.spsIncludeTags[i].tag)) {
                    tagInclude[i] = SpsCellPreview.HashTag(plug.spsIncludeTags[i].tag);
                }
                if (i < plug.spsExcludeTags.Count && !string.IsNullOrEmpty(plug.spsExcludeTags[i].tag)) {
                    tagExclude[i] = SpsCellPreview.HashTag(plug.spsExcludeTags[i].tag);
                }
            }

            // Resolver shader
            var resolverShader = Shader.Find("Hidden/Amity/SpsResolver");
            if (resolverShader == null) {
                resolverShader = Shader.Find("Hidden/VRCFury/SpsResolver");
            }

            // DataGrabPass shader
            var grabShader = Shader.Find("Hidden/Amity/SpsDataGrabPass");
            if (grabShader == null) {
                grabShader = Shader.Find("Hidden/VRCFury/SpsDataGrabPass");
            }

            if (resolverShader == null) {
                Debug.LogWarning("[Selore] SPS resolver shader not found. Install Amity SPS or VRCFury.");
                return;
            }

            var resolverMat = new Material(resolverShader) {
                name = "SpsPlugResolver_Generated",
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = true
            };

            resolverMat.SetFloat("_SPS_Configured", 1f);
            resolverMat.SetFloat("_SPS_Id", id);
            resolverMat.SetFloat("_SPS_PlayerId", playerId);
            resolverMat.SetFloat("_SPS_Enabled", 1f);
            resolverMat.SetFloat("_SPS_Legacy", 1f);

            // Write include tag rules
            for (int i = 0; i < 4; i++) {
                resolverMat.SetFloat("_SPS_TagInclude" + (i + 1), tagInclude[i]);
                bool selfOk = i < plug.spsIncludeTags.Count ? plug.spsIncludeTags[i].allowSelf : true;
                bool othersOk = i < plug.spsIncludeTags.Count ? plug.spsIncludeTags[i].allowOthers : true;
                resolverMat.SetFloat("_SPS_TagInclude" + (i + 1) + "Self", selfOk ? 1f : 0f);
                resolverMat.SetFloat("_SPS_TagInclude" + (i + 1) + "Others", othersOk ? 1f : 0f);
            }

            // Write exclude tag rules
            for (int i = 0; i < 4; i++) {
                resolverMat.SetFloat("_SPS_TagExclude" + (i + 1), tagExclude[i]);
                bool selfOk = i < plug.spsExcludeTags.Count ? plug.spsExcludeTags[i].allowSelf : false;
                bool othersOk = i < plug.spsExcludeTags.Count ? plug.spsExcludeTags[i].allowOthers : false;
                resolverMat.SetFloat("_SPS_TagExclude" + (i + 1) + "Self", selfOk ? 1f : 0f);
                resolverMat.SetFloat("_SPS_TagExclude" + (i + 1) + "Others", othersOk ? 1f : 0f);
            }

            // Compute radius samples from the plug mesh
            ComputeRadiusSamples(renderer, resolverMat);

            var mr = resolverObj.GetComponent<MeshRenderer>();
            if (grabShader != null) {
                var grabMatLocal = new Material(grabShader) {
                    name = "SpsPlugDataGrabPass_Generated",
                    hideFlags = HideFlags.HideAndDontSave,
                    enableInstancing = true
                };
                grabMatLocal.SetFloat("_SPS_Configured", 1f);
                grabMatLocal.SetFloat("_SPS_Id", id);
                grabMatLocal.SetFloat("_SPS_PlayerId", playerId);
                mr.sharedMaterials = new[] { resolverMat, grabMatLocal };
            } else {
                mr.sharedMaterials = new[] { resolverMat };
            }

            // Mark all _SPS_ properties as animated
            foreach (var propName in new[] {
                "_SPS_Configured", "_SPS_Id", "_SPS_PlayerId",
                "_SPS_Enabled", "_SPS_Legacy"
            }) {
                resolverMat.SetOverrideTag(propName + "Animated", "1");
            }
            for (int i = 1; i <= 4; i++) {
                resolverMat.SetOverrideTag($"_SPS_TagInclude{i}Animated", "1");
                resolverMat.SetOverrideTag($"_SPS_TagInclude{i}SelfAnimated", "1");
                resolverMat.SetOverrideTag($"_SPS_TagInclude{i}OthersAnimated", "1");
                resolverMat.SetOverrideTag($"_SPS_TagExclude{i}Animated", "1");
                resolverMat.SetOverrideTag($"_SPS_TagExclude{i}SelfAnimated", "1");
                resolverMat.SetOverrideTag($"_SPS_TagExclude{i}OthersAnimated", "1");
            }
        }

        // Compute radius samples from the plug mesh (75th percentile XY distance per Z bucket)
        private static void ComputeRadiusSamples(Renderer renderer, Material resolverMat) {
            const int bucketCount = 32;

            var mesh = renderer.GetMesh();
            if (mesh == null) return;

            var vertices = mesh.vertices;
            if (vertices.Length == 0) return;

            // Find bounding box along local Z
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var v in vertices) {
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }

            float zRange = maxZ - minZ;
            if (zRange < 0.0001f) return;

            // Allocate per-bucket lists
            var bucketDists = new System.Collections.Generic.List<float>[bucketCount];
            for (int i = 0; i < bucketCount; i++) {
                bucketDists[i] = new System.Collections.Generic.List<float>();
            }

            foreach (var v in vertices) {
                int bucket = Mathf.FloorToInt((v.z - minZ) / zRange * (bucketCount - 1));
                bucket = Mathf.Clamp(bucket, 0, bucketCount - 1);
                float dist = Mathf.Sqrt(v.x * v.x + v.y * v.y);
                bucketDists[bucket].Add(dist);
            }

            // Compute 75th percentile per bucket and pack into float4
            // 8 float4s = 32 floats (one per bucket)
            for (int bucket = 0; bucket < bucketCount; bucket++) {
                var dists = bucketDists[bucket];
                if (dists.Count == 0) continue;
                dists.Sort();
                int p75Idx = Mathf.FloorToInt(dists.Count * 0.75f);
                p75Idx = Mathf.Clamp(p75Idx, 0, dists.Count - 1);
                float radius = dists[p75Idx];

                int vecIdx = bucket / 4;
                int compIdx = bucket % 4;
                string propName = $"_SPS_BakedRadiusSamples{vecIdx}";
                var current = resolverMat.HasProperty(propName)
                    ? resolverMat.GetVector(propName)
                    : Vector4.zero;
                switch (compIdx) {
                    case 0: current.x = radius; break;
                    case 1: current.y = radius; break;
                    case 2: current.z = radius; break;
                    case 3: current.w = radius; break;
                }
                resolverMat.SetVector(propName, current);
            }

            // Baked length and radius
            resolverMat.SetFloat("_SPS_BakedLength", zRange);
            resolverMat.SetFloat("_SPS_BakedRadius", zRange * 0.5f);
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
