Shader "Custom/OceanShimmer"
{
    Properties
    {
        _Color ("Ripple Color", Color) = (1, 1, 1, 1)
        _Speed ("Flow Speed", Float) = 0.5
        _DiagonalBias ("Diagonal Bias", Range(0, 1)) = 0.5
        _Scale ("Noise Scale", Float) = 0.5
        _Intensity ("Intensity", Range(0, 1)) = 0.4
        _Sharpness ("Ripple Sharpness", Range(1, 20)) = 8.0
        _WarpStrength ("Warp Strength", Range(0, 3)) = 1.0
        _FadeSpeed ("Fade Speed", Float) = 1.0
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.2
        _WaveFrequency ("Wave Frequency", Float) = 3.0
        _PixelSize ("Pixel Size", Float) = 8.0
        _MotionFPS ("Motion FPS", Float) = 8.0
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Speed;
            float _DiagonalBias;
            float _Scale;
            float _Intensity;
            float _Sharpness;
            float _WarpStrength;
            float _FadeSpeed;
            float _WaveStrength;
            float _WaveFrequency;
            float _PixelSize;
            float _MotionFPS;
            sampler2D _MainTex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float2 hash2(float2 p) {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(dot(hash2(i),               f),
                         dot(hash2(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(hash2(i + float2(0,1)), f - float2(0,1)),
                         dot(hash2(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y) * 0.5 + 0.5;
            }

            float fbm(float2 p) {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++) {
                    v += a * noise(p);
                    p = p * 2.1 + float2(3.7, 8.3);
                    a *= 0.5;
                }
                return v;
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 wp = i.worldPos * _Scale;

                // Stepped time for pixelated movement
                float t = floor(_Time.y * _MotionFPS) / _MotionFPS * _Speed;

                // Domain warp on static world coords to shape the caustic pattern organically
                float2 q = float2(fbm(wp), fbm(wp + float2(5.2, 1.3)));
                float2 warpedWp = wp + q * _WarpStrength;

                // Scroll the warped pattern in the diagonal direction (clean, stepped)
                warpedWp += float2(t * _DiagonalBias, -t);

                // Wave undulation perpendicular to flow so caustics take a wavy path
                float wt = t * 1.5;
                float2 flowDir = normalize(float2(_DiagonalBias + 0.001, -1.0));
                float2 perpDir = float2(flowDir.y, -flowDir.x);
                float along = dot(warpedWp, flowDir);
                float wave = sin(along * _WaveFrequency - wt) * _WaveStrength;
                wave += sin(along * _WaveFrequency * 0.5 - wt * 0.7 + 1.3) * _WaveStrength * 0.5;
                warpedWp += perpDir * wave;

                // Snap to pixel grid last so all movement is blocky
                warpedWp = floor(warpedWp * _PixelSize) / _PixelSize;

                float n1 = fbm(warpedWp);
                float n2 = fbm(warpedWp + float2(1.7, 9.2));

                float ripple = abs(sin(n1 * _Sharpness + n2 * 2.0));
                ripple = pow(ripple, 3.0);

                float fade = sin(_Time.y * _FadeSpeed) * 0.25 + 0.75;
                return fixed4(_Color.rgb, ripple * _Intensity * fade);
            }
            ENDCG
        }
    }
}
