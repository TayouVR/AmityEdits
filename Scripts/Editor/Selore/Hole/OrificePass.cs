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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.vrchat;
using UnityEditor.Animations;
using VRC.SDK3.Dynamics.Contact.Components;
using org.Tayou.AmityEdits.EditorSeloreSps;

namespace org.Tayou.AmityEdits {
    
    public class OrificePass : Pass<OrificePass> {
        public override string QualifiedName => "org.Tayou.AmityEdits.SeloreOrificeBuilder";
        public override string DisplayName => "Selore Orifice Builder";
        
        // Channel 0
        const float Ch0Regular = 0.41f;
        const float Ch0Ring = 0.42f;
        const float Ch0Normal = 0.45f;
        const float Ch0Physics = 0.49f;
        
        // Channel 1
        const float Ch1Regular = 0.43f;
        const float Ch1Ring = 0.44f;
        const float Ch1Normal = 0.46f;
        const float Ch1Physics = 0.48f;
        
        static readonly float[,] LightRangeMatrix = {
            { Ch0Regular, Ch0Ring, Ch0Normal, Ch0Physics },
            { Ch1Regular, Ch1Ring, Ch1Normal, Ch1Physics }
        };

        const string ContactSpsSocketFront = "SPSLL_Socket_Front";
        const string ContactSpsSocketRoot = "SPSLL_Socket_Root";
        const string ContactSpsSocketRing = "SPSLL_Socket_Ring";
        const string ContactSpsSocketHole = "SPSLL_Socket_Hole";
        const string ContactTpsOrificeRoot = "TPS_Orf_Root";
        const string ContactTpsOrificeNorm = "TPS_Orf_Norm";

        protected override void Execute(BuildContext ctx) {
            var components = ctx.AvatarRootObject.GetComponentsInChildren<SeloreHole>(true);
            Debug.Log($"orifice count: {components.Length}");

            if (components.Length == 0) return;
            
            foreach (var orifice in components) {
                Debug.Log($"orifice: {orifice.name}, target: {orifice.targetObject}, role: {orifice.role}, channel: {orifice.channel}, path: {orifice.gameObject.transform.GetHierarchyPath(ctx.AvatarRootObject.transform)}");
                CreateOrificeInPrefab(orifice, ctx);
            }
        }

        // follow spec as defined here: https://gist.github.com/TayouVR/aad7f8b6d83264b379d90e5100653a76
        private void CreateOrificeInPrefab(SeloreHole seloreHole, BuildContext ctx) {
            var rootObject = seloreHole.targetObject == null ? seloreHole.gameObject.transform : seloreHole.targetObject;

            Debug.Log($"Feature generation for orifice {seloreHole.name} in:");
            Debug.Log(rootObject);
            
            // Lights
            if (seloreHole.featureLights) {
                var lightParent = new GameObject("Lights");
                lightParent.transform.SetParent(rootObject, false);
                CreateLight(seloreHole.role == SeloreRole.Hole ? SeloreLightRole.HoleBase : SeloreLightRole.RingBase,
                    seloreHole.channel, lightParent.transform);
                CreateLight(SeloreLightRole.Normal, seloreHole.channel, lightParent.transform);
            }

            // contact senders
            if (seloreHole.featureContactSenders) {
                var sendersParent = new GameObject("Senders");
                sendersParent.transform.SetParent(rootObject, false);
                CreateContactSender(seloreHole.role == SeloreRole.Hole ? SeloreLightRole.HoleBase : SeloreLightRole.RingBase, seloreHole.role, sendersParent.transform);
                CreateContactSender(SeloreLightRole.Normal, seloreHole.role, sendersParent.transform);
            }
            
            // SPS cell marker
            if (seloreHole.featureSpsCell) {
                CreateSpsMarker(seloreHole, rootObject, ctx);
            }

            // toy contact receivers
            if (seloreHole.featureToyContactReceivers) {
                var receiversParent = new GameObject("Receivers");
                receiversParent.transform.SetParent(rootObject, false);
                CreateToyContactReceivers(seloreHole, receiversParent.transform);
            }
            
            // TODO: repath animations for animatable component properties to lights and contacts
        }

        private void CreateSpsMarker(SeloreHole seloreHole, Transform root, BuildContext ctx) {
            var spsObj = new GameObject("SPS Cell", typeof(MeshFilter), typeof(MeshRenderer));
            spsObj.transform.SetParent(root, false);
            // Keep identity scale: the geometry shader derives the cell's world
            // position/orientation/scale from the object transform. The cell is
            // written to NDC, so it never appears as visible geometry.
            spsObj.transform.localScale = Vector3.one;

            // Trigger mesh: a single tiny triangle. The cell geometry shader
            // writes straight to NDC and ignores the mesh; the triangle only
            // exists to trigger one geometry shader dispatch. Kept small with
            // modest bounds (matching VRCFury's SpsTriggerMesh): a large source
            // triangle or huge bounds can get the renderer culled / the draw
            // clipped, so no cell pixels are ever rasterized.
            var mesh = new Mesh { name = "SpsTriggerMesh" };
            mesh.vertices = new Vector3[] {
                new Vector3(-0.005f, -0.005f, 0),
                new Vector3(-0.005f, 0.005f, 0),
                new Vector3(0.005f, 0.005f, 0)
            };
            mesh.uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1)
            };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            var mf = spsObj.GetComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            AssetDatabase.AddObjectToAsset(mesh, ctx.AssetContainer);

            // Shader: try Amity first, fall back to VRCFury
            var shader = Shader.Find("Hidden/Amity/SpsSocketMarker");
            if (shader == null) {
                shader = Shader.Find("Hidden/VRCFury/SpsSocketMarker");
            }

            if (shader == null) {
                Debug.LogWarning($"[Selore] SPS socket marker shader not found. Install Amity SPS or VRCFury.");
                return;
            }

            var mat = new Material(shader) {
                name = "SpsSocketMarker_Generated",
                enableInstancing = true
            };
            AssetDatabase.AddObjectToAsset(mat, ctx.AssetContainer);

            // Generate a stable ID from position
            Transform target = seloreHole.targetObject != null ? seloreHole.targetObject : seloreHole.transform;
            uint id = SpsCellPreview.ComputeIdFromWorld(target.position);

            // DataGrabPass material (second sub-material, reads at Background-940)
            var grabShader = Shader.Find("Hidden/Amity/SpsDataGrabPass");
            if (grabShader == null) {
                grabShader = Shader.Find("Hidden/VRCFury/SpsDataGrabPass");
            }
            Material grabMat = null;
            if (grabShader != null) {
                grabMat = new Material(grabShader) {
                    name = "SpsDataGrabPass_Generated",
                    enableInstancing = true
                };
                AssetDatabase.AddObjectToAsset(grabMat, ctx.AssetContainer);
                grabMat.SetFloat("_SPS_Configured", 1f);
                grabMat.SetFloat("_SPS_Id", id);
                grabMat.SetFloat("_SPS_PlayerId", 0f);
            }

            var mr = spsObj.GetComponent<MeshRenderer>();
            mr.sharedMaterials = grabMat != null
                ? new[] { mat, grabMat }
                : new[] { mat };

            // Compute flags from role (mirrors the preview logic)
            uint flags = 0;
            if (seloreHole.role == SeloreRole.Hole) {
                flags |= SpsCellPreview.SOCKET_FLAG_HOLE;
            } else if (seloreHole.role == SeloreRole.Ring) {
                flags |= SpsCellPreview.SOCKET_FLAG_HOLE | SpsCellPreview.SOCKET_FLAG_DOUBLE_SIDED;
            } else if (seloreHole.role == SeloreRole.ReversibleRing) {
                flags |= SpsCellPreview.SOCKET_FLAG_DOUBLE_SIDED;
            }

            // Set material properties (property names must match VRCFury SPS)
            mat.SetFloat("_SPS_Configured", 1f);
            mat.SetFloat("_SPS_Id", id);
            mat.SetFloat("_SPS_PlayerId", 0f);
            mat.SetFloat("_SPS_SocketHole", (flags & SpsCellPreview.SOCKET_FLAG_HOLE) != 0 ? 1f : 0f);
            mat.SetFloat("_SPS_SocketDoubleSided", (flags & SpsCellPreview.SOCKET_FLAG_DOUBLE_SIDED) != 0 ? 1f : 0f);
            mat.SetFloat("_SPS_SocketRadiusOffset", 0f);
            mat.SetFloat("_SPS_SocketNextId", 0f);
            mat.SetFloat("_SPS_SocketUseTangentIn", 0f);
            mat.SetFloat("_SPS_SocketUseTangentOut", 0f);

            // Build tag array: user tags first, then shared tag (matching VRCFury convention)
            var tagValues = new uint[8];
            int tagSlot = 0;

            if (seloreHole.spsTags != null) {
                foreach (var t in seloreHole.spsTags) {
                    if (tagSlot >= 8) break;
                    uint h = SpsCellPreview.HashTag(t);
                    if (h != 0) tagValues[tagSlot++] = h;
                }
            }

            if (seloreHole.spsUseSharedTag && tagSlot < 8) {
                tagValues[tagSlot++] = 1337;
            }

            for (int i = 0; i < 8; i++) {
                mat.SetFloat("_SPS_SocketTag" + (i + 1), tagValues[i]);
            }

            // Mark all _SPS_ properties as animated to prevent shader stripping
            foreach (var propName in new[] {
                "_SPS_Configured", "_SPS_Id", "_SPS_PlayerId",
                "_SPS_SocketHole", "_SPS_SocketDoubleSided", "_SPS_SocketRadiusOffset",
                "_SPS_SocketNextId", "_SPS_SocketUseTangentIn", "_SPS_SocketUseTangentOut",
                "_SPS_SocketTangentIn", "_SPS_SocketTangentOut"
            }) {
                mat.SetOverrideTag(propName + "Animated", "1");
            }
            for (int i = 1; i <= 8; i++) {
                mat.SetOverrideTag($"_SPS_SocketTag{i}Animated", "1");
            }
        }

        // not part of DPS spec; check VRCFury, or OSCGoesBrr for spec or infer spec from build output/VRCF code
        private void CreateToyContactReceivers(SeloreHole seloreHole, Transform parent) {
            const float plugContactRadius = 3f;
            const float touchRadius = 0.05f;
            const float frotRadius = 0.1f;

            // Plug detection: tip, root, width
            if (seloreHole.featurePlugReceivers) {
                CreateReceiver("TipSelf",   plugContactRadius, "TPS_Pen_Penetrating", true,  false, parent);
                CreateReceiver("TipOthers", plugContactRadius, "TPS_Pen_Penetrating", false, true,  parent);
                CreateReceiver("RootSelf",  plugContactRadius, "TPS_Pen_Root",        true,  false, parent);
                CreateReceiver("RootOthers", plugContactRadius, "TPS_Pen_Root",        false, true,  parent);
                CreateReceiver("WidthSelf",  plugContactRadius, "TPS_Pen_Width",       true,  false, parent);
                CreateReceiver("WidthOthers", plugContactRadius, "TPS_Pen_Width",       false, true,  parent);
            }

            // Touch detection: hands, fingers, feet
            if (seloreHole.featureTouchReceivers) {
                CreateReceiver("TouchSelf",
                    touchRadius,
                    new[] { "Hand", "Finger", "Foot" },
                    true, false, parent);
                CreateReceiver("TouchOthers",
                    touchRadius,
                    new[] { "Head", "Hand", "Foot", "Finger" },
                    false, true, parent);
            }

            // Frottage: other orifice root
            if (seloreHole.featureFrotReceiver) {
                CreateReceiver("FrotOthers", frotRadius, "TPS_Orf_Root", false, true, parent);
            }
        }

        private void CreateReceiver(string name, float radius, string tag, bool allowSelf, bool allowOthers, Transform parent) {
            CreateReceiver(name, radius, new[] { tag }, allowSelf, allowOthers, parent);
        }

        private void CreateReceiver(string name, float radius, string[] tags, bool allowSelf, bool allowOthers, Transform parent) {
            var obj = new GameObject(name, typeof(VRCContactReceiver));
            obj.transform.SetParent(parent, false);
            var r = obj.GetComponent<VRCContactReceiver>();
            r.radius = radius;
            r.receiverType = VRCContactReceiver.ReceiverType.Proximity;
            r.collisionTags = new List<string>(tags);
            r.allowSelf = allowSelf;
            r.allowOthers = allowOthers;
            r.localOnly = false;
        }

        private void CreateContactSender(SeloreLightRole lightRole, SeloreRole role, Transform parent) {
            var gameObject = new GameObject(lightRole == SeloreLightRole.Normal ? "Front" :  "Root", typeof(VRCContactSender));
            gameObject.transform.SetParent(parent, false);
            var vrcContactSender = gameObject.GetComponent<VRCContactSender>();
            vrcContactSender.radius = 0.001f;

            if (lightRole == SeloreLightRole.Normal) {
                vrcContactSender.collisionTags.Add(ContactSpsSocketFront);
                vrcContactSender.collisionTags.Add(ContactTpsOrificeNorm);
                gameObject.transform.localPosition = new Vector3(0, 0, 0.01f);
            } else {
                vrcContactSender.collisionTags.Add(ContactSpsSocketRoot);
                vrcContactSender.collisionTags.Add(ContactTpsOrificeRoot);
                
                switch (role) {
                    case SeloreRole.Hole:
                        vrcContactSender.collisionTags.Add(ContactSpsSocketHole);
                        break;
                    case SeloreRole.Ring:
                        vrcContactSender.collisionTags.Add(ContactSpsSocketRing);
                        vrcContactSender.collisionTags.Add(ContactSpsSocketHole);
                        break;
                    case SeloreRole.ReversibleRing:
                        vrcContactSender.collisionTags.Add(ContactSpsSocketRing);
                        break;
                }
            }
        }

        private void CreateLight(SeloreLightRole role, SeloreChannel channel, Transform parent) {
            var gameObject = new GameObject(role == SeloreLightRole.Normal ? "Front" :  "Root", typeof(Light));
            gameObject.transform.SetParent(parent, false);
            var light = gameObject.GetComponent<Light>();
            light.color = Color.black;
            light.range = GetRangeFromRoleAndChannel(role, channel);
            light.renderMode = LightRenderMode.ForceVertex;

            if (role == SeloreLightRole.Normal) {
                gameObject.transform.localPosition = new Vector3(0, 0, 0.01f);
            }
        }
        
        private float GetRangeFromRoleAndChannel(SeloreLightRole role, SeloreChannel channel) {
            return LightRangeMatrix[(int)channel, (int)role];
        }
    }

    internal enum SeloreLightRole {
        HoleBase = 0,
        RingBase = 1,
        Normal = 2,
        Tip = 3, // tip shouldn't ever be needed, but for completeness with DPS spec I'm including it
    }
}