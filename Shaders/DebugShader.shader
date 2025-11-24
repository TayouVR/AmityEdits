Shader "Custom/SplineDebugVisualizer"
{
    Properties
    {
        _StartPosition ("Start Position", Vector) = (0,0,0,0)
        _StartRotation ("Start Rotation", Vector) = (0,0,0,0)
        _AxisLength ("Axis Length", Float) = 1.0
        _LineWidth ("Line Width", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+100" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            float3 _StartPosition;
            float3 _StartRotation;
            float3 _EndPosition;
            float3 _EndRotation;
            float _AxisLength;
            float _LineWidth;
            
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

			uniform float _Length;

			void GetBestLights( float Channel, inout int orificeType, inout float3 orificePositionTracker, inout float3 orificeNormalTracker, inout float3 penetratorPositionTracker, inout float penetratorLength ) {
				float ID = step( 0.5 , Channel );
				float baseID = ( ID * 0.02 );
				float holeID = ( baseID + 0.01 );
				float ringID = ( baseID + 0.02 );
				float normalID = ( 0.05 + ( ID * 0.01 ) );
				float penetratorID = ( 0.09 + ( ID * -0.01 ) );
				
				float modulusMask = 0.1;
				
				UNITY_BRANCH
				if (_UseCustomPhysicsID)
				{
					penetratorID = _ID_Physics;
				}
				
				UNITY_BRANCH
				if (_UseIDs)
				{
					modulusMask = 10;
					holeID = _ID_Orifice;
					ringID = _ID_RingOrifice;
					normalID = _ID_Normal;
				}
				
				float4 orificeWorld;
				float4 orificeNormalWorld;
				float4 penetratorWorld;
				for (int i=0;i<4;i++) {
					float range = (0.005 * sqrt(1000000 - unity_4LightAtten0[i])) / sqrt(unity_4LightAtten0[i]);
					if (length(unity_LightColor[i].rgb) < 0.01) {
						if (abs(fmod(range,modulusMask)-holeID)<0.005) {
							orificeType=0;
							orificeWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
							orificePositionTracker = mul( unity_WorldToObject, orificeWorld ).xyz;
						}
						if (abs(fmod(range,modulusMask)-ringID)<0.005) {
							orificeType=1;
							orificeWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
							orificePositionTracker = mul( unity_WorldToObject, orificeWorld ).xyz;
						}
						if (abs(fmod(range,modulusMask)-normalID)<0.005) {
							orificeNormalWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
							orificeNormalTracker = mul( unity_WorldToObject, orificeNormalWorld ).xyz;
						}
						if (abs(fmod(range,modulusMask)-penetratorID)<0.005 && _TipLightEnabled > 0.5) {
							penetratorWorld = float4(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i], 1);
							float3 tempPenetratorPositionTracker = mul( unity_WorldToObject, penetratorWorld ).xyz;
							if (length(penetratorPositionTracker)>length(tempPenetratorPositionTracker)) {
								penetratorPositionTracker = tempPenetratorPositionTracker;
								penetratorLength=unity_LightColor[i].a;
							}
						}
					}
				}
			}

            // Helper function to create rotation matrix from euler angles
            float3x3 EulerToRotMatrix(float3 euler)
            {
                float3 sinXYZ = sin(euler);
                float3 cosXYZ = cos(euler);

                float3x3 rotX = float3x3(
                    1, 0, 0,
                    0, cosXYZ.x, -sinXYZ.x,
                    0, sinXYZ.x, cosXYZ.x
                );

                float3x3 rotY = float3x3(
                    cosXYZ.y, 0, sinXYZ.y,
                    0, 1, 0,
                    -sinXYZ.y, 0, cosXYZ.y
                );

                float3x3 rotZ = float3x3(
                    cosXYZ.z, -sinXYZ.z, 0,
                    sinXYZ.z, cosXYZ.z, 0,
                    0, 0, 1
                );

                return mul(mul(rotZ, rotY), rotX);
            }

            appdata vert(appdata v)
            {
                return v;
            }

            // Function to create a line from point a to b
            void CreateLine(float3 a, float3 b, float4 color, inout TriangleStream<v2f> triStream)
            {
                float3 forward = normalize(b - a);
                float3 right = normalize(cross(forward, float3(0,1,0)));
                float3 up = cross(forward, right);

                right *= _LineWidth * 0.5;

                v2f v[4];
                
                v[0].pos = UnityObjectToClipPos(float4(a - right, 1));
                v[1].pos = UnityObjectToClipPos(float4(a + right, 1));
                v[2].pos = UnityObjectToClipPos(float4(b - right, 1));
                v[3].pos = UnityObjectToClipPos(float4(b + right, 1));

                [unroll]
                for(int i = 0; i < 4; i++)
                {
                    v[i].color = color;
                }

                triStream.Append(v[0]);
                triStream.Append(v[1]);
                triStream.Append(v[2]);
                triStream.Append(v[3]);
                triStream.RestartStrip();
            }

            // Draw coordinate axes at a position with rotation
            void DrawAxes(float3 position, float3x3 rotMatrix, inout TriangleStream<v2f> triStream)
            {
                
                // X axis (Red)
                float3 xAxis = mul(rotMatrix, float3(_AxisLength, 0, 0));
                CreateLine(position, position + xAxis, float4(1,0,0,1), triStream);
                
                // Y axis (Green)
                float3 yAxis = mul(rotMatrix, float3(0, _AxisLength, 0));
                CreateLine(position, position - yAxis, float4(0,1,0,1), triStream);
                
                // Z axis (Blue)
                float3 zAxis = mul(rotMatrix, float3(0, 0, _AxisLength));
                CreateLine(position, position + zAxis, float4(0,0,1,1), triStream);
            }

            [maxvertexcount(24)]
            void geom(point appdata input[1], inout TriangleStream<v2f> triStream)
            {
				float orificeType = 0;
				float3 orificePositionTracker = float3(0,0,100);
				float3 orificeNormalTracker = float3(0,0,99);
				float3 penetratorPositionTracker = float3(0,0,1); // unused
				float3 penetratorNormalTracker = float3(0,0,1); // unused
				float pl=0;
				GetBestLights(_OrificeChannel, orificeType, orificePositionTracker, orificeNormalTracker, penetratorNormalTracker, pl);
	        	
			    float3 direction = normalize(orificePositionTracker - orificeNormalTracker);
			    float3 up = float3(0, 1, 0);
			    float3 right = normalize(cross(direction, up));
			    up = normalize(cross(right, direction));
			    
			    float3x3 rotMatrix = float3x3(
			        up,
			        direction,
			        right
			    );
	        	
                // Draw start position axes
                DrawAxes(_StartPosition, EulerToRotMatrix(_StartRotation), triStream);
                
                // Draw end position axes
                DrawAxes(orificePositionTracker, transpose(rotMatrix), triStream);
                
                // Draw connection line between start and end (White)
                CreateLine(_StartPosition, orificePositionTracker, float4(1,1,1,1), triStream);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}