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
#ifndef SELORE_SPLINE_CHAIN
#define SELORE_SPLINE_CHAIN

#include "selore_globals.cginc"
#include "selore_spline.cginc"
#include "utils.cginc"

// Generic N-segment extension of the two-orifice spline in spline.cginc.
// The plug start plus up to SPS_PLUG_MAX_ENTRIES (5) chain sockets produce
// up to 5 cubic bezier segments (16 control points). Segment i runs from
// node i (start, or socket i-1) to node i+1 (socket i), with C1 continuity
// maintained by reflecting the entry handle into the exit handle - the same
// construction GetCurvePoints uses for p2/p4.

#define SELORE_CHAIN_MAX_SEGMENTS SPS_PLUG_MAX_ENTRIES
#define SELORE_CHAIN_MAX_POINTS (SELORE_CHAIN_MAX_SEGMENTS * 3 + 1)

// Build the control point array for `socketCount` chain sockets (1..5).
// startPos/startUp define the plug's straight spine; chain[i] holds socket
// positions and entry normals in object space.
void SeloreBuildChainCurve(
    float3 startPos,
    float3 startUp,
    Selore_OrificeData chain[SPS_PLUG_MAX_ENTRIES],
    uint socketCount,
    out float3 pts[SELORE_CHAIN_MAX_POINTS]
) {
    [unroll]
    for (uint initPt = 0u; initPt < SELORE_CHAIN_MAX_POINTS; initPt++) {
        pts[initPt] = float3(0, 0, 0);
    }
    // Node positions sit at p0, p3, p6, ... (every third control point).
    pts[0] = startPos;
    float handleLen = distance(startPos, chain[0].position) * Selore_BezierHandleSize;
    pts[1] = startPos + startUp * handleLen;

    [unroll]
    for (uint i = 0u; i < SPS_PLUG_MAX_ENTRIES; i++) {
        if (i >= socketCount) continue;
        uint node = (i + 1u) * 3u;
        pts[node] = chain[i].position;

        // Segment entering this node: exit handle mirrors the entry normal.
        if (i == 0u) {
            pts[node - 1u] = chain[i].position + chain[i].normal * handleLen;
        } else {
            float h = distance(chain[i - 1u].position, chain[i].position) * Selore_BezierHandleSize;
            pts[node - 1u] = chain[i].position + chain[i].normal * h;
        }

        // Exit handle: reflect the entry direction, matching GetCurvePoints'
        // p4 = p3 - o1Up * handleLen2. Written unconditionally so the final
        // segment has a well-defined shape even with no following node.
        float exitLen;
        if (i + 1u < socketCount) {
            exitLen = distance(chain[i].position, chain[i + 1u].position) * Selore_BezierHandleSize;
        } else {
            exitLen = handleLen;
        }
        pts[node + 1u] = chain[i].position - chain[i].normal * exitLen;
    }
}

// Arc length of one segment (3 control points starting at `segIndex * 3`).
float SeloreChainSegmentLength(float3 pts[SELORE_CHAIN_MAX_POINTS], uint segIndex) {
    uint b = segIndex * 3u;
    return CalculateArcLength(pts[b], pts[b + 1u], pts[b + 2u], pts[b + 3u], 1);
}

// Total arc length across all `segmentCount` segments.
float SeloreChainTotalLength(float3 pts[SELORE_CHAIN_MAX_POINTS], uint segmentCount) {
    float total = 0;
    [unroll]
    for (uint i = 0u; i < SELORE_CHAIN_MAX_SEGMENTS; i++) {
        if (i >= segmentCount) continue;
        total += SeloreChainSegmentLength(pts, i);
    }
    return total;
}

// Evaluate position/tangent at global t in [0, segmentCount], where the
// integer part selects the segment and the fraction the position within it.
void SeloreChainEval(
    float3 pts[SELORE_CHAIN_MAX_POINTS],
    uint segmentCount,
    float t,
    out float3 pos,
    out float3 tangent
) {
    uint seg = (uint)floor(t);
    if (seg >= segmentCount) seg = segmentCount - 1u;
    float localT = t - (float)seg;
    uint b = seg * 3u;
    pos = CubicBezier(pts[b], pts[b + 1u], pts[b + 2u], pts[b + 3u], localT);
    tangent = CubicBezierTangent(pts[b], pts[b + 1u], pts[b + 2u], pts[b + 3u], localT);
}

// Map a distance (meters along the path) to a global t, distributing across
// segments by their arc lengths - the N-segment generalization of the
// len1/len2 mapping in SeloreDeform.
float SeloreChainDistanceToT(
    float3 pts[SELORE_CHAIN_MAX_POINTS],
    uint segmentCount,
    float distanceMeters
) {
    float remaining = distanceMeters;
    [unroll]
    for (uint i = 0u; i < SELORE_CHAIN_MAX_SEGMENTS; i++) {
        if (i >= segmentCount) continue;
        float segLen = SeloreChainSegmentLength(pts, i);
        if (remaining < segLen) {
            return (float)i + remaining / max(0.001, segLen);
        }
        remaining -= segLen;
    }
    return (float)segmentCount;
}

#endif
