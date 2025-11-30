Shader ".Amity/Selore Reference Impl" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
//        _Glossiness ("Smoothness", Range(0,1)) = 0.5
//        _Metallic ("Metallic", Range(0,1)) = 0.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _RampTex ("Ramp Texture", 2D) = "white" {}
        
    	// Use name "Selore" for this feature
		// Penetrator Options
    	[Header(Selore Penetrator Options)]
		[Toggle] Selore_PenetratorEnabled ("Penetrator Enabled", Float) = 0
        Selore_DeformStrength ("Deform Strength", Range(0,1)) = 1
        [Vector3] Selore_StartPosition ("Start Position", Vector) = (0,0,0,0)
        Selore_StartRotation ("Start Rotation", Vector) = (0,0,0,0)
    	Selore_PenetratorLength ("Length", float) = 0.2
		[Enum(Channel 0,0,Channel 1,1)]Selore_Channel("Channel",Float) = 0
    	[Toggle] Selore_AllTheWayThrough ("All The Way Through", Float) = 0
        
        [Header(Spline Controls)]
        Selore_BezierHandleSize ("Bezier Handle Size", Range(0.05,0.5)) = 0.15
		[Toggle] Selore_SplineDebug ("Spline Debug", Float) = 0

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
	        #include "core.cginc"
	        #include "UnityCG.cginc"
	        
	        #include "globals.cginc"

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

	        v2f vert (appdata v)
	        {
	            v2f o;
	        	
	        	float4 vertexPos = v.vertex;
	        	float3 vertexNormal = v.normal;
	        	float4 color = v.color;
	        	
	        	SeloreDeform(vertexPos, vertexNormal, color);
	        	
	        	o.color = color;
				o.vertex = UnityObjectToClipPos(vertexPos);
                o.worldNormal = UnityObjectToWorldNormal(vertexNormal);
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
	        	
	        	col = lerp(col, i.color, Selore_SplineDebug);
	            return col;
	        }

	        ENDCG
		}
    }
    FallBack "Diffuse"
}
