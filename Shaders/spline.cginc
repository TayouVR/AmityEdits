
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