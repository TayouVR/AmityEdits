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
    public class ShaderLogicTestEditor : Editor {
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
        
        // Determine the role of a light based on its range and the selected channel
        // Ported from lights.cginc
        private static int GetLightRole(float range, SeloreChannel channel) {
            float dec = range % 0.1f;
            float eps = 0.004f; // Tolerance for float comparison

            // Target decimal values based on channel
            // Channel 0: Hole .01, Ring .02, Normal .05
            // Channel 1: Hole .03, Ring .04, Normal .06
            float targetHole = (channel == 0) ? 0.01f : 0.03f;
            float targetRing = (channel == 0) ? 0.02f : 0.04f;
            float targetNormal = (channel == 0) ? 0.05f : 0.06f;

            if (Math.Abs(dec - targetHole) < eps) return SELORE_LIGHT_ROLE_HOLE;
            if (Math.Abs(dec - targetNormal) < eps) return SELORE_LIGHT_ROLE_NORMAL;

            // Ring Check (includes subtype logic)
            // Check basic ring ID
            if (Math.Abs(dec - targetRing) < eps) return SELORE_LIGHT_ROLE_RING_TWOWAY;

            // Check one-way ring ID (base + 0.005, e.g., #.#25)
            if (Math.Abs(dec - (targetRing + 0.005f)) < eps) return SELORE_LIGHT_ROLE_RING_ONEWAY;

            return SELORE_LIGHT_ROLE_UNKNOWN;
        }

        private static float Map(float value, float min1, float max1, float min2, float max2) {
            if (Mathf.Approximately(max1 - min1, 0f)) {
                return min2;
            }

            return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
        }

        private static float SaturatedMap(float value, float min, float max) {
            return Mathf.Clamp01(Map(value, min, max, 0f, 1f));
        }

        private static Vector3 SafeNormalize(Vector3 value) {
            return value.sqrMagnitude <= Mathf.Epsilon ? Vector3.forward : value.normalized;
        }

        private static float AngleBetween(Vector3 a, Vector3 b) {
            float dot = Mathf.Clamp(Vector3.Dot(SafeNormalize(a), SafeNormalize(b)), -1f, 1f);
            return Mathf.Acos(dot);
        }

        private static Vector3 NearestNormal(Vector3 forward, Vector3 approximate) {
            Vector3 result = Vector3.Cross(forward, Vector3.Cross(approximate, forward));
            return SafeNormalize(result);
        }

        private static Quaternion EulerToRotation(Vector3 euler) {
            Quaternion rotX = Quaternion.AngleAxis(euler.x, Vector3.right);
            Quaternion rotY = Quaternion.AngleAxis(euler.y, Vector3.up);
            Quaternion rotZ = Quaternion.AngleAxis(euler.z, Vector3.forward);
            return rotZ * rotY * rotX;
        }

        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float minT = 1f - t;
            return
                minT * minT * minT * p0 +
                3f * minT * minT * t * p1 +
                3f * minT * t * t * p2 +
                t * t * t * p3;
        }

        private static Vector3 BezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
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

        private struct Selore_LightInfo {
            public Vector3 position;
            public int role;
        };

        // Scans all lights and categorizes them
        // Ported from lights.cginc
        private static void ScanLights(List<Light> rawLights, SeloreChannel channel, out List<Selore_LightInfo> lights) {
            lights = new List<Selore_LightInfo>();
            if (rawLights == null) return;

            foreach (var light in rawLights) {
                if (light == null) continue;
                if (!light.gameObject.activeSelf) continue;
                
                // Note: Shader checks color brightness, here we assume lights in the list are valid for processing
                float range = light.range;
                int role = GetLightRole(range, channel);

                if (role != SELORE_LIGHT_ROLE_UNKNOWN) {
                    lights.Add(new Selore_LightInfo {
                        position = light.transform.position,
                        role = role
                    });
                }
            }
        }

        // Main logic: Finds up to two valid orifices by pairing holes/rings with normal lights
        // Ported from lights.cginc
        private static void GetOrifices(
            SeloreChannel channel,
            Vector3 startPos,
            List<Light> rawLights,
            out Selore_OrificeData o1,
            out Selore_OrificeData o2
        ) {
            ScanLights(rawLights, channel, out List<Selore_LightInfo> lights);

            // Initialize defaults
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

            int foundCount = 0;
            float maxDist = 0.1f; // Maximum distance between orifice light and normal light

            for (int i = 0; i < lights.Count; i++) {
                int role = lights[i].role;

                // If we found a potential orifice light
                if (role == SELORE_LIGHT_ROLE_HOLE || role == SELORE_LIGHT_ROLE_RING_TWOWAY ||
                    role == SELORE_LIGHT_ROLE_RING_ONEWAY) {
                    // Search for a nearby Normal light to pair with
                    int bestNormal = -1;
                    float minDist = 100.0f;

                    for (int j = 0; j < lights.Count; j++) {
                        if (lights[j].role == SELORE_LIGHT_ROLE_NORMAL) {
                            float d = Vector3.Distance(lights[i].position, lights[j].position);
                            if (d < maxDist && d < minDist) {
                                minDist = d;
                                bestNormal = j;
                            }
                        }
                    }

                    // If a pair was found
                    if (bestNormal != -1) {
                        Selore_OrificeData res = new Selore_OrificeData {
                            isValid = true,
                            type = role,
                            position = lights[i].position,
                            normalLightPosition = lights[bestNormal].position
                        };
                        // Calculate direction: From Orifice Position -> Normal Position
                        res.normal = SafeNormalize(res.normalLightPosition - res.position);

                        if (role == SELORE_LIGHT_ROLE_RING_TWOWAY) {
                            Vector3 toStart = SafeNormalize(startPos - res.position);
                            if (Vector3.Dot(res.normal, toStart) < 0.0f)
                                res.normal = -res.normal;
                        }


                        if (foundCount == 0) {
                            o1 = res;
                            foundCount++;
                        } else if (foundCount == 1) {
                            o2 = res;
                            foundCount++;
                        }
                    }
                }

                if (foundCount >= 2) break;
            }

            // Sort based on distance to startPos
            if (o1.isValid && o2.isValid) {
                if (Vector3.Distance(startPos, o1.position) > Vector3.Distance(startPos, o2.position)) {
                    Selore_OrificeData temp = o1;
                    o1 = o2;
                    o2 = temp;
                }
            }
            else if (!o1.isValid && o2.isValid) {
                // Fallback safety: if only O2 was found (unlikely with current logic), move it to O1
                o1 = o2;
                o2.isValid = false;
            }

            if (o2.type == SELORE_LIGHT_ROLE_RING_TWOWAY) {
                Vector3 toStart = SafeNormalize(startPos - o2.position);
                if (Vector3.Dot(o2.normal, toStart) > 0.0f)
                    o2.normal = -o2.normal;
                
            }
        }

        // Calculate the rotation matrix that aligns vector 'from' to vector 'to'
        // Ported from utils.cginc
        private static Matrix4x4 FromToRotationMatrix(Vector3 fromVec, Vector3 toVec) {
            Vector3 v = Vector3.Cross(fromVec, toVec);
            float e = Vector3.Dot(fromVec, toVec);

            if (e > 0.999999f) return Matrix4x4.identity; // Identity if parallel
            if (e < -0.999999f) return Matrix4x4.identity; // Identity if opposite (simplified)

            float h = 1.0f / (1.0f + e);

            Matrix4x4 result = Matrix4x4.zero;
            result.m00 = e + h * v.x * v.x;
            result.m01 = h * v.x * v.y - v.z;
            result.m02 = h * v.x * v.z + v.y;
            result.m10 = h * v.x * v.y + v.z;
            result.m11 = e + h * v.y * v.y;
            result.m12 = h * v.y * v.z - v.x;
            result.m20 = h * v.x * v.z - v.y;
            result.m21 = h * v.y * v.z + v.x;
            result.m22 = e + h * v.z * v.z;
            result.m33 = 1.0f;
            return result;
        }

        // Ported from spline.cginc
        private static float GetDistanceAlongPath(Vector3 startPos, Vector3 startRot, Vector3 position) {
            Matrix4x4 rotMatrix = EulerToRotMatrix(startRot);
            Vector3 forward = rotMatrix.GetColumn(1); // Y-Axis
            Vector3 toVertex = position - startPos;
            return Vector3.Dot(toVertex, forward);
        }

        private static Vector3 GetSplinePosition(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5,
            Vector3 p6, float t) {
            if (t < 1.0f) {
                return CubicBezier(p0, p1, p2, p3, t);
            }
            return CubicBezier(p3, p4, p5, p6, t - 1.0f);
        }

        private static Vector3 GetSplineTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5,
            Vector3 p6, float t) {
            if (t < 1.0f) {
                return CubicBezierTangent(p0, p1, p2, p3, t);
            }
            return CubicBezierTangent(p3, p4, p5, p6, Mathf.Max(0, t - 1.0f));
        }

        private static float GetDistanceAlongLength(Vector3 startPos, Vector3 target, Vector3 position) {
            Vector3 startEndVector = target - startPos;
            Vector3 startVertexPosVector = position - startPos;

            // Project onto the forward direction to get distance along the path
            float distanceAlongPath = Vector3.Dot(startVertexPosVector, startEndVector);

            // Normalize by length to get t value (0-1 range for the path, but can be negative or >1)
            return distanceAlongPath / Vector3.Dot(startEndVector, startEndVector);
        }

        private static void GetCurvePoints(
            out Vector3 p0, out Vector3 p1, out Vector3 p2,
            out Vector3 p3, out Vector3 p4, out Vector3 p5,
            out Vector3 p6, Selore_OrificeData o1, Selore_OrificeData o2,
            Vector3 startPosition, Vector3 startRotation, float penetratorLength, float bezierHandleSize
        ) {
            // Calculate Basis vectors from Rotations
            Matrix4x4 startMatrix = EulerToRotMatrix(startRotation);
            Vector3 startUp = startMatrix.GetColumn(1); // Y-axis

            Vector3 o1Up = o1.normal;
            Vector3 o2Up = -o2.normal;

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
            p5 = p6 + (o2Up * handleLen2);
        }

        private static Matrix4x4 EulerToRotMatrix(Vector3 euler) {
            float x = euler.x * Mathf.Deg2Rad;
            float y = euler.y * Mathf.Deg2Rad;
            float z = euler.z * Mathf.Deg2Rad;

            float sx = Mathf.Sin(x);
            float cx = Mathf.Cos(x);
            float sy = Mathf.Sin(y);
            float cy = Mathf.Cos(y);
            float sz = Mathf.Sin(z);
            float cz = Mathf.Cos(z);

            // HLSL: float3x3(1,0,0, 0,cx,-sx, 0,sx,cx)
            Matrix4x4 rotX = Matrix4x4.identity;
            rotX.m11 = cx;
            rotX.m12 = -sx;
            rotX.m21 = sx;
            rotX.m22 = cx;

            // HLSL: float3x3(cy,0,sy, 0,1,0, -sy,0,cy)
            Matrix4x4 rotY = Matrix4x4.identity;
            rotY.m00 = cy;
            rotY.m02 = sy;
            rotY.m20 = -sy;
            rotY.m22 = cy;

            // HLSL: float3x3(cz,-sz,0, sz,cz,0, 0,0,1)
            Matrix4x4 rotZ = Matrix4x4.identity;
            rotZ.m00 = cz;
            rotZ.m01 = -sz;
            rotZ.m10 = sz;
            rotZ.m11 = cz;

            return rotZ * rotY * rotX;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo2(ShaderLogicTest targetObject, GizmoType gizmoType) {
            Vector3 worldStartPosition = targetObject.transform.TransformPoint(targetObject._StartPosition);

            GetOrifices(
                targetObject._OrificeChannel,
                worldStartPosition,
                targetObject.lights,
                out var o1,
                out var o2
            );

            if (o1.isValid) {
                Matrix4x4 startMatrix = EulerToRotMatrix(targetObject._StartRotation);
                Vector3 startUp = startMatrix.GetColumn(1); // mul(startMatrix, float3(0,1,0)); 

                GetCurvePoints(
                    out var p0, out var p1, 
                    out var p2, out var p3, 
                    out var p4, out var p5, 
                    out var p6, o1, o2,
                    worldStartPosition, targetObject._StartRotation, targetObject._PenetratorLength,
                    targetObject.bezierHandleSize);

                float goForTwoOrifices =
                    Mathf.Min(targetObject._AllTheWayThrough ? 1.0f : 0f, o2.isValid ? 1.0f : 0.0f);

                // Calculate Curve Lengths for logic switching
                float len1 = CalculateArcLength(p0, p1, p2, p3, 1);
                float len2 = CalculateArcLength(p3, p4, p5, p6, 1);
                float totalLen = len1 + len2;
                float lenToWorkWith = Mathf.Lerp(len1, totalLen, goForTwoOrifices);

                List<Vector3> vertices;
                bool meshIsUsed = targetObject.debugMesh != null && targetObject.useDebugMesh;
                if (meshIsUsed) {
                    vertices = targetObject.debugMesh.vertices.Where((_, i) => i % 10 == 0).ToList();
                } else {
                    vertices = new List<Vector3>();
                    for (int i = 0; i < targetObject.sampleCount; i++) {
                        vertices.Add(worldStartPosition + (startUp *
                                                           (targetObject._PenetratorLength * i /
                                                            targetObject.sampleCount * 1.5f -
                                                            targetObject._PenetratorLength * 0.25f)));
                    }
                }

                Vector3 penetratorTip = worldStartPosition + (startUp * targetObject._PenetratorLength);

                foreach (var vertexPos in vertices) {
                    Vector3
                        vertexNormal =
                            Vector3.up; // We don't have vertex normals here, so we assume up for visualization

                    // Port of SeloreDeform logic starts here

                    // Calculate Distance along mesh spine (Meters)
                    float distanceAlongSpine = Vector3.Dot(vertexPos - worldStartPosition, startUp);
                    float currentPosMeters =
                        GetDistanceAlongPath(worldStartPosition, targetObject._StartRotation, vertexPos);

                    // Determine Spline Position and Tangent
                    Vector3 splinePos;
                    Vector3 splineTangent;
                    float visualT = 0; // For debugging color

                    // CALCULATE SPLINE POSITION (Linear Extension vs Bezier)
                    if (currentPosMeters > lenToWorkWith) {
                        Vector3 endPos;
                        Vector3 endTangent;

                        if (goForTwoOrifices < 0.5f) {
                            endPos = p3;
                            endTangent = CubicBezierTangent(p0, p1, p2, p3, 1.0f).normalized;
                        } else {
                            endPos = p6;
                            endTangent = CubicBezierTangent(p3, p4, p5, p6, 1.0f).normalized;
                        }

                        float excessDistance = currentPosMeters - lenToWorkWith;
                        splinePos = endPos + endTangent * excessDistance;
                        splineTangent = endTangent;
                        visualT = 2.5f;
                    } else {
                        float t;
                        if (currentPosMeters < len1) {
                            t = currentPosMeters / Mathf.Max(0.001f, len1);
                        } else {
                            t = 1.0f + ((currentPosMeters - len1) / Mathf.Max(0.001f, len2));
                        }

                        visualT = t;
                        splinePos = GetSplinePosition(p0, p1, p2, p3, p4, p5, p6, t);
                        splineTangent = GetSplineTangent(p0, p1, p2, p3, p4, p5, p6, t).normalized;
                    }

                    // Basis Transformation (Deform Logic)
                    Vector3 pointOnStraightSpine = worldStartPosition + (startUp * distanceAlongSpine);
                    Vector3 offsetFromSpine = vertexPos - pointOnStraightSpine;

                    Matrix4x4 rotationMatrix = FromToRotationMatrix(startUp, splineTangent);
                    Vector3 rotatedOffset = rotationMatrix.MultiplyVector(offsetFromSpine);
                    Vector3 deformedNormal = rotationMatrix.MultiplyVector(vertexNormal);

                    Vector3 deformedPosition = splinePos + rotatedOffset;

                    float distanceAlongPenetrator =
                        GetDistanceAlongLength(worldStartPosition, penetratorTip, vertexPos);

                    // Blend strength
                    float blend1 = Mathf.Clamp((len1 - targetObject._PenetratorLength * 1.5f) * 5, 0, 1);
                    float blend2 = distanceAlongPenetrator <= 0 ? 1 : 0;

                    deformedPosition = Vector3.Lerp(deformedPosition, vertexPos, blend1);
                    deformedNormal = Vector3.Lerp(deformedNormal, vertexNormal, blend1);

                    deformedPosition = Vector3.Lerp(deformedPosition, vertexPos, blend2);
                    deformedNormal = Vector3.Lerp(deformedNormal, vertexNormal, blend2);

                    deformedPosition = Vector3.Lerp(vertexPos, deformedPosition, targetObject._DeformStrength);
                    deformedNormal = Vector3.Lerp(vertexNormal, deformedNormal, targetObject._DeformStrength);

                    bool isHidden = o1.type == SELORE_LIGHT_ROLE_HOLE && (visualT > 1 && (!o2.isValid || visualT < 2));

                    // Debug visualization
                    Color gizmoColor = new Color(o1.isValid ? 1.0f : 0.0f, o2.isValid ? 1.0f : 0.0f,
                        distanceAlongPenetrator, 1);
                    if (!isHidden) {
                        if (meshIsUsed) {
                            GizmoUtils.DrawArrow(deformedPosition, deformedPosition + deformedNormal * 0.01f,
                                gizmoColor);
                            GizmoUtils.DrawSphere(deformedPosition, 0.005f, gizmoColor);
                        } else {
                            GizmoUtils.DrawDisc(deformedPosition, splineTangent, 0.01f, gizmoColor);
                        }
                    }
                }

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

                GizmoUtils.DrawArrow(
                    p3,
                    p2,
                    new Color(0.5f, 1f, 0)
                );

                GizmoUtils.DrawArrow(
                    p6,
                    p5,
                    new Color(1f, 0f, 0.5f)
                );
                GizmoUtils.DrawArrow(
                    p6,
                    p6 - (p5 - p6).normalized * 0.1f,
                    new Color(0.5f, 1f, 0)
                );
                GizmoUtils.DrawArrow(
                    p3,
                    p4,
                    new Color(1f, 0f, 0.5f)
                );
            }

            // Draw original curve for reference
            GizmoUtils.DrawArrow(worldStartPosition,
                worldStartPosition + (Quaternion.Euler(targetObject._StartRotation) * Vector3.up * 0.1f), Color.blue);
        }

        // --- OLD HELPER METHODS KEPT FOR REFERENCE OR CLEANUP LATER ---
        // Some were messy, porting only necessary logic.

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
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

        private static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float t2 = t * t;
            float mt = 1 - t;
            float mt2 = mt * mt;

            return 3 * mt2 * (p1 - p0) +
                   6 * mt * t * (p2 - p1) +
                   3 * t2 * (p3 - p2);
        }

        private static float CalculateArcLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetT,
            int samples = 50) {
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
    }
}