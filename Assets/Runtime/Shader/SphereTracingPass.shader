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

Shader "RGS/SphereTracing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white"
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
		Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile _ _CAPSULE_SEGMENTS

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Ray
        {
            float3 origin;
            float3 direction;
        };

        struct DistResult
        {
            float distance;
            float3 normal;
            float3 color;
        };
        
#ifdef _CAPSULE_SEGMENTS
        struct SegmentData
        {
            float4 StartAge;
            float4 EndAge;
            float2 RadiusLength;
            uint color;
            float3 NormDirection;
        };

        StructuredBuffer<SegmentData> _PointBuffer;
#else
        struct PointData
        {
            float4 posRadius;
            uint color;
            float age;
        };

        StructuredBuffer<PointData> _PointBuffer;
#endif
        TEXTURE2D(_MainTex);

        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float4 _MainTex_ST;

        float4 _bgColor;
        float _useBgColor;
        int _PointCount;
        // surface
        int _MaxSteps;
        float _MinDistance;
        float _normalAvgOffsetScale;
        float _maxDistance;
        float _normalAvgKernel[81];
        // shadows
        int _MaxShadowSteps;
        float _ShadowMinDistance;
        float _softShadowScale;
        float _maxShadowDistance;
        //
        float _showAgeGradient;
        float _minAge;
        float _maxAge;
        // light
        float _ambientLightIntensity;
        float _specularIntensity;
        // bounds
        float _minX;
        float _minY;
        float _minZ;
        float _maxX;
        float _maxY;
        float _maxZ;

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Name "VERTICAL BOX BLUR"

            HLSLPROGRAM

            inline Ray CreateCameraRay(float2 uv, float4x4 camToWorld)
            {
                float3 origin = _WorldSpaceCameraPos;
                float3 direction = mul(unity_CameraInvProjection, float4(uv * 2 - 1, 0, 1)).xyz;
                direction.z *= -1;
                direction = mul(camToWorld, float4(direction, 0)).xyz;
                direction = normalize(direction);
                Ray ray;
                ray.origin = origin;
                ray.direction = direction;
                return ray;
            }

            inline float sqlength(float3 v)
            {
                return (v.x*v.x) + (v.y*v.y) + (v.z*v.z);
            }

#ifdef _CAPSULE_SEGMENTS
            inline float PointDst(float3 p, int i)
            {
                float d = dot(p - _PointBuffer[i].StartAge.xyz, _PointBuffer[i].NormDirection);
                if(d < 0.0 )
                    return length(p - _PointBuffer[i].StartAge.xyz) - (_PointBuffer[i].RadiusLength.x);
                if(d > _PointBuffer[i].RadiusLength.y)
                    return length(p - _PointBuffer[i].EndAge.xyz) - (_PointBuffer[i].RadiusLength.x);
                return length(p - _PointBuffer[i].StartAge.xyz - (d * _PointBuffer[i].NormDirection)) - (_PointBuffer[i].RadiusLength.x);
            }

            inline float3 PointNormal(float3 p, int i)
            {
                float d = dot(p - _PointBuffer[i].StartAge.xyz, _PointBuffer[i].NormDirection);
                if(d < 0.0 )
                    return p - _PointBuffer[i].StartAge.xyz;
                if(d > _PointBuffer[i].RadiusLength.y)
                    return p - _PointBuffer[i].EndAge.xyz;
                return p - _PointBuffer[i].StartAge.xyz - (d * _PointBuffer[i].NormDirection);
            }

            inline float PointAge(float3 p, int i)
            {
                return lerp(_PointBuffer[i].StartAge.w, _PointBuffer[i].EndAge.w, dot(p - _PointBuffer[i].StartAge.xyz, _PointBuffer[i].NormDirection) / _PointBuffer[i].RadiusLength.y);
            }
#else
            inline float PointDst(float3 p, int i)
            {
                return length(p - _PointBuffer[i].posRadius.xyz) - (_PointBuffer[i].posRadius.w);
            }

            inline float3 PointNormal(float3 p, int i)
            {
                return p - _PointBuffer[i].posRadius.xyz;
            }

            inline float PointAge(float3 p,int i)
            {
                return _PointBuffer[i].age ;
            }
#endif

            inline float PointColor(int i)
            {
                return _PointBuffer[i].color;
            }

#define PCX_MAX_BRIGHTNESS 16

            half3 PcxDecodeColor(uint data)
            {
                half r = (data      ) & 0xff;
                half g = (data >>  8) & 0xff;
                half b = (data >> 16) & 0xff;
                half a = (data >> 24) & 0xff;
                return half3(r, g, b) * a * PCX_MAX_BRIGHTNESS / (255 * 255);
            }

            float GetDistSimple(float3 p, float maxDist)
            {
                float m = maxDist;
                for (int i = 0; i < _PointCount; i++) {
                    m = min(m, PointDst(p,i));
                }
                return m;
            }

            inline float3 Lerp3(float3 v1, float3 v2, float3 v3, float t)
            {
                return t <= 0.5 ? lerp(v1, v2, t * 2.0) : lerp(v2, v3, (t - 0.5) * 2.0); 
            }
                            

            DistResult GetDist(float3 p, float maxDist)
            {
                float nextM;
                DistResult result;
                result.distance = maxDist;
                int index;
                for (int i = 0; i < _PointCount; i++) {
                    nextM = PointDst(p,i);
                    if(result.distance > nextM) {
                        index = i;
                        result.distance = nextM;
                    }
                }
                if(_showAgeGradient > 0.5) {
                    result.color = Lerp3(float3(1,0,0), float3(0,1,0), float3(0,0,1), (PointAge(p, index) - _minAge) / (_maxAge - _minAge));
                }else{
                    result.color = PcxDecodeColor(PointColor(index));
                }
                result.normal = normalize(PointNormal(p, index));
                return result;
            }

            inline bool PointOutsideAABB(float3 pos)
            {
                return !(pos.x >= _minX && pos.x <= _maxX && pos.y >= _minY && pos.y <= _maxY && pos.z >= _minZ && pos.z <=  _maxZ); 
            }
            inline bool PointOutsideAABBWithToleracne(float3 pos, float tolerance)
            {
                return !(pos.x >= _minX - tolerance && pos.x <= _maxX + tolerance && pos.y >= _minY - tolerance && pos.y <= _maxY + tolerance && pos.z >= _minZ - tolerance && pos.z <=  _maxZ + tolerance); 
            }

            float SampleShadow(float3 position, float3 lightDirection)
            {
                float currDist;
                float t = _ShadowMinDistance + 0.0005;
                float shadow = 1;
                for (int i = 0; i < _MaxShadowSteps && t < _maxShadowDistance; i++) {
                    float3 p = position + t * lightDirection;
                    if (PointOutsideAABB(p)) return shadow;
                    currDist = GetDistSimple(p, _maxShadowDistance);
                    if (currDist < _ShadowMinDistance) return 0;
                    shadow = min(shadow, _softShadowScale * (currDist/t));
                    t += currDist;
                }
                return shadow;
            }

            // box intersection from https://gist.github.com/unitycoder/8d1c2905f2e9be693c78db7d9d03a102#file-rayboxintersectfast-cs-L4
            float RayBoxIntersect(float3 rpos, float3 rdir, float3 vmin, float3 vmax)
            {
                float t1 = (vmin.x - rpos.x) / rdir.x;
                float t2 = (vmax.x - rpos.x) / rdir.x;
                float t3 = (vmin.y - rpos.y) / rdir.y;
                float t4 = (vmax.y - rpos.y) / rdir.y;
                float t5 = (vmin.z - rpos.z) / rdir.z;
                float t6 = (vmax.z - rpos.z) / rdir.z;

                float aMin = t1 < t2 ? t1 : t2;
                float bMin = t3 < t4 ? t3 : t4;
                float cMin = t5 < t6 ? t5 : t6;

                float aMax = t1 > t2 ? t1 : t2;
                float bMax = t3 > t4 ? t3 : t4;
                float cMax = t5 > t6 ? t5 : t6;

                float fMax = aMin > bMin ? aMin : bMin;
                float fMin = aMax < bMax ? aMax : bMax;

                float t7 = fMax > cMin ? fMax : cMin;
                float t8 = fMin < cMax ? fMin : cMax;

                float t9 = (t8 < 0 || t7 > t8) ? -1 : t7;

                return t9;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                Ray ray = CreateCameraRay(IN.uv, unity_CameraToWorld);
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                if(_PointCount == 0)
                {
                    return color;
                }
                if(_useBgColor > 0.5)
                {
                    color = _bgColor;
                }
                float t =RayBoxIntersect(ray.origin, ray.direction, float3(_minX,_minY,_minZ), float3(_maxX,_maxY,_maxZ));
                if (t <= 0) {
                    return color;
                }
                float depth = 0.0;
                float distance;
                float3 normLightDir = normalize(_MainLightPosition.xyz);

                for (int i = 0; i < _MaxSteps &&  t < _maxDistance; i++) {
                    float3 p = ray.origin + t * ray.direction;
                    distance = GetDistSimple(p, _maxDistance);
                    if (distance < _MinDistance) {
                        DistResult currDist = GetDist(p,_maxDistance);
                        //for(int j = 0; j < 27;j++) {
                        //    currDist.normal += GetDist(p + (_normalAvgOffsetScale * float3(_normalAvgKernel[j*3], _normalAvgKernel[j*3+1], _normalAvgKernel[j*3+2])), _maxDistance).normal;
                        //}
                        float3 diffuse = _MainLightColor.xyz * max(0.0,dot(currDist.normal, normLightDir));
                        float3 reflectDir = reflect(-normLightDir, currDist.normal);
                        float3 viewDir = normalize(ray.origin - p);
                        float spec = pow(max(dot(normalize(reflectDir), viewDir), 0.0), 16.0);
                        float3 specular = _MainLightColor.xyz * spec * _specularIntensity;
                        
                        color = float4(currDist.color * ((specular + diffuse) * min(1.0, SampleShadow(p, _MainLightPosition.xyz)) + float3(_ambientLightIntensity,_ambientLightIntensity,_ambientLightIntensity)),0);
                        depth = t;
                        break;
                    }
                    t += distance;
                    if(PointOutsideAABBWithToleracne(p, 0.01)) break;
                }
                return color;
            }
            ENDHLSL
        }
    }
}