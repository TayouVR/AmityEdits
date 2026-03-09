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
using System;
using System.Collections.Generic;
using System.Linq;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {

    /**
     * This was used to test and debug spline math, fine tune parameters, etc.
     * The shader logic test won't be useful on your avatar, but may be of use if you plan to tinker with Selore, or your own bezier curves.
     */
    [CustomEditor(typeof(ShaderLogicTest))]
    public class ShaderLogicTestEditor: Editor {
        public struct Selore_OrificeData {
            public Vector3 position;
            public Vector3 normal; // The calculated forward direction
            public Vector3 normalLightPosition; // The raw position of the normal light
            public int type;
            public bool isValid;
        };

        private const int SELORE_LIGHT_ROLE_UNKNOWN = 0;
        private const int SELORE_LIGHT_ROLE_HOLE = 1;
        private const int SELORE_LIGHT_ROLE_RING_TWOWAY = 2;
        private const int SELORE_LIGHT_ROLE_RING_ONEWAY = 3;
        private const int SELORE_LIGHT_ROLE_NORMAL = 4;
        
        private static float Map(float value, float min1, float max1, float min2, float max2)
        {
            if (Mathf.Approximately(max1 - min1, 0f)) {
                return min2;
            }

            return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
        }

        private static float SaturatedMap(float value, float min, float max)
        {
            return Mathf.Clamp01(Map(value, min, max, 0f, 1f));
        }

        private static Vector3 SafeNormalize(Vector3 value)
        {
            return value.sqrMagnitude <= Mathf.Epsilon ? Vector3.forward : value.normalized;
        }

        private static float AngleBetween(Vector3 a, Vector3 b)
        {
            float dot = Mathf.Clamp(Vector3.Dot(SafeNormalize(a), SafeNormalize(b)), -1f, 1f);
            return Mathf.Acos(dot);
        }

        private static Vector3 NearestNormal(Vector3 forward, Vector3 approximate)
        {
            Vector3 result = Vector3.Cross(forward, Vector3.Cross(approximate, forward));
            return SafeNormalize(result);
        }

        private static Quaternion EulerToRotation(Vector3 euler)
        {
            Quaternion rotX = Quaternion.AngleAxis(euler.x, Vector3.right);
            Quaternion rotY = Quaternion.AngleAxis(euler.y, Vector3.up);
            Quaternion rotZ = Quaternion.AngleAxis(euler.z, Vector3.forward);
            return rotZ * rotY * rotX;
        }
        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float minT = 1f - t;
            return
                minT * minT * minT * p0 +
                3f * minT * minT * t * p1 +
                3f * minT * t * t * p2 +
                t * t * t * p3;
        }

        private static Vector3 BezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float minT = 1f - t;
            return
                3f * minT * minT * (p1 - p0) +
                6f * minT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        private static void BezierSolve(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float lookingForLength,
            out float curveLength,
            out Vector3 position,
            out Vector3 forward,
            out Vector3 up
        ) {
            const int sampleCount = 50;

            float[] sampledT = new float[sampleCount];
            float[] sampledLength = new float[sampleCount];
            Vector3[] sampledUp = new Vector3[sampleCount];

            float totalLength = 0f;
            Vector3 lastPoint = p0;

            sampledT[0] = 0f;
            sampledLength[0] = 0f;
            sampledUp[0] = Vector3.up;

            for (int i = 1; i < sampleCount; i++) {
                float t = i / (float)(sampleCount - 1);
                Vector3 currentPoint = Bezier(p0, p1, p2, p3, t);
                Vector3 currentForward = SafeNormalize(BezierDerivative(p0, p1, p2, p3, t));
                Vector3 lastUp = sampledUp[i - 1];
                Vector3 currentUp = NearestNormal(currentForward, lastUp);

                sampledT[i] = t;
                totalLength += Vector3.Distance(currentPoint, lastPoint);
                sampledLength[i] = totalLength;
                sampledUp[i] = currentUp;
                lastPoint = currentPoint;
            }

            float adjustedT = 1f;
            Vector3 approximateUp = sampledUp[sampleCount - 1];

            for (int i = 1; i < sampleCount; i++) {
                if (lookingForLength <= sampledLength[i]) {
                    float denom = sampledLength[i] - sampledLength[i - 1];
                    float fraction = Mathf.Approximately(denom, 0f)
                        ? 0f
                        : Map(lookingForLength, sampledLength[i - 1], sampledLength[i], 0f, 1f);

                    adjustedT = Mathf.Lerp(sampledT[i - 1], sampledT[i], fraction);
                    approximateUp = Vector3.Lerp(sampledUp[i - 1], sampledUp[i], fraction);
                    break;
                }
            }

            curveLength = totalLength;
            float finalT = Mathf.Clamp01(adjustedT);
            position = Bezier(p0, p1, p2, p3, finalT);
            forward = SafeNormalize(BezierDerivative(p0, p1, p2, p3, finalT));
            up = NearestNormal(forward, approximateUp);
        }
                private struct OrificeTransformInput
        {
            public Transform orificeTransform;
            public Transform normalTransform;
            public int role;
        }

        private static Selore_OrificeData BuildOrificeFromTransforms(
            OrificeTransformInput input,
            Vector3 startPos,
            bool mirrorShaderNormalBug
        ) {
            Selore_OrificeData result = new Selore_OrificeData {
                isValid = false,
                type = SELORE_LIGHT_ROLE_UNKNOWN,
                position = new Vector3(100f, 100f, 100f),
                normal = Vector3.forward,
                normalLightPosition = Vector3.zero
            };

            if (input.orificeTransform == null) {
                return result;
            }

            result.isValid = true;
            result.type = input.role;
            result.position = input.orificeTransform.position;
            result.normalLightPosition = input.normalTransform != null
                ? input.normalTransform.position
                : Vector3.zero;

            if (input.normalTransform != null) {
                Vector3 rawNormal = mirrorShaderNormalBug
                    ? SafeNormalize(result.position + result.normalLightPosition)
                    : SafeNormalize(result.normalLightPosition - result.position);

                result.normal = rawNormal;
            } else {
                result.normal = SafeNormalize(-result.position);
            }

            if (result.type == SELORE_LIGHT_ROLE_RING_TWOWAY || result.type == SELORE_LIGHT_ROLE_RING_ONEWAY) {
                Vector3 toStart = SafeNormalize(startPos - result.position);
                if (Vector3.Dot(result.normal, toStart) < 0f) {
                    result.normal = -result.normal;
                }
            }

            return result;
        }

        private static void GetOrificesFromTransforms(
            Vector3 startPos,
            OrificeTransformInput first,
            OrificeTransformInput second,
            bool mirrorShaderNormalBug,
            out Selore_OrificeData o1,
            out Selore_OrificeData o2
        ) {
            o1 = new Selore_OrificeData {
                isValid = false,
                type = SELORE_LIGHT_ROLE_UNKNOWN,
                position = new Vector3(100f, 100f, 100f),
                normal = Vector3.forward,
                normalLightPosition = Vector3.zero
            };

            o2 = new Selore_OrificeData {
                isValid = false,
                type = SELORE_LIGHT_ROLE_UNKNOWN,
                position = new Vector3(100f, 100f, 100f),
                normal = Vector3.forward,
                normalLightPosition = Vector3.zero
            };

            List<Selore_OrificeData> found = new List<Selore_OrificeData>(2);

            Selore_OrificeData candidate1 = BuildOrificeFromTransforms(first, startPos, mirrorShaderNormalBug);
            if (candidate1.isValid) {
                found.Add(candidate1);
            }

            Selore_OrificeData candidate2 = BuildOrificeFromTransforms(second, startPos, mirrorShaderNormalBug);
            if (candidate2.isValid) {
                found.Add(candidate2);
            }

            found = found
                .OrderBy(orifice => Vector3.Distance(startPos, orifice.position))
                .ToList();

            if (found.Count > 0) {
                o1 = found[0];
            }

            if (found.Count > 1) {
                o2 = found[1];
            }
        }
        
        // Helper function to create rotation matrix from euler angles
        private static Matrix4x4 EulerToRotMatrix(Vector3 euler)
        {
            float x = euler.x * Mathf.Deg2Rad;
            float y = euler.y * Mathf.Deg2Rad;
            float z = euler.z * Mathf.Deg2Rad;

            float sx = Mathf.Sin(x);
            float cx = Mathf.Cos(x);
            float sy = Mathf.Sin(y);
            float cy = Mathf.Cos(y);
            float sz = Mathf.Sin(z);
            float cz = Mathf.Cos(z);

            Matrix4x4 rotX = Matrix4x4.identity;
            rotX.m11 = cx;
            rotX.m12 = -sx;
            rotX.m21 = sx;
            rotX.m22 = cx;

            Matrix4x4 rotY = Matrix4x4.identity;
            rotY.m00 = cy;
            rotY.m02 = sy;
            rotY.m20 = -sy;
            rotY.m22 = cy;

            Matrix4x4 rotZ = Matrix4x4.identity;
            rotZ.m00 = cz;
            rotZ.m01 = -sz;
            rotZ.m10 = sz;
            rotZ.m11 = cz;

            return rotZ * rotY * rotX;
        }
    
        public static void GetCurvePoints(
            out Vector3 p0, out Vector3 p1, out Vector3 p2, 
            out Vector3 p3, out Vector3 p4, out Vector3 p5, 
            out Vector3 p6, Selore_OrificeData o1, Selore_OrificeData o2,
            Vector3 startPosition,
            Vector3 startRotation,
            float penetratorLength,
            float bezierHandleSize
        ) {
            // Calculate Basis vectors from Rotations
            Matrix4x4 startMatrix = EulerToRotMatrix(startRotation);
            Vector3 startUp = startMatrix.MultiplyVector(new Vector3(0,1,0)); 
            Vector3 startRight = startMatrix.MultiplyVector(new Vector3(1,0,0));
            Vector3 startForward = startMatrix.MultiplyVector(new Vector3(0,0,1)); // Z-axis

            Vector3 o1Up = o1.normal; 
            Vector3 o2Up = -o2.normal; 
    
            // TODO: make handle length dynamic based on distance between points
            float handleLen = penetratorLength * bezierHandleSize;
            float handleLen1 = Vector3.Distance(startPosition, o1.position) * bezierHandleSize;
            float handleLen2 = Vector3.Distance(o1.position, o2.position) * bezierHandleSize;

            // Define Control Points (P0 - P6)
            p0 = startPosition;
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

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(ShaderLogicTest targetObject, GizmoType gizmoType) {
            Vector3 worldStart = targetObject.transform.TransformPoint(targetObject._StartPosition);

            GetOrificesFromTransforms(
                worldStart,
                new OrificeTransformInput {
                    orificeTransform = targetObject.Orifice1Transform,
                    normalTransform = targetObject.Orifice1NormalTransform,
                    role = SELORE_LIGHT_ROLE_HOLE
                },
                new OrificeTransformInput {
                    orificeTransform = targetObject.Orifice2Transform,
                    normalTransform = targetObject.Orifice2NormalTransform,
                    role = SELORE_LIGHT_ROLE_HOLE
                },
                mirrorShaderNormalBug: true,
                out var o1,
                out var o2
            );
            GetCurvePoints(
                out Vector3 p0,
                out Vector3 p1,
                out Vector3 p2,
                out Vector3 p3,
                out Vector3 p4,
                out Vector3 p5,
                out Vector3 p6,
                o1, o2, worldStart,
                targetObject._StartRotation,
                targetObject._PenetratorLength,
                targetObject.bezierHandleSize
            );
            
            
            var penetratorForward = Quaternion.Euler(targetObject._StartRotation) * Vector3.up;

            // Pre-calculate the original basis vectors (Straight orientation)
            var startRot = Quaternion.Euler(targetObject._StartRotation);
            var startRight = startRot * Vector3.right;
            var startForward = startRot * Vector3.forward;

            List<Vector3> vertecies;
            bool meshIsUsed = targetObject.debugMesh != null && targetObject.useDebugMesh;
            // read verts from mesh
            if (meshIsUsed) {
                vertecies = targetObject.debugMesh.vertices.Where((_, i) => i % 10 == 0).ToList();
            } else {
                vertecies = new List<Vector3>();
                for (int i = 0; i < targetObject.sampleCount; i++) {
                    vertecies.Add(p0 + (Quaternion.Euler(targetObject._StartRotation) * new Vector3(0, targetObject._PenetratorLength * i / targetObject.sampleCount * 1.5f - targetObject._PenetratorLength * 0.25f, 0)));
                }
            }

            var orifice2direction = targetObject.Orifice2Transform.rotation * Vector3.up;
            var penetratorTip = p0 + (Quaternion.Euler(targetObject._StartRotation) * new Vector3(0, targetObject._PenetratorLength, 0));
            
            var distanceToNearestOrifice = Vector3.Distance(p3, p0);
            var distanceToSecondNearestOrifice = Vector3.Distance(p6, p0);
            // penetrator pill
            GizmoUtils.DrawCappedCylinder(
                p0,
                penetratorTip,
                0.02f,
                new Color(1f, 0.5f, 0)
            );
            
            // start arrow
            GizmoUtils.DrawArrow(
                p0,
                p1,
                new Color(0f, 0.5f, 1f)
            );
            
            // target point
            GizmoUtils.DrawArrow(
                p3,
                p2,
                new Color(0.5f, 1f, 0)
            );
            
            // target point
            GizmoUtils.DrawArrow(
                p6,
                p5,
                new Color(1f, 0f, 0.5f)
            );
            GizmoUtils.DrawArrow(
                p3,
                p4,
                new Color(1f, 0f, 0.5f)
            );
            
            float len1 = CalculateArcLength(p0, p1, p2, p3, 1f);
            float len2 = CalculateArcLength(p3, p4, p5, p6, 1f);
            float totalCurveLength = len1 + len2;

            var vertCount = targetObject.sampleCount;
            foreach (var vertexPos in vertecies) {
                var distanceAlongPenetrator = GetDistanceAlongPenetrator(p0, penetratorTip, vertexPos);
                var t = FindSplineT(p0, p1, p2, p3, p4, p5, p6, distanceAlongPenetrator * targetObject._PenetratorLength); 
                
                // Convert normalized distance (0-1) to actual meters
                var targetArcLength = distanceAlongPenetrator * targetObject._PenetratorLength;

                Vector3 splinePos;
                Vector3 splineTangent;
                
                // LINEAR EXTENSION LOGIC
                if (targetArcLength > totalCurveLength) {
                    // We are past the curve, continue straight
                    float excessDistance = targetArcLength - totalCurveLength;
                    
                    // Get the tangent and position at the very end of the curve (Segment 2, t=1.0)
                    Vector3 endTangent = CubicBezierTangent(p3, p4, p5, p6, 1f).normalized;
                    Vector3 endPosition = CubicBezier(p3, p4, p5, p6, 1f); // Should be p6
                    
                    splinePos = endPosition + endTangent * excessDistance;
                    splineTangent = endTangent;
                } else {
                    // 1. Get Position and Tangent on the curve
                    splinePos = GetSplinePosition(p0, p1, p2, p3, p4, p5, p6, t);
                    splineTangent = GetSplineTangent(p0, p1, p2, p3, p4, p5, p6, t).normalized;
                }

                // 2. Calculate the offset of the vertex from the straight spine (Pre-deformation)
                //    We project the vertex onto the center line to find the perpendicular offset
                Vector3 pointOnStraightSpine = p0 + penetratorForward * (distanceAlongPenetrator * targetObject._PenetratorLength);
                Vector3 offsetFromSpine = vertexPos - pointOnStraightSpine;

                // 3. Construct the Basis Transformation
                //    We find the rotation that aligns the Original Up (penetratorForward) to the New Up (splineTangent)
                Quaternion rotationToSpline = Quaternion.FromToRotation(penetratorForward, splineTangent);
                
                //    Calculate the new basis vectors for this specific point on the curve
                Vector3 newRight = rotationToSpline * startRight;
                Vector3 newForward = rotationToSpline * startForward;
                
                // 4. Reconstruct the offset in the new Basis
                //    (This maintains the volume/thickness of the mesh while bending it)
                float localX = Vector3.Dot(offsetFromSpine, startRight);
                float localZ = Vector3.Dot(offsetFromSpine, startForward);
                
                Vector3 rotatedOffset = (newRight * localX) + (newForward * localZ);

                // 5. Final Position
                var deformedPos = splinePos + rotatedOffset;
                
                var lerpFactor =
                    Mathf.Clamp((distanceToNearestOrifice - targetObject._PenetratorLength * 1.5f) * 5, 0, 1) +
                    (t <= 0 ? 1 : 0);

                // Blend between original and deformed based on distance/logic
                deformedPos = Vector3.Lerp(deformedPos, vertexPos, lerpFactor);
                var bezierTangent = Vector3.Lerp(splineTangent, penetratorForward, lerpFactor);
                
                var gizmoColor = new Color(distanceAlongPenetrator < 0 ? 1 : 0, t > 2 ? 1 : 0,
                    distanceAlongPenetrator, 1);
                if (meshIsUsed) {
                    GizmoUtils.DrawArrow(deformedPos, deformedPos + bezierTangent * 0.01f, gizmoColor);
                    GizmoUtils.DrawSphere(deformedPos, 0.005f, gizmoColor);
                } else {
                    GizmoUtils.DrawDisc(deformedPos, bezierTangent, 0.01f, gizmoColor);
                }
            }
        }
        
        static Vector3 GetSplinePosition(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Vector3 p6, float t) {
            if (t < 1f) {
                return CubicBezier(p0, p1, p2, p3, t);
            }
            return CubicBezier(p3, p4, p5, p6, t - 1f);
        }

        static Vector3 GetSplineTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Vector3 p6, float t) {
            if (t < 1f) {
                return CubicBezierTangent(p0, p1, p2, p3, t);
            }

            return CubicBezierTangent(p3, p4, p5, p6, Mathf.Clamp(t - 1f, 0, 1));
        }

        // Calculate approximate arc length of the curve from t=0 to t=targetT
        static float CalculateArcLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetT, int samples = 50) {
            float length = 0f;
            Vector3 previousPoint = p0;
            
            for (int i = 1; i <= samples; i++) {
                float t = (i / (float)samples) * targetT;
                Vector3 currentPoint = CubicBezier(p0, p1, p2, p3, t);
                length += Vector3.Distance(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
            
            return length;
        }

        // Find the t value that corresponds to a desired arc length along the curve
        static float FindSplineT(
            Vector3 p0, Vector3 p1, Vector3 p2, 
            Vector3 p3, Vector3 p4, Vector3 p5, 
            Vector3 p6, float targetArcLength
        ) {
            // Calculate length of the first segment (p0 -> p3)
            float len1 = CalculateArcLength(p0, p1, p2, p3, 1f);

            if (targetArcLength <= len1) {
                // Target is within the first segment
                return FindTAtArcLength(p0, p1, p2, p3, targetArcLength);
            } else {
                // Target is in the second segment (or beyond)
                // Search on the second curve, subtracting the length of the first one
                // We add 1f to the result to map it to the 1-2 range
                return 1f + FindTAtArcLength(p3, p4, p5, p6, targetArcLength - len1);
            }
        }

        // Find the t value that corresponds to a desired arc length along the curve
        static float FindTAtArcLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetArcLength) {
            // Binary search to find the t value
            float minT = 0f;
            float maxT = 2f; // Allow going past the curve end (t > 1)
            float epsilon = 0.001f;
            
            // Quick check if we're before the start
            if (targetArcLength <= 0f) return 0f;
            
            for (int iteration = 0; iteration < 20; iteration++) {
                float midT = (minT + maxT) * 0.5f;
                float arcLength = CalculateArcLength(p0, p1, p2, p3, midT);
                
                if (Mathf.Abs(arcLength - targetArcLength) < epsilon) {
                    return midT;
                }
                
                if (arcLength < targetArcLength) {
                    minT = midT;
                } else {
                    maxT = midT;
                }
            }
            
            return (minT + maxT) * 0.5f;
        }
        
        static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
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
        
        // First derivative of the cubic Bezier curve (tangent direction)
        static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float t2 = t * t;
            float mt = 1 - t;
            float mt2 = mt * mt;
    
            return 3 * mt2 * (p1 - p0) +
                   6 * mt * t * (p2 - p1) +
                   3 * t2 * (p3 - p2);
        }
        
        static float GetDistanceAlongPenetrator(Vector3 startPos, Vector3 target, Vector3 position) {
            var startEndVector = target - startPos;
            var startVertexPosVector = position - startPos;
            
            // Project onto the forward direction to get distance along the path
            float distanceAlongPath = Vector3.Dot(startVertexPosVector, startEndVector);
			     
            // Normalize by length to get t value (0-1 range for the path, but can be negative or >1)
            return distanceAlongPath / startEndVector.sqrMagnitude;
        }
    }
}