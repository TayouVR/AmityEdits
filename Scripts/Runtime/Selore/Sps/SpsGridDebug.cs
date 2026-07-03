// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;

namespace org.Tayou.AmityEdits {
    [AddComponentMenu("Amity Edits/Debug/SPS Grid Debug Overlay")]
    public class SpsGridDebug : AmityBaseComponent {
        [Header("Visibility")]
        public bool showOverlay = true;
        public bool showRing = true;
        public bool showArrow = true;
        public bool showTags = true;
        public bool showChainLinks = true;

        [Header("Colors")]
        public Color holeColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        public Color ringColor = new Color(0.2f, 0.5f, 1f, 0.9f);
        public Color reversibleColor = new Color(0.2f, 1f, 0.3f, 0.9f);
        public Color plugColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        public Color chainColor = new Color(1f, 1f, 1f, 0.6f);

        private GameObject _overlayObject;
        private MeshRenderer _overlayRenderer;
        private Material _overlayMat;

        void OnEnable() {
            CreateOverlay();
        }

        void OnDisable() {
            DestroyOverlay();
        }

        void OnDestroy() {
            DestroyOverlay();
        }

        void Update() {
            if (_overlayMat == null) return;
            _overlayMat.SetFloat("_ShowRing", showRing ? 1f : 0f);
            _overlayMat.SetFloat("_ShowArrow", showArrow ? 1f : 0f);
            _overlayMat.SetFloat("_ShowTags", showTags ? 1f : 0f);
            _overlayMat.SetFloat("_ShowChain", showChainLinks ? 1f : 0f);
            _overlayMat.SetColor("_HoleColor", holeColor);
            _overlayMat.SetColor("_RingColor", ringColor);
            _overlayMat.SetColor("_ReversibleColor", reversibleColor);
            _overlayMat.SetColor("_PlugColor", plugColor);
            _overlayMat.SetColor("_ChainColor", chainColor);

            if (_overlayObject != null) {
                _overlayObject.SetActive(showOverlay);
            }
        }

        private void CreateOverlay() {
            if (_overlayObject != null) return;

            var shader = Shader.Find("Hidden/Amity/SpsDebugOverlay");
            if (shader == null) {
                Debug.LogWarning("[SpsGridDebug] SPS debug overlay shader not found. " +
                                 "Make sure Amity SPS is installed.");
                return;
            }

            _overlayObject = new GameObject("SPS Debug Overlay");
            _overlayObject.transform.SetParent(transform, false);
            _overlayObject.transform.localPosition = Vector3.zero;
            _overlayObject.transform.localRotation = Quaternion.identity;
            _overlayObject.transform.localScale = Vector3.one;

            var mf = _overlayObject.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateFullScreenQuad();

            _overlayMat = new Material(shader) {
                name = "SpsDebugOverlay_Generated",
                hideFlags = HideFlags.DontSave
            };

            _overlayRenderer = _overlayObject.AddComponent<MeshRenderer>();
            _overlayRenderer.sharedMaterial = _overlayMat;
            _overlayRenderer.enabled = true;

            // Apply initial state
            showOverlay = true;
        }

        private void DestroyOverlay() {
            if (_overlayObject != null) {
                if (Application.isPlaying) {
                    Destroy(_overlayObject);
                } else {
                    DestroyImmediate(_overlayObject);
                }
                _overlayObject = null;
                _overlayRenderer = null;
                _overlayMat = null;
            }
        }

        private static Mesh CreateFullScreenQuad() {
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
            mesh.hideFlags = HideFlags.HideAndDontSave;
            return mesh;
        }
    }
}
