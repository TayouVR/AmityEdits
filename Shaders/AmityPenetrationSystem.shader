Shader "Custom/AmityPenetrationSystem" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    	
        
		// Penetrator Options
    	[Header(Penetrator)]
		[Toggle] _PenetratorEnabled ("Penetrator Enabled", Float) = 0
        _DeformStrength ("Deform Strength", Range(0,1)) = 1
        _StartPosition ("Start Position", Vector) = (0,0,0,0)
        _StartRotation ("Start Rotation", Vector) = (0,0,0,0)
    	_PenetratorLength ("Length", float) = 0.2
		[Enum(Channel 0,0,Channel 1,1)]_OrificeChannel("Orifice Channel",Float) = 0 
        
    	[Header(Penetrator Legacy)]
		[Toggle(_USE_IDS)] _UseIDs("Use IDs", Float) = 0
		_ID_Orifice("ID Oriface", Float) = 0
		_ID_RingOrifice("ID Ring Oriface", Float) = 0
		_ID_Normal("ID Normal", Float) = 0
        
        [Header(Spline Controls)]
        _BezierHandleSize ("Bezier Handle Size", Float) = 0.25
        _Orifice1Position ("Orifice 1 Position", Vector) = (0,0.2,0,0)
        _Orifice1Rotation ("Orifice 1 Rotation (Euler)", Vector) = (0,0,0,0)
        _Orifice2Position ("Orifice 2 Position", Vector) = (0,0.4,0,0)
        _Orifice2Rotation ("Orifice 2 Rotation (Euler)", Vector) = (0,0,0,0)

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
	        
	        #include "utils.cginc"
	        #include "spline.cginc"

	        // Use shader model 3.0 target, to get nicer looking lighting
	        #pragma target 3.0

	        sampler2D _MainTex;

	        struct appdata
	        {
	            float4 vertex : POSITION;
	            float2 uv : TEXCOORD0;
                float4 color : COLOR;
	        };

	        struct v2f
	        {
                float4 color : COLOR;
	            float2 uv : TEXCOORD0;
	            float4 vertex : SV_POSITION;
	        };
	        
	        half _Glossiness;
	        half _Metallic;
	        fixed4 _Color;
	        
			float _UseIDs;
			float _ID_Orifice;
			float _ID_RingOrifice;
			float _ID_Normal;

			float _UseCustomPhysicsID;
			float _ID_Physics;

			float _OrificeChannel;
			
			float _PenetratorEnabled;
			float _penetratorStrength;
			float _TipLightEnabled;

			float _PenetratorLength;
	        
	        float4 _MainTex_ST;
	        float3 _StartPosition;
	        float3 _StartRotation;
	        float _DeformStrength;
            
            float _BezierHandleSize;
            float3 _Orifice1Position;
            float3 _Orifice1Rotation;
            float3 _Orifice2Position;
            float3 _Orifice2Rotation;


	        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	        // #pragma instancing_options assumeuniformscaling
	        UNITY_INSTANCING_BUFFER_START(Props)
	            // put more per-instance properties here
	        UNITY_INSTANCING_BUFFER_END(Props)
	        
	        void GetCurvePoints(out float3 p0, out float3 p1, out float3 p2, out float3 p3, out float3 p4, out float3 p5, out float3 p6) {
                // Calculate Basis vectors from Rotations
                float3x3 startMatrix = EulerToRotMatrix(_StartRotation);
                float3 startUp = mul(startMatrix, float3(0,1,0)); 
                float3 startRight = mul(startMatrix, float3(1,0,0));
                float3 startForward = mul(startMatrix, float3(0,0,1)); // Z-axis

                float3x3 o1Matrix = EulerToRotMatrix(_Orifice1Rotation);
                float3 o1Up = mul(o1Matrix, float3(0,1,0)); 

                float3x3 o2Matrix = EulerToRotMatrix(_Orifice2Rotation);
                float3 o2Up = mul(o2Matrix, float3(0,1,0)); 
	        	
                float handleLen = _PenetratorLength * _BezierHandleSize;

                // Define Control Points (P0 - P6)
                p0 = _StartPosition;
                p1 = p0 + (startUp * handleLen);
                
                p3 = _Orifice1Position;
                // Entering orifice 1
                p2 = p3 + (o1Up * handleLen);
                // Exiting orifice 1 (opposite direction to maintain smoothness)
                p4 = p3 - (o1Up * handleLen);

                p6 = _Orifice2Position;
                // Entering orifice 2
                p5 = p6 + (o2Up * handleLen); // Note: Check direction logic, usually -Up if going into it
		        
	        }
	        
	        v2f vert (appdata_full v)
	        {
	            v2f o;
	        	
                float3x3 startMatrix = EulerToRotMatrix(_StartRotation);
                float3 startUp = mul(startMatrix, float3(0,1,0)); 

	        	float3 p0;
	        	float3 p1;
	        	float3 p2;
	        	float3 p3;
	        	float3 p4;
	        	float3 p5;
	        	float3 p6;
	        	GetCurvePoints(p0, p1, p2, p3, p4, p5, p6);

	        	
                // 3. Calculate Distance along mesh spine (Meters)
                // Project vector (vertex - start) onto the StartUp vector
                float distanceAlongSpine = dot(v.vertex.xyz - _StartPosition, startUp);
	        	float currentPosMeters = GetDistanceAlongPath(_StartPosition, _StartRotation, v.vertex);

                // 4. Calculate Curve Lengths for logic switching
                float len1 = CalculateArcLength(p0, p1, p2, p3, 1);
                float len2 = CalculateArcLength(p3, p4, p5, p6, 1);
                float totalLen = len1 + len2;
	        	
                // 5. Determine Spline Position and Tangent
                float3 splinePos;
                float3 splineTangent;
                float visualT = 0; // For debugging color


                // 3. CALCULATE SPLINE POSITION (Linear Extension vs Bezier)
                if (currentPosMeters > totalLen) {
                    // --- LINEAR EXTENSION MODE ---
                    // Calculate end of curve 2
                    float3 endTangent = normalize(CubicBezierTangent(p3, p4, p5, p6, 1.0));
                    float3 endPos = CubicBezier(p3, p4, p5, p6, 1.0);
                    
                    float excessDistance = currentPosMeters - totalLen;
                    
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

                // 6. Basis Transformation (Deform Logic)
                
                // A. Find the point on the original straight spine
                float3 pointOnStraightSpine = _StartPosition + (startUp * distanceAlongSpine);
                
                // B. Find the offset of the vertex from that spine
                float3 offsetFromSpine = v.vertex.xyz - pointOnStraightSpine;

                // C. Create rotation that aligns Original Up to New Tangent
                float3x3 rotationMatrix = FromToRotation(startUp, splineTangent);

                // D. Apply rotation to the offset
                float3 rotatedOffset = mul(rotationMatrix, offsetFromSpine);

                // E. Final Result
                float3 deformedPosition = splinePos + rotatedOffset;
	        	
	        	float distanceAlongPenetrator = GetDistanceAlongPenetrator(_StartPosition, _StartPosition + (startUp * _PenetratorLength), v.vertex);

                float lerpFactor =
                    clamp((len1 - _PenetratorLength * 1.5f) * 5, 0, 1) +
                    (distanceAlongPenetrator <= 0 ? 1 : 0);
	        	
                // Blend strength
                deformedPosition = lerp(deformedPosition, v.vertex.xyz, lerpFactor);
                deformedPosition = lerp(v.vertex.xyz, deformedPosition, _DeformStrength);
            
                o.vertex = UnityObjectToClipPos(float4(deformedPosition, 1));
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // Debug visualization: Color gradient based on T
                float4 gizmoColor = float4(distanceAlongPenetrator < 0 ? 1 : 0, visualT > 2 ? 1 : 0,
                    distanceAlongPenetrator, 1);
                o.color = gizmoColor; //float4(visualT, 1-visualT, visualT > 2 ? 1 : 0, 1);
	            
	        	
                return o;
	        }

	        
	        fixed4 frag (v2f i) : SV_Target
	        {
	            fixed4 col = tex2D(_MainTex, i.uv) * _Color;
	            return i.color; //col;
	        }

	        ENDCG
		}
    }
    FallBack "Diffuse"
}