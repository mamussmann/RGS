// 
// Copyright (c) 2024 Marc Mußmann
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
// This file incorporates work covered by the following copyright and  
// permission notice:  
// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license 


Shader "RGS/UI/PieMenuRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ringSelect ("Select", Color) = (1,1,1,1)
        _ringBackground ("Backround", Color) = (1,1,1,1)

        _angleDelta ("Delta angle", Float) = 0
        _angleValue ("Angle value", Float) = 0
        _ringRadius ("Radius", Float) = 0.5
        _ringWidth ("Width", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _angleDelta;
            float _angleValue;
            float _ringRadius;
            float _ringWidth;
            fixed4 _ringSelect;
            fixed4 _ringBackground;
            float _isNoSelect;

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
                float _Constant_PI = 3.1415926;
                float _Constant_Tau = _Constant_PI * 2.0;
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif


                float2 position = IN.texcoord - 0.5;
                float dotValue = dot(normalize(position), float2(0,1));
                float signDotValue = dot(normalize(position), float2(1,0));
                float angle = signDotValue < 0.0 ? abs(acos(dotValue)) : (_Constant_Tau) - abs(acos(dotValue));
                float dist = length(position);
                color.a = dist > _ringRadius && dist < _ringRadius + _ringWidth ? sin(((dist - _ringRadius) / _ringWidth) * _Constant_PI) : 0.0;

                if(_isNoSelect > 0.5) {
                    color.rgb = _ringBackground.rgb;
                    return color;
                }
                float start = _angleValue - (_angleDelta/2.0f);
                start = start < 0.0 ? _Constant_Tau + start : start;
                float end = _angleValue + (_angleDelta/2.0f);
                end = end < 0.0 ? _Constant_Tau + end : end;
                float flip = 0.0f;
                if(start > end) {
                    float temp = end;
                    end = start;
                    start = temp;
                    flip = 1.0f;
                }
                float value = angle > start && angle < end ? 1.0 - flip : flip;
                color.rgb = lerp(_ringBackground, _ringSelect, value).rgb;
                return color;
            }
        ENDCG
        }
    }
}