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
#ifndef SELORE_SPLINE
#define SELORE_SPLINE

#include "utils.cginc"

float3 CubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t) {
    float t2 = t * t;
    float t3 = t2 * t;
    float mt = 1 - t;
    float mt2 = mt * mt;
    float mt3 = mt2 * mt;
			    
    return mt3 * p0 + 
           3 * mt2 * t * p1 + 
           3 * mt * t2 * p2 + 
           t3 * p3;
}

float3 CubicBezierTangent(float3 p0, float3 p1, float3 p2, float3 p3, float t) {
    float t2 = t * t;
    float mt = 1 - t;
    float mt2 = mt * mt;
    
    return 3 * mt2 * (p1 - p0) +
           6 * mt * t * (p2 - p1) +
           3 * t2 * (p3 - p2);
}

// Logic to handle 2 segments (0-1 and 1-2)
float3 GetSplinePosition(float3 p0, float3 p1, float3 p2, float3 p3, float3 p4, float3 p5, float3 p6, float t) {
    if (t < 1.0) {
        return CubicBezier(p0, p1, p2, p3, t);
    }
    return CubicBezier(p3, p4, p5, p6, t - 1.0);
}

float3 GetSplineTangent(float3 p0, float3 p1, float3 p2, float3 p3, float3 p4, float3 p5, float3 p6, float t) {
    if (t < 1.0) {
        return CubicBezierTangent(p0, p1, p2, p3, t);
    }
    return CubicBezierTangent(p3, p4, p5, p6, max(0, t - 1.0));
}

float GetDistanceAlongPath(float3 startPos, float3 startRot, float4 position) {
    float3x3 rotMatrix = EulerToRotMatrix(startRot);
    float3 forward = float3(rotMatrix[0][1], rotMatrix[1][1], rotMatrix[2][1]); // Y-Axis
    float3 toVertex = position.xyz - startPos;
    return dot(toVertex, forward);
}

float GetT(float3 startPos, float3 startRot, float4 position, float length) {
    // Create rotation matrix from euler angles
    float3x3 rotMatrix = EulerToRotMatrix(startRot);
			    
    // Get forward direction from rotation (assuming Z-forward convention)
    float3 forward = float3(rotMatrix[0][1], rotMatrix[1][1], rotMatrix[2][1]);
			    
    // Vector from start position to current vertex position
    float3 toVertex = position.xyz - startPos;
			    
    // Project onto the forward direction to get distance along the path
    float distanceAlongPath = dot(toVertex, forward);
			    
    // Normalize by length to get t value (0-1 range for the path, but can be negative or >1)
    return distanceAlongPath / length;
}

// Calculate approximate arc length of the curve from t=0 to t=targetT
float CalculateArcLength(float3 p0, float3 p1, float3 p2, float3 p3, float targetT, int samples = 50) {
    float length = 0;
    float3 previousPoint = p0;
	            
    for (int i = 1; i <= samples; i++) {
        float t = (i / (float)samples) * targetT;
        float3 currentPoint = CubicBezier(p0, p1, p2, p3, t);
        length += distance(previousPoint, currentPoint);
        previousPoint = currentPoint;
    }
	            
    return length;
}
        
float GetDistanceAlongLength(float3 startPos, float3 target, float3 position) {
    float3 startEndVector = target - startPos;
    float3 startVertexPosVector = position - startPos;
	            
    // Project onto the forward direction to get distance along the path
    float distanceAlongPath = dot(startVertexPosVector, startEndVector);
				     
    // Normalize by length to get t value (0-1 range for the path, but can be negative or >1)
    return distanceAlongPath / dot(startEndVector, startEndVector);
}
	        
void GetCurvePoints(
    out float3 p0, out float3 p1, out float3 p2, 
    out float3 p3, out float3 p4, out float3 p5, 
    out float3 p6, Selore_OrificeData o1, Selore_OrificeData o2
    ) {
    // Calculate Basis vectors from Rotations
    float3x3 startMatrix = EulerToRotMatrix(Selore_StartRotation);
    float3 startUp = mul(startMatrix, float3(0,1,0)); 
    float3 startRight = mul(startMatrix, float3(1,0,0));
    float3 startForward = mul(startMatrix, float3(0,0,1)); // Z-axis

    float3 o1Up = o1.normal; 
    float3 o2Up = -o2.normal; 
    
    // TODO: make handle length dynamic based on distance between points
    float handleLen = Selore_PenetratorLength * Selore_BezierHandleSize;
    float handleLen1 = distance(Selore_StartPosition, o1.position) * Selore_BezierHandleSize;
    float handleLen2 = distance(o1.position, o2.position) * Selore_BezierHandleSize;

    // Define Control Points (P0 - P6)
    p0 = Selore_StartPosition;
    p1 = p0 + (startUp * handleLen1);
    
    p3 = o1.position;
    // Entering orifice 1
    p2 = p3 + (o1Up * handleLen1);
    // Exiting orifice 1 (opposite direction to maintain smoothness)
    p4 = p3 - (o1Up * handleLen2);

    p6 = o2.position;
    // Entering orifice 2
    p5 = p6 + (o2Up * handleLen2); // Note: Check direction logic, usually -Up if going into it
}
#endif
