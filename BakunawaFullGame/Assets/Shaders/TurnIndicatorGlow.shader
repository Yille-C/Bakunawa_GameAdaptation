Shader "UI/TurnIndicatorGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 0.5, 1, 1)
        _GlowColor2 ("Secondary Glow Color", Color) = (0, 0.3, 0.8, 1)
        _EdgeWidth ("Edge Width", Range(0.001, 0.1)) = 0.025
        _GlowFalloff ("Glow Falloff", Range(0.01, 0.2)) = 0.06
        _GlowIntensity ("Glow Intensity", Range(0.5, 4)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0.5, 8)) = 2.0
        _FlowSpeed ("Flow Speed", Range(0.1, 3)) = 0.8
        _ParticleCount ("Particle Count", Range(3, 20)) = 8
        _Alpha ("Alpha", Range(0, 1)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float4 _GlowColor2;
            float _EdgeWidth;
            float _GlowFalloff;
            float _GlowIntensity;
            float _PulseSpeed;
            float _FlowSpeed;
            float _ParticleCount;
            float _Alpha;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;
                
                // Calculate distance from each edge
                float distFromLeft = uv.x;
                float distFromRight = 1.0 - uv.x;
                float distFromBottom = uv.y;
                float distFromTop = 1.0 - uv.y;
                
                // Find minimum distance to any edge
                float edgeDist = min(min(distFromLeft, distFromRight), min(distFromBottom, distFromTop));
                
                // FEATHERED GLOW: Smooth gradient from edge to transparent
                // No hard edge - just a smooth falloff
                float totalGlowWidth = _EdgeWidth + _GlowFalloff;
                
                // Soft feathered glow - strongest at edge, fading smoothly to nothing
                float featheredGlow = 1.0 - smoothstep(0, totalGlowWidth, edgeDist);
                
                // Apply exponential falloff for more natural feathering
                featheredGlow = pow(featheredGlow, 1.5);
                
                // Animated pulse (subtle breathing)
                float pulse = 0.8 + 0.2 * sin(time * _PulseSpeed);
                
                // Flowing particle effect along edges
                float particleEffect = 0;
                
                // Only add particles when close to edge
                if (edgeDist < totalGlowWidth)
                {
                    float edgeStrength = 1.0 - (edgeDist / totalGlowWidth);
                    edgeStrength = pow(edgeStrength, 2.0); // Concentrate particles near edge
                    
                    // Horizontal edges (top/bottom) - horizontal particle flow
                    float horizParticles = 0;
                    if (distFromTop < totalGlowWidth || distFromBottom < totalGlowWidth)
                    {
                        // Layer 1: Main particles
                        float p1 = frac(uv.x * _ParticleCount + time * _FlowSpeed);
                        float particle1 = smoothstep(0.2, 0, abs(p1 - 0.5));
                        
                        // Layer 2: Offset particles (opposite direction)
                        float p2 = frac(uv.x * _ParticleCount * 0.6 - time * _FlowSpeed * 0.7 + 0.33);
                        float particle2 = smoothstep(0.18, 0, abs(p2 - 0.5)) * 0.7;
                        
                        // Layer 3: Slower, larger particles
                        float p3 = frac(uv.x * _ParticleCount * 0.4 + time * _FlowSpeed * 0.4 + 0.66);
                        float particle3 = smoothstep(0.25, 0, abs(p3 - 0.5)) * 0.5;
                        
                        horizParticles = (particle1 + particle2 + particle3) * edgeStrength;
                    }
                    
                    // Vertical edges (left/right) - vertical particle flow
                    float vertParticles = 0;
                    if (distFromLeft < totalGlowWidth || distFromRight < totalGlowWidth)
                    {
                        // Layer 1
                        float p1 = frac(uv.y * _ParticleCount + time * _FlowSpeed);
                        float particle1 = smoothstep(0.2, 0, abs(p1 - 0.5));
                        
                        // Layer 2
                        float p2 = frac(uv.y * _ParticleCount * 0.6 - time * _FlowSpeed * 0.7 + 0.33);
                        float particle2 = smoothstep(0.18, 0, abs(p2 - 0.5)) * 0.7;
                        
                        // Layer 3
                        float p3 = frac(uv.y * _ParticleCount * 0.4 + time * _FlowSpeed * 0.4 + 0.66);
                        float particle3 = smoothstep(0.25, 0, abs(p3 - 0.5)) * 0.5;
                        
                        vertParticles = (particle1 + particle2 + particle3) * edgeStrength;
                    }
                    
                    particleEffect = max(horizParticles, vertParticles);
                }
                
                // Subtle corner glow pulses
                float cornerGlow = 0;
                float cornerRadius = 0.1;
                
                float c1 = 1.0 - smoothstep(0, cornerRadius, length(uv - float2(0, 0)));
                float c2 = 1.0 - smoothstep(0, cornerRadius, length(uv - float2(1, 0)));
                float c3 = 1.0 - smoothstep(0, cornerRadius, length(uv - float2(0, 1)));
                float c4 = 1.0 - smoothstep(0, cornerRadius, length(uv - float2(1, 1)));
                
                // Staggered corner pulsing
                cornerGlow = (c1 * (0.5 + 0.5 * sin(time * _PulseSpeed * 1.3)) +
                              c2 * (0.5 + 0.5 * sin(time * _PulseSpeed * 1.3 + 1.57)) +
                              c3 * (0.5 + 0.5 * sin(time * _PulseSpeed * 1.3 + 3.14)) +
                              c4 * (0.5 + 0.5 * sin(time * _PulseSpeed * 1.3 + 4.71))) * 0.25;
                
                // Combine: feathered base + particles + corners
                float finalGlow = featheredGlow * pulse + particleEffect * 0.5 + cornerGlow;
                finalGlow = saturate(finalGlow);
                
                // Color blending: particles add secondary color highlights
                float colorBlend = particleEffect * 0.4 + cornerGlow * 0.3;
                float3 finalColor = lerp(_GlowColor.rgb, _GlowColor2.rgb, colorBlend);
                finalColor *= _GlowIntensity;
                
                // Final alpha with feathered falloff
                float alpha = finalGlow * _Alpha * i.color.a;
                
                // Ensure center is completely transparent
                if (edgeDist > totalGlowWidth)
                    alpha = 0;
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
