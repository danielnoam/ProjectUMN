Shader "Custom/DissolveOverlay" {
    Properties {
        _SecondTex ("Secondary (RGB)", 2D) = "white" {}
        _Color2 ("Secondary Color", Color) = (1,1,1,1)
        
        _NoiseTex ("Dissolve Noise", 2D) = "white" {} 
        _NScale ("Noise Scale", Range(0, 10)) = 1 
        _DisAmount ("Noise Cutoff", Range(0.01, 1)) = 0.01
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.5
        
        _DisLineWidth ("Line Width", Range(0, 2)) = 0.05
        [HDR]_DisLineColor ("Line Color", Color) = (1,1,1,1)
        
        _Radius ("Effect Radius", Range(0, 30)) = 5
        _ShapeCutoff ("Shape Cutoff", Range(0, 1)) = 0.5
        _ShapeSmoothness ("Shape Smoothness", Range(0, 1)) = 0.1
        
        [KeywordEnum(SwapTextures, Appear, Disappear)] _STYLE ("Effect Style", Float) = 0
        [Toggle(_USE_MULTIPLE_INTERACTORS)] _UseMultipleInteractors ("Use Multiple Interactors", Float) = 0
    }
 
    SubShader {
        Tags {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        // Don't write to the depth buffer
        ZWrite Off
        // Less than or equal depth passes
        ZTest LEqual
        // Blend mode
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
        Pass {
            Name "DissolveOverlay"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _STYLE_SWAPTEXTURES _STYLE_APPEAR _STYLE_DISAPPEAR
            #pragma shader_feature_local _USE_MULTIPLE_INTERACTORS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // Function for rotating boxes
            float3 RotateAroundAxis(float3 position, float3 axis, float angle)
            {
                angle = radians(angle);
                float s = sin(angle);
                float c = cos(angle);
                float one_minus_c = 1.0 - c;
                
                axis = normalize(axis);
                float3x3 rot_mat;
                rot_mat[0] = float3(
                    one_minus_c * axis.x * axis.x + c,
                    one_minus_c * axis.x * axis.y - axis.z * s,
                    one_minus_c * axis.z * axis.x + axis.y * s
                );
                rot_mat[1] = float3(
                    one_minus_c * axis.x * axis.y + axis.z * s,
                    one_minus_c * axis.y * axis.y + c,
                    one_minus_c * axis.y * axis.z - axis.x * s
                );
                rot_mat[2] = float3(
                    one_minus_c * axis.z * axis.x - axis.y * s,
                    one_minus_c * axis.y * axis.z + axis.x * s,
                    one_minus_c * axis.z * axis.z + c
                );
                
                return mul(rot_mat, position);
            }
            
            // Interactor properties
            uniform float3 _Position;
            
            // Texture samplers
            TEXTURE2D(_SecondTex);
            SAMPLER(sampler_SecondTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            // Property variables
            CBUFFER_START(UnityPerMaterial)
                float4 _SecondTex_ST;
                half4 _Color2;
                half4 _DisLineColor;
                float _DisAmount;
                float _NScale;
                float _DisLineWidth;
                float _NoiseStrength;
                float _Radius;
                float _ShapeCutoff;
                float _ShapeSmoothness;
            CBUFFER_END
            
            // Multiple interactor arrays
            float4 _ShaderInteractorsPositions[20];
            float _ShaderInteractorsRadiuses[20];
            float4 _ShaderInteractorsBoxBounds[20];
            float4 _ShaderInteractorRotation[20];
            int _InteractorCount;
            
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform position from object to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                
                // Transform normal from object to world space
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Pass through texture coordinates
                output.uv = TRANSFORM_TEX(input.uv, _SecondTex);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Calculate interactor effect
                float interactorEffect = 0;
                
                #ifdef _USE_MULTIPLE_INTERACTORS
                    float sphereEffect = 0;
                    float boxEffect = 0;
                    
                    // Process all active interactors
                    for (int i = 0; i < min(_InteractorCount, 20); i++) {
                        // Sphere shape
                        float dist = distance(_ShaderInteractorsPositions[i].xyz, input.positionWS);
                        float sphereRadius = 1.0 - saturate(dist / _ShaderInteractorsRadiuses[i]);
                        sphereRadius = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, sphereRadius);
                        sphereEffect += sphereRadius;
                        
                        // Box shape
                        float3 rotation = _ShaderInteractorRotation[i].xyz;
                        float3 localPos = _ShaderInteractorsPositions[i].xyz - input.positionWS;
                        
                        // Apply rotation
                        float3 rotatedPos = localPos;
                        rotatedPos = RotateAroundAxis(rotatedPos, float3(1,0,0), rotation.x);
                        rotatedPos = RotateAroundAxis(rotatedPos, float3(0,1,0), rotation.y);
                        rotatedPos = RotateAroundAxis(rotatedPos, float3(0,0,1), rotation.z);
                        
                        // Calculate box bounds
                        float3 boxSize = _ShaderInteractorsBoxBounds[i].xyz;
                        float3 boxDistance = saturate(boxSize - abs(rotatedPos));
                        float boxMask = saturate(boxDistance.x * boxDistance.y * boxDistance.z);
                        boxMask = smoothstep(_ShapeCutoff, _ShapeCutoff + _ShapeSmoothness, boxMask);
                        
                        boxEffect += boxMask;
                    }
                    
                    interactorEffect = saturate(sphereEffect + boxEffect); // Clamp to 0-1 range
                #else
                    // Single interactor - sphere only
                    float dist = distance(_Position, input.positionWS);
                    interactorEffect = 1.0 - saturate(dist / _Radius);
                #endif
                
                // Apply triplanar noise mapping for better-looking effects
                float3 blendNormal = saturate(pow(input.normalWS * 1.4, 4));
                
                // Sample noise texture from three directions
                float4 noiseXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xy + _Time.y) * _NScale);
                float4 noiseXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.xz + _Time.y) * _NScale);
                float4 noiseYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (input.positionWS.yz + _Time.y) * _NScale);
                
                // Blend noise samples based on normal direction
                float3 noiseValue = noiseXY.rgb;
                noiseValue = lerp(noiseValue, noiseYZ.rgb, blendNormal.x);
                noiseValue = lerp(noiseValue, noiseXZ.rgb, blendNormal.y);
                
                // Combine noise with interactor effect
                float effectNoise = lerp(noiseValue.r * interactorEffect, interactorEffect, _NoiseStrength);
                
                // Create dissolve effect with cutoff
                float cutoff = step(_DisAmount, effectNoise);
                
                // Sample secondary texture
                half4 c2 = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, input.uv) * _Color2;
                
                // Create effect line
                float lineEffect = step(effectNoise - _DisLineWidth, _DisAmount) * cutoff;
                half3 dissolveLine = lineEffect * _DisLineColor.rgb * _DisLineColor.a;
                
                // Final color
                half4 finalColor;
                
                #if defined(_STYLE_SWAPTEXTURES)
                    // In SwapTextures mode, we show the secondary texture where the effect is active
                    finalColor.rgb = c2.rgb + dissolveLine;
                    finalColor.a = cutoff * max(c2.a, lineEffect);
                #elif defined(_STYLE_APPEAR)
                    // In Appear mode, we simply show the overlay where the effect is active
                    finalColor.rgb = dissolveLine;
                    finalColor.a = lineEffect;
                #elif defined(_STYLE_DISAPPEAR)
                    // In Disappear mode, we invert the effect
                    finalColor.rgb = c2.rgb + dissolveLine;
                    finalColor.a = (1.0 - cutoff + lineEffect) * max(c2.a, lineEffect);
                #else
                    // Default fallback
                    finalColor.rgb = c2.rgb + dissolveLine;
                    finalColor.a = cutoff * max(c2.a, lineEffect);
                #endif
                
                // Ensure we have some alpha where the line effect is
                finalColor.a = max(finalColor.a, lineEffect);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}