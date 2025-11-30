#ifndef SELORE_UTILS
#define SELORE_UTILS

// Helper function to create rotation matrix from euler angles
float3x3 EulerToRotMatrix(float3 euler)
{
    float3 sinXYZ = sin(radians(euler));
    float3 cosXYZ = cos(radians(euler));

    float3x3 rotX = float3x3(
        1,		  0,		 0,
        0, cosXYZ.x, -sinXYZ.x,
        0, sinXYZ.x,  cosXYZ.x
    );

    float3x3 rotY = float3x3(
        cosXYZ.y,  0, sinXYZ.y,
        0,		   1,		 0,
        -sinXYZ.y, 0, cosXYZ.y
    );

    float3x3 rotZ = float3x3(
        cosXYZ.z, -sinXYZ.z, 0,
        sinXYZ.z, cosXYZ.z,  0,
        0,		  0,		 1
    );

    return mul(mul(rotZ, rotY), rotX);
}

// Calculate the rotation matrix that aligns vector 'from' to vector 'to'
float3x3 FromToRotation(float3 fromVec, float3 toVec)
{
    float3 v = cross(fromVec, toVec);
    float e = dot(fromVec, toVec);
                
    if (e > 0.999999) return float3x3(1,0,0, 0,1,0, 0,0,1); // Identity if parallel
    if (e < -0.999999) return float3x3(1,0,0, 0,1,0, 0,0,1); // Identity if opposite (simplified)

    float h = 1.0 / (1.0 + e);
                
    return float3x3(
        e + h * v.x * v.x,          h     * v.x    * v.y - v.z,    h     * v.x * v.z + v.y,
        h     * v.x * v.y + v.z,    e + h * v.y    * v.y,          h     * v.y * v.z - v.x,
        h     * v.x * v.z - v.y,    h     * v.y    * v.z + v.x,    e + h * v.z * v.z
    );
}
#endif
