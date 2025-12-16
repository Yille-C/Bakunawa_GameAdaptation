Shader "UI/WaterRipple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RippleColor ("Ripple Color", Color) = (0.6, 0.8, 1, 0.3)
        _RippleSpeed ("Ripple Speed", Range(0.5, 5)) = 2.0
        _RippleFrequency ("Ripple Frequency", Range(1, 20)) = 8.0
        _RippleAmplitude ("Ripple Amplitude", Range(0.001, 0.05)) = 0.015
        _RippleFalloff ("Ripple Falloff", Range(0.1, 2)) = 0.8
        _RippleCount ("Max Ripples", Range(1, 10)) = 6
        _TimeOffset ("Time Offset", Float) = 0
        _Alpha ("Alpha", Range(0, 1)) = 0.4
        
        // Size controls
        _RippleMaxSize ("Ripple Max Size", Range(0.05, 0.5)) = 0.15
        _RingThickness ("Ring Thickness", Range(0.002, 0.02)) = 0.006
        _AspectRatio ("Aspect Ratio", Float) = 1.777
        
        // Random ripple centers (passed from script)
        _Ripple1Center ("Ripple 1 Center", Vector) = (0.2, 0.3, 0, 0)
        _Ripple2Center ("Ripple 2 Center", Vector) = (0.7, 0.6, 0, 0)
        _Ripple3Center ("Ripple 3 Center", Vector) = (0.4, 0.8, 0, 0)
        _Ripple4Center ("Ripple 4 Center", Vector) = (0.85, 0.2, 0, 0)
        _Ripple5Center ("Ripple 5 Center", Vector) = (0.15, 0.75, 0, 0)
        _Ripple6Center ("Ripple 6 Center", Vector) = (0.6, 0.4, 0, 0)
        
        // Ripple birth times (for staggered animation)
        _RippleTimes ("Ripple Times", Vector) = (0, 0.5, 1, 1.5)
        _RippleTimes2 ("Ripple Times 2", Vector) = (2, 2.5, 0, 0)
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
            float4 _RippleColor;
            float _RippleSpeed;
            float _RippleFrequency;
            float _RippleAmplitude;
            float _RippleFalloff;
            float _RippleCount;
            float _TimeOffset;
            float _Alpha;
            
            // Size controls
            float _RippleMaxSize;
            float _RingThickness;
            float _AspectRatio;
            
            float4 _Ripple1Center;
            float4 _Ripple2Center;
            float4 _Ripple3Center;
            float4 _Ripple4Center;
            float4 _Ripple5Center;
            float4 _Ripple6Center;
            float4 _RippleTimes;
            float4 _RippleTimes2;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            // Calculate a single ripple's contribution - perfect circles with aspect ratio correction
            float CalculateRipple(float2 uv, float2 center, float birthTime, float time)
            {
                float rippleAge = time - birthTime;
                float rippleCycle = 2.0; // Ripple lasts 2 seconds
                
                // Loop the ripple
                float localTime = fmod(rippleAge, rippleCycle);
                if (localTime < 0) localTime += rippleCycle;
                
                // Correct for aspect ratio to get perfect circles
                // Scale UV so that circles appear round, not elliptical
                float2 correctedUV = uv;
                float2 correctedCenter = center;
                correctedUV.x *= _AspectRatio;
                correctedCenter.x *= _AspectRatio;
                
                // Calculate distance in corrected space (this gives us perfect circles)
                float dist = distance(correctedUV, correctedCenter);
                
                // Use Inspector-controlled values
                float ringSpeed = (_RippleMaxSize / rippleCycle) * _RippleSpeed;
                float ringThickness = _RingThickness;
                
                float ripple = 0;
                
                // Ring 1 - main ring (outermost)
                float ring1Radius = localTime * ringSpeed;
                float ring1Dist = abs(dist - ring1Radius);
                float ring1 = smoothstep(ringThickness, 0, ring1Dist);
                
                // Ring 2 - middle ring
                float ring2Radius = localTime * ringSpeed * 0.65;
                float ring2Dist = abs(dist - ring2Radius);
                float ring2 = smoothstep(ringThickness * 0.85, 0, ring2Dist) * 0.5;
                
                // Ring 3 - innermost ring
                float ring3Radius = localTime * ringSpeed * 0.35;
                float ring3Dist = abs(dist - ring3Radius);
                float ring3 = smoothstep(ringThickness * 0.7, 0, ring3Dist) * 0.25;
                
                ripple = ring1 + ring2 + ring3;
                
                // Fade out as ripple expands and ages
                float fadeOut = 1.0 - (localTime / rippleCycle);
                fadeOut = fadeOut * fadeOut; // Quadratic falloff
                
                // Distance-based fade (ripples weaken as they expand)
                float maxDist = _RippleMaxSize * _AspectRatio;
                float distanceFade = 1.0 - smoothstep(0, maxDist, dist);
                
                ripple *= fadeOut * distanceFade;
                
                // Clip to max size
                if (dist > maxDist) ripple = 0;
                
                return saturate(ripple);
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y + _TimeOffset;
                
                float totalRipple = 0;
                
                // Calculate each ripple
                if (_RippleCount >= 1)
                    totalRipple += CalculateRipple(uv, _Ripple1Center.xy, _RippleTimes.x, time);
                if (_RippleCount >= 2)
                    totalRipple += CalculateRipple(uv, _Ripple2Center.xy, _RippleTimes.y, time);
                if (_RippleCount >= 3)
                    totalRipple += CalculateRipple(uv, _Ripple3Center.xy, _RippleTimes.z, time);
                if (_RippleCount >= 4)
                    totalRipple += CalculateRipple(uv, _Ripple4Center.xy, _RippleTimes.w, time);
                if (_RippleCount >= 5)
                    totalRipple += CalculateRipple(uv, _Ripple5Center.xy, _RippleTimes2.x, time);
                if (_RippleCount >= 6)
                    totalRipple += CalculateRipple(uv, _Ripple6Center.xy, _RippleTimes2.y, time);
                
                totalRipple = saturate(totalRipple);
                
                // Clean, flat ripple appearance - white/light blue circles
                float3 rippleColor = lerp(_RippleColor.rgb, float3(1, 1, 1), 0.6);
                
                // Only show where there are ripples, otherwise fully transparent
                float alpha = totalRipple * _Alpha * i.color.a;
                
                return fixed4(rippleColor, alpha);
            }
            ENDCG
        }
    }
}
