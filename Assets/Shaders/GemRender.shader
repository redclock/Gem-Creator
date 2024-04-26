Shader "GemCreator/GemRender"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _EyePos("Eye Pos", Vector) = (0, 0, 2, 0)
        _MainLightDir("Light Dir", Vector) = (-0.5, -0.5, 2.2, 1)
        [HDR] _MainColor("Main Color", Color) = (0.1, 0.5, 1.1, 1)

        [Toggle(USE_SPECULAR)] _UseSpec("Enable Specualar", Float) = 1
        _Roughness("Roughness", Range(0, 1)) = 0.1
        
        [Toggle(USE_SCATTER)] _UseScatter("Enable Scattering", Float) = 1
        _RoundNormal("Round Normal", Range(0, 1)) = 0.5
        _Transmission("Transmission", Range(0, 4)) = 1
        _Fresnel("Fresnel", Range(0, 1)) = 0.5
        _FadeOut("Fade Out", Range(0.5, 1)) = 0.5
        
        [Toggle(USE_REFRACT)] _UseRefract("Enable Refract Light", Float) = 1
        _RefractFactor("Refract Factor", Range(0, 1)) = 0.5
        _RefractOffset("Refract Offset", Range(-1, 1)) = 0.5

    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_SPECULAR
            #pragma shader_feature USE_SCATTER
            #pragma shader_feature USE_REFRACT

            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _EyePos;
            float4 _MainLightDir;
            
            float4 _MainColor;
            
            float _Roughness;
            
            float _RoundNormal;
            float _Transmission;
            float _Fresnel;
            float _FadeOut;
            
            float _RefractFactor;
            float _RefractOffset;
            
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Constants
            const float PI = 3.14159265359;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // GGX distribution function
            float GGX_D(float roughness, float NdotH) {
                float alpha = roughness * roughness;
                float alpha2 = alpha * alpha;
                float NdotH2 = NdotH * NdotH;
                float d = (NdotH2 * (alpha2 - 1) + 1);
                return alpha2 / (PI * d * d);
            }

            float sqr(float x) {
                return x * x;
            }
            
            float GGXNormalDistribution(float roughness, float NdotH)
            {
                float roughnessSqr = roughness*roughness;
                float NdotHSqr = NdotH*NdotH;
                float TanNdotHSqr = (1-NdotHSqr)/NdotHSqr;
                return (1/3.1415926535) * sqr(roughness/(NdotHSqr * (roughnessSqr + TanNdotHSqr)));
            }

            
            float PhongNormalDistribution(float RdotV, float specularpower, float speculargloss){
                float Distribution = pow(RdotV,speculargloss) * specularpower;
                Distribution *= (2+specularpower) / (2*3.1415926535);
                return Distribution;
            }

            float BlinnPhongNormalDistribution(float NdotH, float specularpower, float speculargloss){
                float Distribution = pow(max(0, NdotH),speculargloss) * specularpower;
                Distribution *= (2+specularpower) * 10;
                return Distribution;
            }   
            
            // Geometry function
            float GGX_G(float roughness, float NdotL, float NdotV) {
                float k = (roughness + 1) * (roughness + 1) / 8;
                float gl = NdotL / (NdotL * (1 - k) + k);
                float gv = NdotV / (NdotV * (1 - k) + k);
                return gl * gv;
            }

            // Fresnel-Schlick approximation
            float FresnelSchlick(float cosTheta, float F0) {
                float sq = cosTheta * cosTheta;
                float quad = sq * sq;
                return F0 + (1.0 - F0) * quad;
            }
            
            
            // GGX Lighting function
            float GGXLighting(float3 lightDir, float3 lightDir2, float3 viewDir, float3 normal, float roughness)
            {
                // Normalize vectors
                lightDir = normalize(lightDir);
                viewDir = normalize(viewDir);
                normal = normalize(normal);

                // Calculate halfway vector
                float3 halfway1 = normalize(lightDir + viewDir);
                float3 halfway2 = normalize(lightDir2 + viewDir);
                // Compute cosines of angles
                float NdotL = dot(normal, lightDir);
                float NdotV = dot(normal, viewDir);
                float NdotH = max(dot(normal, halfway1), dot(normal, halfway1));
                //float VdotH = dot(viewDir, halfway);

                float glossiness = 1 - roughness;

                //float D = BlinnPhongNormalDistribution(NdotH, glossiness,  max(1, glossiness * 200));
                // Calculate GGX terms
                float D = GGXNormalDistribution(roughness, NdotH);
                //float G = GGX_G(roughness, NdotL, NdotV);
                //float F = FresnelSchlick(NdotV, F0);
                //return D * F * G / 4;
                //return F;
                // Calculate specular lighting

                return D / 8;

                // Final lighting color
                //return specular;

            }

            
            half3 computeScattering(half3 normal, half3 lightDir, half3 scatterColor)
            {
                half NdotL = dot(normal, lightDir);
                half3 color = scatterColor * max(0, lerp(1, -NdotL, _FadeOut));
                return color;
            }

            half3 computeBackScattering(half3 normal, half3 lightDir, half3 scatterColor)
            {
                half NdotL = dot(normal, lightDir);
                half3 color = scatterColor * max(0, -NdotL * 0.5 + 0.5);
                return color;
            }
            
            half3 computeSpecular(half3 normal, half3 viewDir, half3 lightDir)
            {
                float roughness = _Roughness;
                float F0 = 0.04;

                float specular = GGXLighting(lightDir, viewDir, normal, roughness, F0);
                return specular;
            }

            half3 getSphereNormal(half2 position, half2 lightDir)
            {
                half2 xy = (position.xy - lightDir.xy * 0.3) * (_Transmission);
                
                float sq = dot(xy, xy);
                if (sq > 1)
                {
                    return normalize(float3(xy, 0));
                }
                else
                {
                    return float3(xy, sqrt(1 - sq));
                }
                
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //return fixed4(i.uv, 0, 1);
                
                fixed4 col = tex2D(_MainTex, i.uv);

                
                if (col.a < 0.5)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                if (length(col.xyz) < 0.5)
                {
                    //return fixed4(0, 0, 0, 0);
                }

                half3 normal = col.xyz * 2 - 1;

                normal = normalize(normal);

                //return float4(normal * 0.5 + 0.5, 1);
                half3 position = float3(i.uv * 2 - 1, 0);
                
                //return float4(sphereNormal * 0.5 + 0.5, 1);
                
                half3 viewDir = normalize(_EyePos - position);
                //return float4(viewDir * 0.5 + 0.5, 1);
                half3 lightDir = normalize(_MainLightDir);

                half3 sphereNormal = getSphereNormal(position.xy, lightDir);

                float F0 = _Fresnel;
                float NdotV = saturate(dot(normal, viewDir));
                float F = FresnelSchlick(1 - NdotV, F0);
                
                //return float4(F, F, F, 1);
                //return fixed4(lightDir, 1);
                half3 color = 0;

                half3 lightDir2 = float3(-lightDir.x, -lightDir.y, lightDir.z);

                #ifdef USE_SPECULAR
                color += GGXLighting(lightDir, lightDir2, viewDir, normal, _Roughness) * F;
                #endif
                
                half3 scatterNormal = normalize(lerp(normal, sphereNormal, _RoundNormal)); 
                
                #ifdef USE_SCATTER
                color += computeScattering(scatterNormal, lightDir, _MainColor) * (1 - F);
                // color += computeScattering(scatterNormal, lightDir2, _MainColor) * （1 - F) * 0.1;
                #endif
                
                //return float4(color, 1);
                
                half2 uvOffset = (normal.xy - viewDir.xy * 0.5) * _RefractOffset;
                half2 backUV = 1 - i.uv.xy + uvOffset;
                fixed4 backNormal = tex2D(_MainTex, backUV);
                //return backNormal;
                //backNormal *= 0.5;
                backNormal = normalize(backNormal * 2 - 1);
                half3 refractColor = computeBackScattering(backNormal.xyz, -lightDir, _MainColor);
                //backColor = computeTransmission(backNormal.xyz, -lightDir, 0);
                
                #ifdef USE_REFRACT
                color += refractColor * _RefractFactor;
                #endif

                color.rgb *= _MainLightDir.a;
                return float4(color, 1);
            }
            ENDCG
        }
    }
}