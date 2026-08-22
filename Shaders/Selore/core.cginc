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
#ifndef SELORE_CORE
#define SELORE_CORE

#include "globals.cginc"
#include "lights.cginc"
#include "spline.cginc"
#include "utils.cginc"

void SeloreDeform(inout float4 vertexPos, inout float3 vertexNormal, inout float4 color) {
    float3 worldStartPosition = mul(unity_ObjectToWorld, float4(Selore_StartPosition, 1.0)).xyz;
    
    Selore_OrificeData o1;
    Selore_OrificeData o2;

#if defined(SELORE_SPS)
    {
        SpsTexture gridTex = SPS_GET_TEX(_VFGridFinal);
        uint uniqueId = sps_to_uint(Selore_SpsId);
        if (uniqueId == 0u) uniqueId = sps_hash_world(sps_object_origin_world(), 0u);
        uint playerId = sps_to_uint(Selore_SpsPlayerId);

        float3 socketWorld, socketForward, socketUp;
        float socketScale;
        uint socketFlags;
        if (selore_sps_read_plug(Selore_UseSps, gridTex, uniqueId, playerId,
            socketWorld, socketForward, socketUp, socketScale, socketFlags)) {
            // The SPS grid is a world-space protocol (plug and socket can live on
            // different avatars). Everything downstream in this file — the spline
            // construction, spine projection, arc lengths — runs in this renderer's
            // object space, just like the legacy-lights path converts light
            // positions to object space. So localize the socket data here once,
            // rather than mixing coordinate systems through the bezier solve.
            o1.position = sps_toLocal(socketWorld);
            o1.normal = sps_direction_toLocal(socketForward);
            o1.normalLightPosition = sps_toLocal(socketWorld + socketForward);
            // Map the socket's SPS flags to a Selore orifice role, mirroring
            // VRCFury's SPS2 encoding (SpsConfigurer.AddSocket):
            //   HOLE set            -> hole (mesh interior gets hidden/shrunk)
            //   DOUBLE_SIDED set    -> two-way ring
            //   neither             -> one-way ring (VRCFury encodes one-way as
            //                          the absence of both bits; guided-path
            //                          waypoints are authored this way)
            // Only HOLE changes deform behavior, matching VRCFury's deform.
            if (sps_has_flag(socketFlags, SPS_SOCKET_FLAG_HOLE)) {
                o1.type = SELORE_LIGHT_ROLE_HOLE;
            } else if (sps_has_flag(socketFlags, SPS_SOCKET_FLAG_DOUBLE_SIDED)) {
                o1.type = SELORE_LIGHT_ROLE_RING_TWOWAY;
            } else {
                o1.type = SELORE_LIGHT_ROLE_RING_ONEWAY;
            }
            o1.isValid = true;
            o2 = o1;
            o2.isValid = false;
        } else {
            o1.isValid = false;
            o2.isValid = false;
        }
    }
#else
    GetOrifices(Selore_Channel, worldStartPosition, o1, o2);
#endif
    
    if (o1.isValid) {
        float3x3 startMatrix = EulerToRotMatrix(Selore_StartRotation);
        float3 startUp = mul(startMatrix, float3(0,1,0)); 

        float3 p0;
        float3 p1;
        float3 p2;
        float3 p3;
        float3 p4;
        float3 p5;
        float3 p6;
        GetCurvePoints(p0, p1, p2, p3, p4, p5, p6, o1, o2);
        
        float goForTwoOrifices = min(Selore_AllTheWayThrough, o2.isValid);
        
        // Calculate Distance along mesh spine (Meters)
        // Project vector (vertex - start) onto the StartUp vector
        float distanceAlongSpine = dot(vertexPos.xyz - Selore_StartPosition, startUp);
        float currentPosMeters = GetDistanceAlongPath(Selore_StartPosition, Selore_StartRotation, vertexPos);

        // Calculate Curve Lengths for logic switching
        float len1 = CalculateArcLength(p0, p1, p2, p3, 1);
        float len2 = CalculateArcLength(p3, p4, p5, p6, 1);
        float totalLen = len1 + len2;
        float lenToWorkWith = lerp(len1, totalLen, goForTwoOrifices);
        
        // Determine Spline Position and Tangent
        float3 splinePos;
        float3 splineTangent;
        float visualT = 0; // For debugging color


        // CALCULATE SPLINE POSITION (Linear Extension vs Bezier)
        if (currentPosMeters > lenToWorkWith) {
            // --- LINEAR EXTENSION MODE ---
            float3 endPos;
            float3 endTangent;

            if (goForTwoOrifices < 0.5) {
                endPos = p3;
                endTangent = normalize(CubicBezierTangent(p0, p1, p2, p3, 1.0));
            } else {
                endPos = p6;
                endTangent = normalize(CubicBezierTangent(p3, p4, p5, p6, 1.0));
            }
            // TODO: both of these curve calculations can probably be replaced with just a reference to the o1 and o2 positions and normals.
            
            float excessDistance = currentPosMeters - lenToWorkWith;
            
            splinePos = endPos + endTangent * excessDistance;
            splineTangent = endTangent;
            visualT = 2.5; // Debug color
        }
        else {
            // --- BEZIER MODE ---
            // Map distance to t based on segment lengths
            float t;
            if (currentPosMeters < len1) {
                // Segment 1 (0 to 1)
                t = currentPosMeters / max(0.001, len1); // Avoid div/0
            } else {
                // Segment 2 (1 to 2)
                t = 1.0 + ((currentPosMeters - len1) / max(0.001, len2));
            }
            visualT = t;

            splinePos = GetSplinePosition(p0, p1, p2, p3, p4, p5, p6, t);
            splineTangent = normalize(GetSplineTangent(p0, p1, p2, p3, p4, p5, p6, t));
        }

        
        // Basis Transformation (Deform Logic)
        // I'm not great with the math here, so I'm documenting each line so I can still understand what's happening here in the future.
        
        // Find the point on the original straight spine
        float3 pointOnStraightSpine = Selore_StartPosition + (startUp * distanceAlongSpine);
        
        // Find the offset of the vertex from that spine
        float3 offsetFromSpine = vertexPos.xyz - pointOnStraightSpine;

        // Create rotation that aligns Original Up to New Tangent
        float3x3 rotationMatrix = FromToRotation(startUp, splineTangent);

        // Apply rotation to the offset
        float3 rotatedOffset = mul(rotationMatrix, offsetFromSpine);
        
        // Rotate the normal using the same matrix
        float3 deformedNormal = mul(rotationMatrix, vertexNormal);

        // Final Result
        float3 deformedPosition = splinePos + rotatedOffset;
        
        float distanceAlongPenetrator = GetDistanceAlongLength(Selore_StartPosition, Selore_StartPosition + (startUp * Selore_PenetratorLength), vertexPos);
        
        // ------------------------------------------------------------------
        // Deform engagement gate.
        // Replaces the old `blend1` formula, which accidentally disabled the
        // deform entirely whenever the socket was farther than 1.5x plug
        // length (the common case). Behavior is configurable via the spline
        // options; by default only a generous distance cutoff applies and the
        // plug responds from any angle (180 deg = full sphere).
        // ------------------------------------------------------------------
        float3 toOrifice = o1.position - Selore_StartPosition;
        float orfDistance = length(toOrifice);
        float3 toOrificeDir = toOrifice / max(orfDistance, 1e-5);
        float applyLerp = 1;

        // Exit angle: between the plug's straight spine and the socket
        // direction. 180 deg limit = no limit.
        if (Selore_MaxExitAngleDeg < 179.5) {
            float exitAngle = degrees(acos(clamp(dot(startUp, toOrificeDir), -1, 1)));
            if (exitAngle > Selore_MaxExitAngleDeg) applyLerp = 0;
        }

        // Entrance angle: between the socket's forward and the approach
        // direction. Two-way rings accept entry from both sides, so skip the
        // check there (same as VRCFury).
        if (Selore_MaxEntranceAngleDeg < 179.5 && o1.type != SELORE_LIGHT_ROLE_RING_TWOWAY) {
            float entranceAngle = degrees(acos(clamp(dot(o1.normal, -toOrificeDir), -1, 1)));
            if (entranceAngle > Selore_MaxEntranceAngleDeg) applyLerp = 0;
        }

        // Hilt override: when fully seated in a hole, force engagement even if
        // an angle check above rejected the deform (same idea as VRCFury).
        if (Selore_HiltDistanceFactor > 0 && o1.type == SELORE_LIGHT_ROLE_HOLE) {
            if (orfDistance <= Selore_PenetratorLength * Selore_HiltDistanceFactor) applyLerp = 1;
        }

        // Distance fade: ramp deform out over the last 25% of the allowed
        // range so distant sockets ease off instead of popping. 0 = unlimited.
        if (Selore_MaxDistanceFactor > 0) {
            float maxDist = Selore_PenetratorLength * Selore_MaxDistanceFactor;
            applyLerp = min(applyLerp, 1 - smoothstep(maxDist * 0.75, maxDist, orfDistance));
        }

        float blend2 = distanceAlongPenetrator <= 0 ? 1 : 0;

        deformedPosition = lerp(deformedPosition, vertexPos.xyz, 1 - applyLerp);
        deformedNormal = lerp(deformedNormal, vertexNormal, 1 - applyLerp);
        
        deformedPosition = lerp(deformedPosition, vertexPos.xyz, blend2);
        deformedNormal = lerp(deformedNormal, vertexNormal, blend2);
        
        deformedPosition = lerp(vertexPos.xyz, deformedPosition, Selore_DeformStrength);
        deformedNormal = lerp(vertexNormal, deformedNormal, Selore_DeformStrength);
        
        const float nan = 0.0 / 0.0;
        const float4 nanPosition = float4(nan, nan, nan, nan);
        
        // hide section between point 1 and 2, as its inside body and supposed to not be visible.
        if (o1.type == SELORE_LIGHT_ROLE_HOLE) {
            float shrink = 0.0;
            bool hide = false;

            if (o2.isValid) {
                float shrinkIn  = smoothstep(0.90, 1.10, visualT);
                float shrinkOut = 1.0 - smoothstep(1.90, 2.10, visualT);
                shrink = saturate(shrinkIn * shrinkOut);
                hide = visualT > 1.10 && visualT < 1.90;
            } else {
                shrink = smoothstep(1.00, 1.15, visualT);
                hide = visualT > 1.15;
            }

            deformedPosition = lerp(deformedPosition, splinePos, shrink * Selore_DeformStrength);
                        
            if (hide) {
                deformedPosition = nanPosition;
            }
        }
        
        // Debug visualization
        // color = float4(distanceAlongPenetrator < 0 ? 1 : 0, visualT > 2 ? 1 : 0, distanceAlongPenetrator, 1);
        color = float4(o1.isValid, o2.isValid, distanceAlongPenetrator, 1);
        
        vertexPos = float4(deformedPosition, 1);
        vertexNormal = deformedNormal;
    }
}
#endif
