Shader "Noir/ObjectOutline"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.75, 0.35, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _LineWidth ("Line width (world units)", Float) = 0.005
        _HaloWidth ("Halo width (world units)", Float) = 0.03
        _HaloStrength ("Halo strength", Range(0, 1)) = 0.4
        _Flicker ("Flicker", Range(0, 1)) = 0
        _Pad ("Hull padding (world units)", Float) = 0.03
        _Solid ("0 = card (SDF), 1 = solid hull", Float) = 0
        _Radial ("0 = box, 1 = radial, 2 = smoothed normals", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [HideInInspector] _BoundsCenter ("Bounds center (object space)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BoundsExtents ("Bounds extents (object space)", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Outline"
            Cull [_Cull]
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                float _LineWidth;
                float _HaloWidth;
                half _HaloStrength;
                half _Flicker;
                float _Pad;
                float _Solid;
                float _Radial;
                float _Cull;
                float4 _BoundsCenter;
                float4 _BoundsExtents;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 smoothNormal : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 local : TEXCOORD0;
            };

            float3 ObjectScale()
            {
                float4x4 m = GetObjectToWorldMatrix();
                return max(float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22)), 1e-5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 s = ObjectScale();
                float3 ext = _BoundsExtents.xyz;
                float3 c = IN.positionOS.xyz - _BoundsCenter.xyz;
                float3 p = IN.positionOS.xyz;

                if (_Radial > 1.5)
                {
                    float3 n = dot(IN.smoothNormal, IN.smoothNormal) > 0.5 ? IN.smoothNormal : IN.normalOS;
                    p += normalize(n + 1e-6) * (_Pad / s);
                }
                else if (_Radial > 0.5)
                    p += normalize(c + 1e-6) * (_Pad / s);
                else
                {
                    float3 solidAxis = step(1e-4, ext);
                    float3 onFace = step(ext * 0.98, abs(c)) * solidAxis;
                    p += sign(c) * onFace * (_Pad / s);
                }

                OUT.local = p - _BoundsCenter.xyz;
                OUT.positionCS = TransformObjectToHClip(p);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * 9.0;
                half flick = 1.0 - _Flicker * (sin(t) * sin(t * 2.7 + 1.3) * 0.5 + 0.5);

                half alpha;
                if (_Solid > 0.5)
                    alpha = _Intensity * flick;
                else
                {
                    float3 s = ObjectScale();
                    float3 ext = _BoundsExtents.xyz;
                    float3 q = max(abs(IN.local) * s - ext * s, 0.0) * step(1e-4, ext);
                    float d = length(q);
                    float aa = max(fwidth(d), 1e-6);
                    half inner = saturate(d / aa);
                    half edge = 1.0 - smoothstep(_LineWidth * 0.5, _LineWidth, d);
                    half halo = pow(saturate(1.0 - d / max(_HaloWidth, 1e-5)), 2.0) * _HaloStrength;

                    alpha = saturate(edge + halo) * inner * _Intensity * flick;
                }

                return half4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}