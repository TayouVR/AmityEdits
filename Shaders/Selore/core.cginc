// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2025 Tayou <git@tayou.org>
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
    float3 worldStartPosition = mul(unity_ObjectToWorld, Selore_StartPosition);
    
    Selore_OrificeData o1;
    Selore_OrificeData o2;
    GetOrifices(Selore_Channel, worldStartPosition, o1, o2);
    
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

        
        // Calculate Distance along mesh spine (Meters)
        // Project vector (vertex - start) onto the StartUp vector
        float distanceAlongSpine = dot(vertexPos.xyz - Selore_StartPosition, startUp);
        float currentPosMeters = GetDistanceAlongPath(Selore_StartPosition, Selore_StartRotation, vertexPos);

        // Calculate Curve Lengths for logic switching
        float len1 = CalculateArcLength(p0, p1, p2, p3, 1);
        float len2 = CalculateArcLength(p3, p4, p5, p6, 1);
        float totalLen = len1 + len2;
        float lenToWorkWith = lerp(len1, totalLen, Selore_AllTheWayThrough);
        
        // Determine Spline Position and Tangent
        float3 splinePos;
        float3 splineTangent;
        float visualT = 0; // For debugging color


        // CALCULATE SPLINE POSITION (Linear Extension vs Bezier)
        if (currentPosMeters > lenToWorkWith) {
            // --- LINEAR EXTENSION MODE ---
            // Calculate end of curve 2
            float3 endTangent = normalize(CubicBezierTangent(p3, p4, p5, p6, 1.0));
            float3 endPos = CubicBezier(p3, p4, p5, p6, 1.0);
            if (Selore_AllTheWayThrough < 0.5) {
                // If not all the way through, calculate end of curve 1 instead.
				endTangent = normalize(CubicBezierTangent(p0, p1, p2, p3, 1.0));
				endPos = CubicBezier(p0, p1, p2, p3, 1.0);
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
        // I'm not great with the math here, so I'm documenting each line so I can still understand whats happening here i nthe future.
        
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
        
        // Blend strength
        // TODO: these lerps could probably all be one, but it was easier for me to do it like this. I want to consolidate them eventually.
        float blend1 = clamp((len1 - Selore_PenetratorLength * 1.5f) * 5, 0, 1);
        float blend2 = distanceAlongPenetrator <= 0 ? 1 : 0;

        deformedPosition = lerp(deformedPosition, vertexPos.xyz, blend1);
        deformedNormal = lerp(deformedNormal, vertexNormal, blend1);
        
        deformedPosition = lerp(deformedPosition, vertexPos.xyz, blend2);
        deformedNormal = lerp(deformedNormal, vertexNormal, blend2);
        
        deformedPosition = lerp(vertexPos.xyz, deformedPosition, Selore_DeformStrength);
        deformedNormal = lerp(vertexNormal, deformedNormal, Selore_DeformStrength);
        
        // hide section between point 1 and 2, as its inside body and supposed to not be visible.
        if (o1.type == SELORE_LIGHT_ROLE_HOLE && (visualT > 1 && visualT < 2)) {
            float nan = 0.0 / 0.0;
            float4 nanPosition = float4(nan, nan, nan, nan);
	        deformedPosition = nanPosition;
        }
        
        // Debug visualization
        color = float4(distanceAlongPenetrator < 0 ? 1 : 0, visualT > 2 ? 1 : 0, distanceAlongPenetrator, 1);
        
        vertexPos = float4(deformedPosition, 1);
        vertexNormal = deformedNormal;
    }
}
#endif
