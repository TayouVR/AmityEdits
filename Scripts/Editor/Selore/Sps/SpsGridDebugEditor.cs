// SPDX-License-Identifier: GPL-3.0-only
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(SpsGridDebug))]
    public class SpsGridDebugEditor : Editor {
        private GameObject _previewObject;

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            root.Bind(serializedObject);

            // Show overlay toggle
            root.Add(new PropertyField(
                serializedObject.FindProperty("showOverlay"), "Show Overlay"));

            // --- Visual Elements foldout ---
            var elementsFoldout = new Foldout { text = "Visual Elements" };
            elementsFoldout.Add(new PropertyField(serializedObject.FindProperty("showRing"), "Ring"));
            elementsFoldout.Add(new PropertyField(serializedObject.FindProperty("showArrow"), "Direction Arrow"));
            elementsFoldout.Add(new PropertyField(serializedObject.FindProperty("showTags"), "Tag Dots"));
            elementsFoldout.Add(new PropertyField(serializedObject.FindProperty("showChainLinks"), "Chain Links"));
            root.Add(elementsFoldout);

            // --- Colors foldout ---
            var colorsFoldout = new Foldout { text = "Colors" };
            colorsFoldout.Add(new PropertyField(serializedObject.FindProperty("holeColor"), "Hole"));
            colorsFoldout.Add(new PropertyField(serializedObject.FindProperty("ringColor"), "Ring"));
            colorsFoldout.Add(new PropertyField(serializedObject.FindProperty("reversibleColor"), "Reversible"));
            colorsFoldout.Add(new PropertyField(serializedObject.FindProperty("plugColor"), "Plug"));
            colorsFoldout.Add(new PropertyField(serializedObject.FindProperty("chainColor"), "Chain"));
            root.Add(colorsFoldout);

            // --- Editor Preview ---
            var previewContainer = new VisualElement();
            previewContainer.Add(new Label("Editor Preview") {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 }
            });

            var previewToggle = new Toggle("Show Preview") { value = false };
            previewToggle.RegisterValueChangedCallback(evt => {
                if (evt.newValue) {
                    CreatePreview();
                } else {
                    DestroyPreview();
                }
            });
            previewContainer.Add(previewToggle);

            var refreshBtn = new Button(RefreshPreview) { text = "Refresh Preview" };
            previewContainer.Add(refreshBtn);

            root.Add(previewContainer);

            return root;
        }

        private void CreatePreview() {
            DestroyPreview();
            var debug = (SpsGridDebug)target;
            if (debug == null) return;

            var shader = Shader.Find("Hidden/Amity/SpsDebugOverlay");
            if (shader == null) {
                Debug.LogWarning("[SpsGridDebug] Preview shader not found.");
                return;
            }

            _previewObject = new GameObject("SPS Debug Overlay (Preview)");
            _previewObject.transform.SetParent(debug.transform, false);
            _previewObject.transform.localPosition = Vector3.zero;
            _previewObject.transform.localRotation = Quaternion.identity;
            _previewObject.transform.localScale = Vector3.one;

            var mesh = new Mesh { name = "SpsDebugOverlayQuad_Preview" };
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
            mesh.hideFlags = HideFlags.HideAndDontSave;

            _previewObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mat = new Material(shader) {
                name = "SpsDebugOverlay_EditorPreview",
                hideFlags = HideFlags.HideAndDontSave
            };
            ApplyProperties(mat);
            _previewObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void DestroyPreview() {
            if (_previewObject != null) {
                Object.DestroyImmediate(_previewObject);
                _previewObject = null;
            }
        }

        private void RefreshPreview() {
            if (_previewObject == null) return;
            var mr = _previewObject.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null) {
                ApplyProperties(mr.sharedMaterial);
            }
        }

        private void ApplyProperties(Material mat) {
            var debug = (SpsGridDebug)target;
            if (debug == null) return;
            mat.SetFloat("_ShowRing", debug.showRing ? 1f : 0f);
            mat.SetFloat("_ShowArrow", debug.showArrow ? 1f : 0f);
            mat.SetFloat("_ShowTags", debug.showTags ? 1f : 0f);
            mat.SetFloat("_ShowChain", debug.showChainLinks ? 1f : 0f);
            mat.SetColor("_HoleColor", debug.holeColor);
            mat.SetColor("_RingColor", debug.ringColor);
            mat.SetColor("_ReversibleColor", debug.reversibleColor);
            mat.SetColor("_PlugColor", debug.plugColor);
            mat.SetColor("_ChainColor", debug.chainColor);
        }

        void OnDisable() {
            DestroyPreview();
        }
    }
}
