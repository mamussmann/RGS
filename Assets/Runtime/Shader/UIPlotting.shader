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

Shader "RGS/UI/Plotting"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _HalfLineWidth ("Line width", Float) = 0.01
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
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
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
            float _flipXY;
            float _HalfLineWidth;
            int _SampleTypes;
            float4 _ColorSample0;
            int _SampleCount0;
            float _Samples0[200];
            float4 _ColorSample1;
            int _SampleCount1;
            float _Samples1[200];
            float4 _ColorSample2;
            int _SampleCount2;
            float _Samples2[200];

            float4 _ColorSample3;
            int _SampleCount3;
            float _Samples3[200];
            float4 _ColorSample4;
            int _SampleCount4;
            float _Samples4[200];
            float4 _ColorSample5;
            int _SampleCount5;
            float _Samples5[200];
            float4 _ColorSample6;
            int _SampleCount6;
            float _Samples6[200];
            float4 _ColorSample7;
            int _SampleCount7;
            float _Samples7[200];
            float4 _ColorSample8;
            int _SampleCount8;
            float _Samples8[200];
            float4 _ColorSample9;
            int _SampleCount9;
            float _Samples9[200];

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                if(_flipXY > 0.5) {
                    float tx = OUT.texcoord.x;
                    OUT.texcoord.x = OUT.texcoord.y;
                    OUT.texcoord.y = tx;
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            void Plot(int sampleCount, float samples[200], float4 sampleColor, float3 bgColor, inout half4 color, v2f IN)
            {
                float _Constant_PI = 3.1415926;
                for (int i=0; i< _SampleCount0 - 1; i++) 
                {
                    if(IN.texcoord.x >= samples[i * 2]  && IN.texcoord.x < samples[i * 2 + 2])
                    {
                        float dist = samples[i * 2 ] - samples[i * 2 + 2];
                        float m = samples[i * 2 + 3] - samples[i * 2 +1];
                        float b = samples[i * 2 +1 ];
                        float yLine = ((samples[i * 2] - IN.texcoord.x) / dist) * m + b;

                        if(IN.texcoord.y > yLine - _HalfLineWidth && IN.texcoord.y <= yLine + _HalfLineWidth)
                        {
                            float t = (IN.texcoord.y - (yLine - _HalfLineWidth)) / (_HalfLineWidth * 2.0);
                            float mxFac = abs(sin(t * _Constant_PI));
                            color.rgb = mxFac * sampleColor + ((1.0 - mxFac) * bgColor);
                        }
                    }
                }
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                if(_SampleTypes > 0) {Plot(_SampleCount0, _Samples0, _ColorSample0, color.rgb, color, IN);}
                if(_SampleTypes > 1) {Plot(_SampleCount1, _Samples1, _ColorSample1, color.rgb, color, IN);}
                if(_SampleTypes > 2) {Plot(_SampleCount2, _Samples2, _ColorSample2, color.rgb, color, IN);}
                if(_SampleTypes > 3) {Plot(_SampleCount3, _Samples3, _ColorSample3, color.rgb, color, IN);}
                if(_SampleTypes > 4) {Plot(_SampleCount4, _Samples4, _ColorSample4, color.rgb, color, IN);}
                if(_SampleTypes > 5) {Plot(_SampleCount5, _Samples5, _ColorSample5, color.rgb, color, IN);}
                if(_SampleTypes > 6) {Plot(_SampleCount6, _Samples6, _ColorSample6, color.rgb, color, IN);}
                if(_SampleTypes > 7) {Plot(_SampleCount7, _Samples7, _ColorSample7, color.rgb, color, IN);}
                if(_SampleTypes > 8) {Plot(_SampleCount8, _Samples8, _ColorSample8, color.rgb, color, IN);}
                if(_SampleTypes > 9) {Plot(_SampleCount9, _Samples9, _ColorSample9, color.rgb, color, IN);}
                
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}