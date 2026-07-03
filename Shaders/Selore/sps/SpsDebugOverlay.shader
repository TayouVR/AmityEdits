Shader "Hidden/Amity/SpsDebugOverlay" {
    Properties {
        [Header(Visibility Toggles)]
        [Toggle] _ShowRing("Show Ring", Float) = 1
        [Toggle] _ShowArrow("Show Arrow", Float) = 1
        [Toggle] _ShowTags("Show Tags", Float) = 1
        [Toggle] _ShowChain("Show Chain Links", Float) = 1

        [Header(Colors)]
        _HoleColor("Hole Color", Color) = (1, 0.2, 0.2, 0.9)
        _RingColor("Ring Color", Color) = (0.2, 0.5, 1, 0.9)
        _ReversibleColor("Reversible Color", Color) = (0.2, 1, 0.3, 0.9)
        _PlugColor("Plug Color", Color) = (1, 0.8, 0.2, 0.9)
        _ChainColor("Chain Color", Color) = (1, 1, 1, 0.6)
    }
    SubShader {
        Tags {
            "Queue" = "Transparent+1"
            "RenderType" = "Overlay"
            "IgnoreProjector" = "True"
        }
        GrabPass { "_VFGridFinal" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "sps_cell_layout.cginc"
            #include "sps_types.cginc"
            #include "sps_utils.cginc"
            #include "sps_id.cginc"

            SPS_INIT_TEX(_VFGridFinal)

            float _ShowRing;
            float _ShowArrow;
            float _ShowTags;
            float _ShowChain;
            float4 _HoleColor;
            float4 _RingColor;
            float4 _ReversibleColor;
            float4 _PlugColor;
            float4 _ChainColor;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = float4(v.vertex.xy, 0, 1);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            // Ring: returns 1 for pixels inside the ring outline, 0 otherwise
            float debug_ring(float2 local, float radius, float thickness) {
                float dist = length(local);
                float inner = radius - thickness;
                float outer = radius + thickness;
                return 1.0 - smoothstep(inner, radius, dist) +
                       smoothstep(radius, outer, dist);
            }

            // Arrow: returns 1 for pixels inside the arrow shape, 0 otherwise
            float debug_arrow(float2 local, float2 dir, float size) {
                float len = length(local);
                float2 norm = len > 0.001 ? local / len : float2(0, 1);
                float cosAngle = dot(norm, dir);
                cosAngle = clamp(cosAngle, -1, 1);
                float angle = acos(cosAngle);

                float headLen = size * 0.5;
                float headAngle = 0.6;
                float stemWidth = size * 0.12;

                if (len > headLen) {
                    // Arrow head (cone)
                    return angle < headAngle ? 1 : 0;
                }
                // Arrow stem
                float perpDist = abs(len * sin(angle));
                return perpDist < stemWidth ? 1 : 0;
            }

            // Dot: returns 1 for pixels inside a filled circle
            float debug_dot(float2 local, float2 center, float dotRadius) {
                float dist = distance(local, center);
                return 1.0 - smoothstep(dotRadius - 0.5, dotRadius + 0.5, dist);
            }

            // Compute color from a hash value (simple hue mapping)
            float3 debug_hash_color(uint hash) {
                float h = (hash % 360u) / 360.0;
                float s = 0.8;
                float v = 0.9;
                float3 rgb = 0;
                float hi = floor(h * 6);
                float f = h * 6 - hi;
                float p = v * (1 - s);
                float q = v * (1 - f * s);
                float t = v * (1 - (1 - f) * s);
                if (hi < 1) rgb = float3(v, t, p);
                else if (hi < 2) rgb = float3(q, v, p);
                else if (hi < 3) rgb = float3(p, v, t);
                else if (hi < 4) rgb = float3(p, q, v);
                else if (hi < 5) rgb = float3(t, p, v);
                else rgb = float3(v, p, q);
                return rgb;
            }

            fixed4 frag(v2f i) : SV_Target {
                // Compute pixel position in screen space
                float2 pixel = i.uv * _ScreenParams.xy;
                // Flip Y for Unity screen coord convention
                pixel.y = _ScreenParams.y - pixel.y;

                // Compute cell grid position
                int2 cellCoord = floor(pixel / SPS_CELL_WIDTH);
                int columns = sps_cell_grid_columns();
                int screenIndex = cellCoord.y * columns + cellCoord.x;

                if (screenIndex <= 0) clip(-1);

                // Read cell from grid
                SpsTexture gridTex = SPS_GET_TEX(_VFGridFinal);
                int2 origin = cellCoord * SPS_CELL_WIDTH;
                SpsCell cell = sps_get_cell_raw(gridTex, uint2(origin));
                if (!sps_cell_check_magic(cell)) clip(-1);

                // Read product type
                uint product = cell.read_uint(SPS_HEADER_PRODUCT_INDEX);
                if (product == 0u) clip(-1);

                // Local position within cell, centered
                float2 cellCenter = float2(SPS_CELL_WIDTH, SPS_CELL_HEIGHT) * 0.5;
                float2 local = pixel - (float2)origin - cellCenter;

                // Read cell data
                uint flags = 0;
                uint nextId = 0;
                float3 worldPos = sps_cell_header_world(cell);
                float3 forward = sps_cell_header_forward(cell);
                float3 up = sps_cell_header_up(cell);
                float scale = sps_cell_header_scale(cell);

                // Determine socket role from payload
                bool isSocket = (product == SPS_PRODUCT_SOCKET);
                bool isPlug = (product == SPS_PRODUCT_PLUG);
                if (isSocket) {
                    flags = cell.read_uint(SPS_SOCKET_PAYLOAD_FLAGS);
                    nextId = cell.read_uint(SPS_SOCKET_PAYLOAD_NEXT_ID);
                }

                // Compute ring color from role
                float4 ringColor = _PlugColor;
                if (isSocket) {
                    bool hole = (flags & SPS_SOCKET_FLAG_HOLE) != 0;
                    bool doubleSided = (flags & SPS_SOCKET_FLAG_DOUBLE_SIDED) != 0;
                    if (hole && doubleSided) ringColor = _RingColor;
                    else if (hole) ringColor = _HoleColor;
                    else if (doubleSided) ringColor = _ReversibleColor;
                    else ringColor = float4(1, 1, 1, 0.9);
                }

                // Project forward to screen-space 2D direction
                float3 viewForward = mul((float3x3)UNITY_MATRIX_V, normalize(forward));
                float2 screenDir = normalize(viewForward.xy);

                // Ring radius based on scale
                float ringRadius = min(scale * 0.5, SPS_CELL_WIDTH * 0.35);
                float ringThickness = max(1.0, ringRadius * 0.15);

                float4 result = 0;

                // Draw ring
                if (sps_to_bool(_ShowRing)) {
                    float ringAlpha = debug_ring(local, ringRadius, ringThickness);
                    result = lerp(result, ringColor, ringAlpha * ringColor.a);
                }

                // Draw direction arrow
                if (sps_to_bool(_ShowArrow) && isSocket) {
                    float arrowSize = ringRadius * 1.2;
                    float arrowAlpha = debug_arrow(local, screenDir, arrowSize);
                    result = lerp(result, ringColor * 1.5, arrowAlpha * 0.8);
                }

                // Draw tag dots
                if (sps_to_bool(_ShowTags) && isSocket) {
                    float dotRadius = max(1.5, ringRadius * 0.12);
                    int tagCount = min(SPS_SOCKET_PAYLOAD_TAG_COUNT, 8);
                    [unroll]
                    for (int ti = 0; ti < tagCount; ti++) {
                        uint tagHash = cell.read_uint(SPS_SOCKET_PAYLOAD_TAG_START + ti);
                        if (tagHash == 0u) continue;

                        float angle = (float)ti / tagCount * 6.28318;
                        float2 dotPos = float2(cos(angle), sin(angle)) * (ringRadius + dotRadius + 2);
                        float dotAlpha = debug_dot(local, dotPos, dotRadius);
                        float3 dotColor = debug_hash_color(tagHash);
                        result = lerp(result, float4(dotColor, 0.9), dotAlpha * 0.85);
                    }
                }

                // Draw chain indicator
                if (sps_to_bool(_ShowChain) && isSocket && nextId != 0u) {
                    float chainSize = ringRadius * 0.5;
                    float2 chainDir = float2(1, 0);
                    float chainAlpha = debug_arrow(local - float2(cellCenter.x * 0.35, 0), chainDir, chainSize);
                    result = lerp(result, _ChainColor, chainAlpha * _ChainColor.a);
                }

                // Ensure at least minimal alpha for readability
                if (result.a < 0.01) clip(-1);
                return result;
            }
            ENDCG
        }
    }
}
