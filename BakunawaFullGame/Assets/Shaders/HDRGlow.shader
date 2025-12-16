Shader "UI/HDRGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 0.85, 0.3, 1)
        _GlowIntensity ("Glow Intensity", Range(1, 10)) = 3.0
        _GlowFalloff ("Glow Falloff", Range(0.5, 3)) = 1.5
        
        // UI Masking support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Blend SrcAlpha One // Additive blending for glow
        ZWrite Off
        Cull Off
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
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
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _GlowIntensity;
            float _GlowFalloff;
            float4 _ClipRect;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate radial gradient for glow
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center) * 2.0; // 0 at center, 1 at edge
                
                // Soft radial falloff
                float glow = saturate(1.0 - pow(dist, _GlowFalloff));
                
                // HDR color output - values > 1 will trigger bloom
                float3 hdrColor = _Color.rgb * i.color.rgb * _GlowIntensity;
                float alpha = glow * _Color.a * i.color.a;
                
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                return float4(hdrColor, alpha);
            }
            ENDCG
        }
    }
}
