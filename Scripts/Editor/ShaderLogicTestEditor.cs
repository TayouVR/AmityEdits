using System.Collections.Generic;
using System.Linq;
using org.Tayou.AmityEdits.EditorUtils;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits {

    
    [CustomEditor(typeof(ShaderLogicTest))]
    public class ShaderLogicTestEditor: Editor {

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(ShaderLogicTest targetObject, GizmoType gizmoType) {
            var penetratorForward = Quaternion.Euler(targetObject._StartRotation) * Vector3.up;

            // Pre-calculate the original basis vectors (Straight orientation)
            var startRot = Quaternion.Euler(targetObject._StartRotation);
            var startRight = startRot * Vector3.right;
            var startForward = startRot * Vector3.forward;
            
            // start
            var p0 = targetObject.transform.position + targetObject._StartPosition;
            var p1 = p0 + (Quaternion.Euler(targetObject._StartRotation) * new Vector3(0, targetObject._PenetratorLength * targetObject.bezierHandleSize, 0));
            
            // first orifice
            var p3 = targetObject.Orifice1Transform.position;
            var p2 = p3 + (targetObject.Orifice1Transform.rotation * new Vector3(0, targetObject._PenetratorLength * targetObject.bezierHandleSize, 0));
            var p4 = p3 + (targetObject.Orifice1Transform.rotation * new Vector3(0, -(targetObject._PenetratorLength * targetObject.bezierHandleSize), 0));
            
            // second orifice
            var p6 = targetObject.Orifice2Transform.position;
            var p5 = p6 + (targetObject.Orifice2Transform.rotation * new Vector3(0, -(targetObject._PenetratorLength * targetObject.bezierHandleSize), 0));

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