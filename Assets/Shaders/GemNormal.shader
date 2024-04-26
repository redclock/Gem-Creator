Shader "GemCreator/GemNormal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
       
        Cull Off ZWrite Off ZTest Always
        LOD 100
        
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "PreviewType" = "Quad"
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            #define MAX_EDGES 10
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 pos : TEXCOORD0;
            };
            
            float4 _FaceNormal;
            float4 _EdgePoints[MAX_EDGES];
            float4 _EdgeNormals[MAX_EDGES];
            int _EdgeCount;
            float _SmoothDistance;
            float _SmoothPower;
            

            float getPointDistanceToSegment(float2 p, float2 segLineStart, float2 segLineEnd)
            {
                float2 v = segLineEnd - segLineStart;
                float2 w = p - segLineStart;
                float c1 = dot(w, v);
                if (c1 <= 0)
                    return distance(p, segLineStart);
                float c2 = dot(v, v);
                if (c2 <= c1)
                    return distance(p, segLineEnd);
                float b = c1 / c2;
                float2 pb = segLineStart + b * v;
                return distance(p, pb);
            }

            float easeInOut(float t)
            {
                return t * t * (3 - 2 * t);
            }

            float easeInOutCirc(float x)
            {
                return x < 0.5
                    ? (1 - sqrt(1 - pow(2 * x, 2))) / 2
                        : (sqrt(1 - pow(-2 * x + 2, 2)) + 1) / 2;
            }

            float easeInOutSine(float x)
            {
                return -(cos(3.1415926536 * x) - 1) / 2;
            }

            float easeOutCirc(float x)
            {
                return sqrt(1 - pow(x - 1, 2));
            }

            float easeInCirc(float x)
            {
                return 1 - sqrt(1 - pow(x, 2));
            }

            float easeInSine(float x)
            {
                return 1 - cos(x * 3.1415926536 / 2);
            }

            float ease(float x)
            {
                return pow(x, _SmoothPower);
            }
            
            float smoothDistFactor(float d, float smoothDistance)
            {
                return ease(saturate(1 - d / smoothDistance)); 
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos = v.vertex;
                return o;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                fixed4 col;
                float3 edgeNormal = 0;
                float weight = 0;
                float maxF = 0;

                if (_SmoothDistance.x > 0.001) {
                    for (int i = 0; i < _EdgeCount; i++)
                    {
                        float d = getPointDistanceToSegment(input.pos.xy, _EdgePoints[i].xy, _EdgePoints[i].zw);
                        float f = smoothDistFactor(d, _SmoothDistance);
                        edgeNormal += _EdgeNormals[i].xyz * f;
                        maxF = max(maxF, f);
                        weight += f;
                    }
                }
                
                fixed3 edgeNormalPart = edgeNormal / (weight + 0.0001);                
                fixed3 faceNormalPart = _FaceNormal.xyz;
                fixed3 finalNormal = normalize(edgeNormalPart * maxF + faceNormalPart * (1 - maxF));
                
                col.rgb = finalNormal * 0.5 + 0.5;
                col.a = 1;
                return col;
            }
            
            ENDCG
        }
    }
}
