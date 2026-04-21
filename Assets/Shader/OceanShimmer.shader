Shader "Custom/OceanShimmer"
{
    Properties
    {
          _ShimmerColor ("Shimmer Color", Color) = (1, 1, 1, 1)
          _Speed ("Scroll Speed", Float) = 1.0
          _Frequency ("Band Frequency", Float) = 5.0
          _Intensity ("Intensity", Range(0,1)) = 0.3
          _PixelSize ("Pixel Size", Float) = 32.0
          _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _ShimmerColor;
            float _Speed;
            float _Frequency;
            float _Intensity;
            float _PixelSize;
            sampler2D _MainTex;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float band1 = sin(i.uv.y * _Frequency + _Time.y * _Speed);
                band1 = band1 * 0.5 + 0.5;

                float band2 = sin(i.uv.y * _Frequency * 0.6 + _Time.y * _Speed * 1.7);
                band2 = band2 * 0.5 + 0.5;

                float shimmer = (band1 + band2) * 0.5;
                return _ShimmerColor * shimmer * _Intensity;
            }
            ENDCG
        }
    }
}
