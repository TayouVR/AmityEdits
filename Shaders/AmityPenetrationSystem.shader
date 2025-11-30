Shader "Custom/AmityPenetrationSystem" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _RampTex ("Ramp Texture", 2D) = "white" {}
    	
        
		// Penetrator Options
    	[Header(Penetrator)]
		[Toggle] _PenetratorEnabled ("Penetrator Enabled", Float) = 0
        _DeformStrength ("Deform Strength", Range(0,1)) = 1
        _StartPosition ("Start Position", Vector) = (0,0,0,0)
        _StartRotation ("Start Rotation", Vector) = (0,0,0,0)
    	_PenetratorLength ("Length", float) = 0.2
		[Enum(Channel 0,0,Channel 1,1)]_OrificeChannel("Orifice Channel",Float) = 0
    	[Toggle] _AllTheWayThrough ("All The Way Through", Float) = 0
        
        [Header(Spline Controls)]
        _BezierHandleSize ("Bezier Handle Size", Range(0.05,0.5)) = 0.15
		[Toggle] _SplineDebug ("Spline Debug", Float) = 0

    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        LOD 200
        Cull [_Cull]

        Pass {

	        CGPROGRAM
	        // Physically based Standard lighting model, and enable shadows on all light types
	        #pragma vertex vert
	        #pragma fragment frag
	        #include "UnityCG.cginc"
	        
	        #include "globals.cginc"
	        #include "utils.cginc"
	        #include "spline.cginc"
	        #include "lights.cginc"

	        // Use shader model 3.0 target, to get nicer looking lighting
	        #pragma target 3.0

	        sampler2D _MainTex;
            sampler2D _RampTex;
	        fixed4 _Color;
			float4 _MainTex_ST;

	        struct appdata
	        {
	            float4 vertex : POSITION;
	        	float3 normal : NORMAL;
	            float2 uv : TEXCOORD0;
                float4 color : COLOR;
				uint id : SV_VERTEXID;
	        };

	        struct v2f
	        {
	            float4 vertex : SV_POSITION;
	            float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
	        };
	        

	        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	        // #pragma instancing_options assumeuniformscaling
	        UNITY_INSTANCING_BUFFER_START(Props)
	            // put more per-instance properties here
	        UNITY_INSTANCING_BUFFER_END(Props)
	        
	        void GetCurvePoints(
	        	out float3 p0, out float3 p1, out float3 p2, 
	        	out float3 p3, out float3 p4, out float3 p5, 
	        	out float3 p6, OrificeData o1, OrificeData o2
	        	) {
                // Calculate Basis vectors from Rotations
                float3x3 startMatrix = EulerToRotMatrix(_StartRotation);
                float3 startUp = mul(startMatrix, float3(0,1,0)); 
                float3 startRight = mul(startMatrix, float3(1,0,0));
                float3 startForward = mul(startMatrix, float3(0,0,1)); // Z-axis

                float3 o1Up = o1.normal; 
                float3 o2Up = -o2.normal; 
	        	
	        	// TODO: make handle length dynamic based on distance between points
                float handleLen = _PenetratorLength * _BezierHandleSize;
                float handleLen1 = distance(_StartPosition, o1.position) * _BezierHandleSize;
                float handleLen2 = distance(o1.position, o2.position) * _BezierHandleSize;

                // Define Control Points (P0 - P6)
                p0 = _StartPosition;
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
	        
	        v2f vert (appdata v)
	        {
	            v2f o;
	            
	        	float3 worldStartPosition = mul(unity_ObjectToWorld, _StartPosition);
	        	
	        	OrificeData o1;
	        	OrificeData o2;
	        	GetOrifices(_OrificeChannel, worldStartPosition, o1, o2);
	        	
	        	if (o1.isValid) {
	                float3x3 startMatrix = EulerToRotMatrix(_StartRotation);
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
	                float distanceAlongSpine = dot(v.vertex.xyz - _StartPosition, startUp);
	        		float currentPosMeters = GetDistanceAlongPath(_StartPosition, _StartRotation, v.vertex);

	                // Calculate Curve Lengths for logic switching
	                float len1 = CalculateArcLength(p0, p1, p2, p3, 1);
	                float len2 = CalculateArcLength(p3, p4, p5, p6, 1);
	                float totalLen = len1 + len2;
	        		float lenToWorkWith = lerp(len1, totalLen, _AllTheWayThrough);
	        		
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
	                    if (_AllTheWayThrough < 0.5) {
							endTangent = normalize(CubicBezierTangent(p0, p1, p2, p3, 1.0));
							endPos = CubicBezier(p0, p1, p2, p3, 1.0);
	                    }
	                    
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
	                
	                // Find the point on the original straight spine
	                float3 pointOnStraightSpine = _StartPosition + (startUp * distanceAlongSpine);
	                
	                // Find the offset of the vertex from that spine
	                float3 offsetFromSpine = v.vertex.xyz - pointOnStraightSpine;

	                // Create rotation that aligns Original Up to New Tangent
	                float3x3 rotationMatrix = FromToRotation(startUp, splineTangent);

	                // Apply rotation to the offset
	                float3 rotatedOffset = mul(rotationMatrix, offsetFromSpine);
	        		
                    // Rotate the normal using the same matrix
                    float3 deformedNormal = mul(rotationMatrix, v.normal);

	                // Final Result
	                float3 deformedPosition = splinePos + rotatedOffset;
	        		
	        		float distanceAlongPenetrator = GetDistanceAlongLength(_StartPosition, _StartPosition + (startUp * _PenetratorLength), v.vertex);
	        		
	                // Blend strength
                    float blend1 = clamp((len1 - _PenetratorLength * 1.5f) * 5, 0, 1);
                    float blend2 = distanceAlongPenetrator <= 0 ? 1 : 0;

	                deformedPosition = lerp(deformedPosition, v.vertex.xyz, blend1);
                    deformedNormal = lerp(deformedNormal, v.normal, blend1);
                    
	                deformedPosition = lerp(deformedPosition, v.vertex.xyz, blend2);
                    deformedNormal = lerp(deformedNormal, v.normal, blend2);
	        		
	                deformedPosition = lerp(v.vertex.xyz, deformedPosition, _DeformStrength);
                    deformedNormal = lerp(v.normal, deformedNormal, _DeformStrength);
	        		
	        		// hide section between point 1 and 2, as its inside body and supposed to not be visible.
	        		if (o1.type == LIGHT_ROLE_HOLE && (visualT > 1 && visualT < 2)) {
		                float nan = 0.0 / 0.0;
		                float4 nanPosition = float4(nan, nan, nan, nan);
	        			deformedPosition = nanPosition;
	        		}
	            
	                o.vertex = UnityObjectToClipPos(float4(deformedPosition, 1));
	        		
	                // Debug visualization: Color gradient based on T
	                float4 gizmoColor = float4(distanceAlongPenetrator < 0 ? 1 : 0, visualT > 2 ? 1 : 0, distanceAlongPenetrator, 1);
	                o.color = gizmoColor;
                    o.worldNormal = UnityObjectToWorldNormal(deformedNormal);
	        	} else {
					o.vertex = UnityObjectToClipPos(v.vertex);
                    o.worldNormal = UnityObjectToWorldNormal(v.normal);
	        	}
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
	        	
                return o;
	        }

	        
	        fixed4 frag (v2f i) : SV_Target
	        {
	            fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Calculate Half-Lambert (ranges from 0 to 1 instead of -1 to 1)
                // This allows the Ramp Texture to control the shadow falloff completely
                float ndotl = dot(normal, lightDir) * 0.5 + 0.5;
                
                // Sample the ramp texture
                fixed3 ramp = tex2D(_RampTex, float2(ndotl, 0.5)).rgb;
                
                col.rgb *= ramp;
	        	
	        	col = lerp(col, i.color, _SplineDebug);
	            return col;
	        }

	        ENDCG
		}
    }
    FallBack "Diffuse"
}
