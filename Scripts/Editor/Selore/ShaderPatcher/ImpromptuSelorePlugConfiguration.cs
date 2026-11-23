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
using System.Collections.Generic;
using org.Tayou.AmityEdits.EditorSeloreSps;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {
    /// <summary>
    /// Edit-mode helper for testing Selore with a hand-written / reference
    /// shader. It does NOT run the shader patcher. It only:
    ///   1. adds (or updates) an SPS plug resolver + DataGrabPass renderer under
    ///      the selected object, and
    ///   2. switches the Selore/SPS properties + keyword on the materials of the
    ///      renderer so the shader actually reads the resolved plug data.
    /// Everything is registered with Undo, and re-running it updates the existing
    /// resolver instead of adding another one.
    /// </summary>
    public static class ImpromptuSelorePlugConfiguration {
        private const string MenuPath = "Tools/Amity/Impromptu Selore Plug Configuration";
        private const string ResolverName = "SPS Plug Resolver (Impromptu)";

        // Selore material properties (see selore_props.cginc).
        private const string PropEnabled = "Selore_PenetratorEnabled";
        private const string PropStartPosition = "Selore_StartPosition";
        private const string PropStartRotation = "Selore_StartRotation";
        private const string PropLength = "Selore_PenetratorLength";
        private const string PropUseSps = "Selore_UseSps";
        private const string PropSpsId = "Selore_SpsId";
        private const string PropSpsPlayerId = "Selore_SpsPlayerId";
        private const string SpsKeyword = "SELORE_SPS";

        [UnityEditor.MenuItem(MenuPath, false, 49)]
        private static void Run(MenuCommand command) {
            // Hierarchy context menus call the method once per selected object.
            // Only the call for the active object does the work (for the whole
            // selection), the rest return early.
            if (command.context != null && command.context != Selection.activeGameObject) return;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Impromptu Selore Plug Configuration");
            var group = Undo.GetCurrentGroup();

            var done = 0;
            foreach (var go in Selection.gameObjects) {
                if (EditorUtility.IsPersistent(go)) continue; // prefab assets, not scene objects
                try {
                    if (Configure(go)) done++;
                } catch (System.Exception e) {
                    Debug.LogError($"[Selore] Impromptu configuration failed on '{go.name}': {e}", go);
                }
            }

            Undo.CollapseUndoOperations(group);
            if (done == 0) {
                Debug.LogWarning("[Selore] Impromptu configuration: nothing configured. Select a GameObject that has " +
                                 "(or whose parent/child has) a Mesh/SkinnedMesh renderer using a Selore shader.");
            }
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        private static bool Validate() {
            return Selection.gameObjects.Length > 0;
        }

        private static bool Configure(GameObject selected) {
            var renderer = FindRenderer(selected);
            if (renderer == null) {
                Debug.LogWarning($"[Selore] No Renderer found for '{selected.name}'.", selected);
                return false;
            }

            var seloreMaterials = new List<Material>();
            foreach (var m in renderer.sharedMaterials) {
                if (m != null && m.HasProperty(PropUseSps)) seloreMaterials.Add(m);
            }
            if (seloreMaterials.Count == 0) {
                Debug.LogWarning($"[Selore] '{renderer.name}' has no material with a '{PropUseSps}' property. " +
                                 "Is it using the Selore reference shader (or a Selore-patched shader)?", renderer);
                return false;
            }

            // The material id and the resolver id MUST match. Use an explicit id
            // (the shader's world-position fallback would otherwise hash the
            // *renderer* origin on the material side but the *resolver* origin on
            // the resolver side, and those differ whenever you select a bone/child).
            // Reuse the id of an existing resolver so re-running keeps working.
            var resolverTransform = selected.transform.Find(ResolverName);
            uint id = SpsCellPreview.ComputeIdFromWorld(selected.transform.position);
            uint playerId = 0;
            if (resolverTransform != null) {
                var existingMr = resolverTransform.GetComponent<MeshRenderer>();
                var existingMat = existingMr != null && existingMr.sharedMaterials.Length > 0
                    ? existingMr.sharedMaterials[0]
                    : null;
                if (existingMat != null && existingMat.HasProperty("_SPS_Id")) {
                    id = (uint)Mathf.RoundToInt(existingMat.GetFloat("_SPS_Id"));
                    playerId = (uint)Mathf.RoundToInt(existingMat.GetFloat("_SPS_PlayerId"));
                }
            }

            // Plug axis: the dominant mesh axis of the renderer, same as the patcher's auto-detect.
            var axisOS = SelorePatcherPass.ComputePlugLengthAxis(renderer);

            var resolverMat = CreateOrUpdateResolver(selected.transform, renderer, axisOS, id, playerId);
            if (resolverMat == null) return false;

            ConfigureMaterials(selected.transform, renderer, seloreMaterials, resolverMat, id, playerId);

            Debug.Log($"[Selore] Impromptu plug configured on '{selected.name}': renderer='{renderer.name}', " +
                      $"id={id}, axis={SelorePatcherPass.AxisName(axisOS)}, " +
                      $"length={resolverMat.GetFloat("_SPS_BakedLength"):F4}, " +
                      $"radius={resolverMat.GetFloat("_SPS_BakedRadius"):F4}, " +
                      $"materials={seloreMaterials.Count}", selected);
            return true;
        }

        private static Renderer FindRenderer(GameObject go) {
            var r = go.GetComponent<Renderer>();
            if (r != null) return r;
            for (var t = go.transform.parent; t != null; t = t.parent) {
                r = t.GetComponent<Renderer>();
                if (r != null) return r;
            }
            // Skip our own resolver renderer when looking into children.
            foreach (var child in go.GetComponentsInChildren<Renderer>(true)) {
                if (child.gameObject.name != ResolverName) return child;
            }
            return null;
        }

        private static Material CreateOrUpdateResolver(Transform plug, Renderer renderer, Vector3 axisOS, uint id, uint playerId) {
            var resolverShader = Shader.Find("Hidden/Amity/SpsResolver") ?? Shader.Find("Hidden/VRCFury/SpsResolver");
            var grabShader = Shader.Find("Hidden/Amity/SpsDataGrabPass") ?? Shader.Find("Hidden/VRCFury/SpsDataGrabPass");
            if (resolverShader == null) {
                Debug.LogError("[Selore] SPS resolver shader (Hidden/Amity/SpsResolver) not found.");
                return null;
            }
            if (resolverShader.name.StartsWith("Hidden/Amity") && ShaderUtil.ShaderHasError(resolverShader)) {
                Debug.LogError("[Selore] Hidden/Amity/SpsResolver has compile errors; check the Console.");
                return null;
            }

            // --- GameObject ---
            var existing = plug.Find(ResolverName);
            GameObject resolverObj;
            if (existing != null) {
                resolverObj = existing.gameObject;
                Undo.RecordObject(resolverObj.transform, "Update SPS Plug Resolver");
            } else {
                resolverObj = new GameObject(ResolverName, typeof(MeshFilter), typeof(MeshRenderer));
                Undo.RegisterCreatedObjectUndo(resolverObj, "Add SPS Plug Resolver");
                resolverObj.transform.SetParent(plug, false);
            }
            resolverObj.transform.localPosition = Vector3.zero;
            resolverObj.transform.localScale = Vector3.one;

            // Resolver local +Z must point along the plug length axis.
            var worldDir = renderer.transform.TransformDirection(axisOS);
            var worldUp = Mathf.Abs(Vector3.Dot(worldDir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            var plugRotInv = Quaternion.Inverse(plug.rotation);
            resolverObj.transform.localRotation = Quaternion.LookRotation(plugRotInv * worldDir, plugRotInv * worldUp);

            // --- Trigger mesh (one tiny triangle with generous bounds so it's never culled) ---
            var mf = resolverObj.GetComponent<MeshFilter>();
            if (mf.sharedMesh == null) {
                var mesh = new Mesh { name = "SpsPlugResolverMesh" };
                mesh.vertices = new[] {
                    new Vector3(-0.005f, -0.005f, 0),
                    new Vector3(-0.005f, 0.005f, 0),
                    new Vector3(0.005f, 0.005f, 0)
                };
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) };
                mesh.triangles = new[] { 0, 1, 2 };
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
                mf.sharedMesh = mesh;
                Undo.RegisterCreatedObjectUndo(mesh, "Add SPS Plug Resolver mesh");
            }

            var mr = resolverObj.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // --- Materials ---
            var mats = mr.sharedMaterials;
            Material resolverMat = mats.Length > 0 ? mats[0] : null;
            Material grabMat = mats.Length > 1 ? mats[1] : null;
            if (resolverMat == null || resolverMat.shader != resolverShader) {
                resolverMat = new Material(resolverShader) { name = "SpsPlugResolver_Impromptu", enableInstancing = true };
                Undo.RegisterCreatedObjectUndo(resolverMat, "Add SPS resolver material");
            } else {
                Undo.RecordObject(resolverMat, "Update SPS resolver material");
            }
            if (grabShader != null && (grabMat == null || grabMat.shader != grabShader)) {
                grabMat = new Material(grabShader) { name = "SpsPlugDataGrabPass_Impromptu", enableInstancing = true };
                Undo.RegisterCreatedObjectUndo(grabMat, "Add SPS data grab material");
            } else if (grabMat != null) {
                Undo.RecordObject(grabMat, "Update SPS data grab material");
            }

            // The resolver aborts unless _SPS_Configured == 1.
            resolverMat.SetFloat("_SPS_Configured", 1f);
            resolverMat.SetFloat("_SPS_Id", id);
            resolverMat.SetFloat("_SPS_PlayerId", playerId);
            resolverMat.SetFloat("_SPS_Enabled", 1f);
            resolverMat.SetFloat("_SPS_Legacy", 1f);

            // Default tag rules: accept the shared default tag 1337 from anyone.
            for (var i = 1; i <= 4; i++) {
                var isDefaultSlot = i == 4;
                resolverMat.SetFloat("_SPS_TagInclude" + i, isDefaultSlot ? 1337f : 0f);
                resolverMat.SetFloat("_SPS_TagInclude" + i + "Self", 1f);
                resolverMat.SetFloat("_SPS_TagInclude" + i + "Others", 1f);
                resolverMat.SetFloat("_SPS_TagExclude" + i, 0f);
                resolverMat.SetFloat("_SPS_TagExclude" + i + "Self", 0f);
                resolverMat.SetFloat("_SPS_TagExclude" + i + "Others", 0f);
            }

            // Length / radius profile measured from the plug mesh.
            SelorePatcherPass.ComputeRadiusSamples(renderer, resolverMat, axisOS);

            if (grabMat != null) {
                grabMat.SetFloat("_SPS_Configured", 1f);
                grabMat.SetFloat("_SPS_Id", id);
                grabMat.SetFloat("_SPS_PlayerId", playerId);
                mr.sharedMaterials = new[] { resolverMat, grabMat };
            } else {
                Debug.LogWarning("[Selore] SPS DataGrabPass shader not found; Selore reads _VFGridFinal, which will be missing.");
                mr.sharedMaterials = new[] { resolverMat };
            }

            EditorUtility.SetDirty(resolverMat);
            if (grabMat != null) EditorUtility.SetDirty(grabMat);
            EditorUtility.SetDirty(mr);
            return resolverMat;
        }

        private static void ConfigureMaterials(Transform plug, Renderer renderer, List<Material> materials,
            Material resolverMat, uint id, uint playerId) {
            // Start position / rotation of the penetrator in the renderer's object
            // space. Only touched if the selected object is not the renderer itself,
            // in which case the selection is the penetrator origin.
            var setPose = plug != renderer.transform;
            var startPos = renderer.transform.InverseTransformPoint(plug.position);
            var startRot = (Quaternion.Inverse(renderer.transform.rotation) * plug.rotation).eulerAngles;

            foreach (var mat in materials) {
                Undo.RecordObject(mat, "Configure Selore material");

                SetFloat(mat, PropEnabled, 1f);
                SetFloat(mat, PropUseSps, 1f);
                SetFloat(mat, PropSpsId, id);
                SetFloat(mat, PropSpsPlayerId, playerId);

                // [Toggle] (without a keyword name) doesn't switch the keyword,
                // so always enable it directly.
                mat.EnableKeyword(SpsKeyword);

                if (setPose) {
                    if (mat.HasProperty(PropStartPosition)) mat.SetVector(PropStartPosition, new Vector4(startPos.x, startPos.y, startPos.z, 0f));
                    if (mat.HasProperty(PropStartRotation)) mat.SetVector(PropStartRotation, new Vector4(startRot.x, startRot.y, startRot.z, 0f));
                }
                // Penetrator length: use what the resolver measured from the mesh
                // so shader and resolver agree. Only if still at the 0.2 default /
                // unset, so a hand-tuned value is kept.
                if (mat.HasProperty(PropLength)) {
                    var current = mat.GetFloat(PropLength);
                    if (current <= 0f || Mathf.Approximately(current, 0.2f)) {
                        var measured = resolverMat.GetFloat("_SPS_BakedLength");
                        if (measured > 0.0001f) mat.SetFloat(PropLength, measured);
                    }
                }

                EditorUtility.SetDirty(mat);
            }
        }

        private static void SetFloat(Material m, string name, float value) {
            if (m.HasProperty(name)) m.SetFloat(name, value);
        }
    }
}
