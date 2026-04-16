// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2023 Tayou <git@tayou.org>
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
                CreateOrificeInPrefab(orifice);
            }
        }

        // follow spec as defined here: https://gist.github.com/TayouVR/aad7f8b6d83264b379d90e5100653a76
        private void CreateOrificeInPrefab(SeloreHole seloreHole) {
            var rootObject = (object)seloreHole.targetObject != null ? seloreHole.targetObject : seloreHole.gameObject.transform;

            Debug.Log(rootObject);
            
            // TODO: feature gate lights, contact senders and toy contact receivers
            
            // Lights
            var lightParent = new GameObject("Lights");
            lightParent.transform.SetParent(rootObject, false);
            CreateLight(seloreHole.role == SeloreRole.Hole ? SeloreLightRole.HoleBase : SeloreLightRole.RingBase, seloreHole.channel, lightParent.transform);
            CreateLight(SeloreLightRole.Normal, seloreHole.channel, lightParent.transform);
            
            // contact senders
            var sendersParent = new GameObject("Senders");
            sendersParent.transform.SetParent(rootObject, false);
            CreateContactSender(seloreHole.role == SeloreRole.Hole ? SeloreLightRole.HoleBase : SeloreLightRole.RingBase, seloreHole.role, sendersParent.transform);
            CreateContactSender(SeloreLightRole.Normal, seloreHole.role, sendersParent.transform);
            
            // toy contact receivers
            var receiversParent = new GameObject("Receivers");
            receiversParent.transform.SetParent(rootObject, false);
            CreateToyContactReceivers(seloreHole, receiversParent.transform);
            
            // TODO: repath animations for animatable component properties to lights and contacts
        }

        // not part of DPS spec; check VRCFury, or OSCGoesBrr for spec or infer spec from build output/VRCF code
        private void CreateToyContactReceivers(SeloreHole seloreHole, Transform receiversParentTransform) {
            // TODO: implement toy contact receivers
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