// SPDX-License-Identifier: GPL-3.0-only
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
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
#include "spline_chain.cginc"
#include "utils.cginc"

#if defined(SELORE_SPS)
// Read the full resolved socket chain (entries S1..S5) from the plug cell for
// multi-point pathing, converting each entry to object space. Returns the
// number of valid chain sockets (0 = no target resolved).
uint selore_sps_read_chain(
    float useSps,
    SpsTexture gridTex,
    uint uniqueId,
    uint playerId,
    out Selore_OrificeData outChain[SPS_PLUG_MAX_ENTRIES]
) {
    [unroll]
    for (uint i = 0u; i < SPS_PLUG_MAX_ENTRIES; i++) {
        outChain[i].isValid = false;
        outChain[i].type = SELORE_LIGHT_ROLE_UNKNOWN;
        outChain[i].position = float3(100, 100, 100);
        outChain[i].normal = float3(0, 0, 1);
        outChain[i].normalLightPosition = float3(0, 0, 0);
    }

    if (!sps_to_bool(useSps)) return 0u;

    uint slotCount = sps_socket_slot_count();
    uint slotSeed = sps_hash_id(uniqueId, playerId);

    [unroll]
    for (uint r = 0u; r < SPS_CELL_REPLICA_COUNT; r++) {
        uint candidateIdx = sps_hashed_screen_slot_index_from_id(slotSeed, r);
        if (candidateIdx >= slotCount) continue;

        SpsCell cell = sps_get_cell(gridTex, (int)candidateIdx);
        if (!sps_cell_check_magic(cell)) continue;
        if (cell.read_uint(SPS_HEADER_PRODUCT_INDEX) != SPS_PRODUCT_PLUG) continue;
        if (sps_cell_header_player_id(cell) != playerId) continue;
        if (sps_cell_header_unique_id(cell) != uniqueId) continue;

        uint count = 0u;
        bool ended = false;
        [unroll]
        for (uint e = 0u; e < SPS_PLUG_MAX_ENTRIES; e++) {
            float3 posW = cell.read_float3(SPS_PLUG_ENTRY_BASE(e) + SPS_PLUG_ENTRY_POS);
            float3 fwdW = cell.read_float3(SPS_PLUG_ENTRY_BASE(e) + SPS_PLUG_ENTRY_FWD);
            uint flags = cell.read_uint(SPS_PLUG_ENTRY_BASE(e) + SPS_PLUG_ENTRY_FLAGS);

            // A zero position/forward marks the end of the resolved chain.
            if (all(posW == 0.0) && all(fwdW == 0.0)) ended = true;
            if (ended) continue;

            // Localize to object space (the deform spline runs in object space).
            outChain[e].position = sps_toLocal(posW);
            outChain[e].normal = sps_direction_toLocal(fwdW);
            outChain[e].normalLightPosition = sps_toLocal(posW + fwdW);
            // Map SPS flags to a Selore role (mirrors VRCFury's encoding):
            // HOLE -> hole, DOUBLE_SIDED -> two-way ring, neither -> one-way ring.
            if (sps_has_flag(flags, SPS_SOCKET_FLAG_HOLE)) {
                outChain[e].type = SELORE_LIGHT_ROLE_HOLE;
            } else if (sps_has_flag(flags, SPS_SOCKET_FLAG_DOUBLE_SIDED)) {
                outChain[e].type = SELORE_LIGHT_ROLE_RING_TWOWAY;
            } else {
                outChain[e].type = SELORE_LIGHT_ROLE_RING_ONEWAY;
            }
            outChain[e].isValid = true;
            count++;
        }
        return count;
    }

    return 0u;
}
#endif

void SeloreDeform(inout float4 vertexPos, inout float3 vertexNormal, inout float4 color) {
    float3 worldStartPosition = mul(unity_ObjectToWorld, float4(Selore_StartPosition, 1.0)).xyz;
    
    Selore_OrificeData o1;
    Selore_OrificeData o2;

    // Unified socket chain driving the deform: up to SPS_PLUG_MAX_ENTRIES (5)
    // sockets for SPS2 multi-point pathing, or 1-2 from the legacy lights.
    Selore_OrificeData chain[SPS_PLUG_MAX_ENTRIES] = (Selore_OrificeData[SPS_PLUG_MAX_ENTRIES])0;
    uint chainCount = 0u;

#if defined(SELORE_SPS)
    {
        SpsTexture gridTex = SPS_GET_TEX(_VFGridFinal);
        uint uniqueId = sps_to_uint(Selore_SpsId);
        if (uniqueId == 0u) uniqueId = sps_hash_world(sps_object_origin_world(), 0u);
        uint playerId = sps_to_uint(Selore_SpsPlayerId);

        chainCount = selore_sps_read_chain(Selore_UseSps, gridTex, uniqueId, playerId, chain);
        if (chainCount > 0u) {
            o1 = chain[0];
            o2 = chainCount > 1u ? chain[1] : chain[0];
            o2.isValid = chainCount > 1u;
        } else {
            o1.isValid = false;
            o2.isValid = false;
        }
    }
#else
    GetOrifices(Selore_Channel, worldStartPosition, o1, o2);
    if (o1.isValid) {
        chain[0] = o1;
        chainCount = 1u;
        // Legacy "all the way through": append the second orifice as a chain hop.
        if (o2.isValid && Selore_AllTheWayThrough > 0.5) {
            chain[1] = o2;
            chainCount = 2u;
        }
    }
#endif
    
    if (o1.isValid) {
        float3x3 startMatrix = EulerToRotMatrix(Selore_StartRotation);
        float3 startUp = mul(startMatrix, float3(0,1,0)); 

        // Build the N-segment spline through the chain (start + each socket).
        float3 pts[SELORE_CHAIN_MAX_POINTS] = (float3[SELORE_CHAIN_MAX_POINTS])0;
        [unroll]
        for (uint initPt = 0u; initPt < SELORE_CHAIN_MAX_POINTS; initPt++) {
            pts[initPt] = Selore_StartPosition;
        }
        SeloreBuildChainCurve(Selore_StartPosition, startUp, chain, chainCount, pts);
        uint segmentCount = chainCount;
        
        // Calculate Distance along mesh spine (Meters)
        // Project vector (vertex - start) onto the StartUp vector
        float distanceAlongSpine = dot(vertexPos.xyz - Selore_StartPosition, startUp);
        float currentPosMeters = GetDistanceAlongPath(Selore_StartPosition, Selore_StartRotation, vertexPos);

        // Calculate Curve Lengths for logic switching
        float totalLen = SeloreChainTotalLength(pts, segmentCount);
        float lenToWorkWith = totalLen;
        
        // Determine Spline Position and Tangent
        float3 splinePos;
        float3 splineTangent;
        float visualT = 0; // For debugging color


        // CALCULATE SPLINE POSITION (Linear Extension vs Bezier)
        if (currentPosMeters > lenToWorkWith) {
            // --- LINEAR EXTENSION MODE ---
            uint lastSeg = segmentCount - 1u;
            uint endBase = lastSeg * 3u;
            float3 endPos = pts[endBase + 3u];
            float3 endTangent = normalize(CubicBezierTangent(
                pts[endBase], pts[endBase + 1u], pts[endBase + 2u], pts[endBase + 3u], 1.0));
            
            float excessDistance = currentPosMeters - lenToWorkWith;
            
            splinePos = endPos + endTangent * excessDistance;
            splineTangent = endTangent;
            visualT = (float)segmentCount + 0.5; // Debug color (past the last node)
        }
        else {
            // --- BEZIER MODE ---
            // Map distance to t based on segment lengths
            float t = SeloreChainDistanceToT(pts, segmentCount, currentPosMeters);
            visualT = t;

            SeloreChainEval(pts, segmentCount, t, splinePos, splineTangent);
            splineTangent = normalize(splineTangent);
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
        
        // Hide the mesh section inside the terminal socket when that socket is
        // a hole (the interior isn't supposed to be visible). visualT counts
        // segments, so node k sits at t=k; the section past the last node is
        // inside the terminal socket.
        Selore_OrificeData terminal = chain[chainCount - 1u];
        if (terminal.type == SELORE_LIGHT_ROLE_HOLE) {
            float lastNodeT = (float)(chainCount - 1u);
            float shrink = 0.0;
            bool hide = false;

            if (chainCount > 1u) {
                // Interior section of the chain (between first and last node)
                // hides when the terminal hole swallows the path.
                float shrinkIn  = smoothstep(lastNodeT - 0.10, lastNodeT + 0.10, visualT);
                float shrinkOut = 1.0;
                shrink = saturate(shrinkIn * shrinkOut);
                hide = visualT > lastNodeT + 0.10;
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
