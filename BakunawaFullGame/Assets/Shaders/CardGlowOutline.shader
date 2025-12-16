Shader "UI/CardGlowOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _GlowColor ("Glow Color", Color) = (1, 0.85, 0.2, 1)
        _GlowSize ("Glow Size", Range(0, 50)) = 8
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        
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
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GlowOutline"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            fixed4 _GlowColor;
            float _GlowSize;
            float _GlowIntensity;
            float _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // Create a glowing rectangular border effect
                // Distance from edges (0 at edge, 0.5 at center)
                float2 edgeDist = min(uv, 1.0 - uv);
                float minDist = min(edgeDist.x, edgeDist.y);
                
                // Normalized glow size in UV space
                float glowUV = _GlowSize * _MainTex_TexelSize.x;
                
                // Soft edge glow - peaks at the edge and fades inward
                float glow = 1.0 - smoothstep(0.0, glowUV * 2.0, minDist);
                
                // Add inner falloff for more natural look
                float innerFade = smoothstep(0.0, glowUV * 0.5, minDist);
                glow *= innerFade;
                
                // Pulsing animation
                float pulse = 0.7 + 0.3 * sin(_Time.y * _PulseSpeed);
                glow *= pulse;
                
                // Final color with glow
                fixed4 col = _GlowColor;
                col.a *= glow * _GlowIntensity * IN.color.a;
                
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                return col;
            }
            ENDCG
        }
    }
}
