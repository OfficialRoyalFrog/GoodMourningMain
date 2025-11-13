Shader "UI/Screen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil     ("Stencil ID", Float) = 0
        _StencilOp   ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend OneMinusDstColor One
        ColorMask [_ColorMask]
        Stencil{ Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }

        Pass
        {
            Name "ScreenUI"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float2 worldXY:TEXCOORD1; };

            sampler2D _MainTex; float4 _MainTex_ST; float4 _Color;
            float4 _ClipRect;

            inline float Get2DClipping(float2 pos, float4 r){ float2 i = step(r.xy,pos) * step(pos,r.zw); return i.x*i.y; }

            v2f vert(appdata v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
                o.color = v.color * _Color;
                o.worldXY = v.vertex.xy;
                return o;
            }

            half4 frag(v2f i):SV_Target
            {
                half4 tex = tex2D(_MainTex, i.uv);
                half4 tint = _Color * i.color;

                half a = tex.a * tint.a;
                half3 rgb = tex.rgb * tint.rgb;
                rgb *= a;                                     // premultiply to kill white fringes and transparent glow

                #ifdef UNITY_UI_CLIP_RECT
                    a *= Get2DClipping(i.worldXY, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(a - 0.001h);
                #endif

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}