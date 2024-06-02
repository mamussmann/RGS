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

Shader "RGS/UI/Histogram"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

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
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _MaxValue;
            int _AgentCount;
            int _ArrayLength;
            StructuredBuffer<float> _BarValues;
            float4 _BarColors[32];

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float maxValue = _MaxValue;
                int agentCount = _AgentCount;//3;
                int arrayLength = _ArrayLength;//9;
                float lineWidthX = 0.1;
                float lineWidthY = lineWidthX * 0.25;
                float lineOffset = lineWidthY;

                float uvY = 1.0f - IN.texcoord.y;
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                int barCount = arrayLength/agentCount;
                float barWidth = 1.0/(float)(arrayLength/agentCount);
                for (int i=0; i < agentCount; i++) 
                {
                    int barIndex = (uvY + (lineOffset * i)) / barWidth;
                    float lastFactor = _BarValues[max(barIndex-1, 0) * agentCount + i] / maxValue;
                    float nextFactor = _BarValues[min(barIndex+1, barCount) * agentCount + i] / maxValue;
                    float factor = _BarValues[barIndex * agentCount + i] / maxValue;
                    float localBarXValue = ((uvY + (lineOffset * i)) - (barIndex * barWidth)) / barWidth;
                    color = IN.texcoord.x <= factor && IN.texcoord.x >= lastFactor - lineWidthY && localBarXValue < lineWidthX ? _BarColors[i] : color;
                    color = IN.texcoord.x <= factor && IN.texcoord.x >= nextFactor - lineWidthY && localBarXValue > 1.0 - lineWidthX ? _BarColors[i] : color;
                    color =  IN.texcoord.x <= factor &&  IN.texcoord.x > factor - lineWidthY  ? _BarColors[i] : color;
                }

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