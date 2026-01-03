Shader "Sailing/SimpleWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.1, 0.5, 0.8, 0.8)
        _SpecColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.5
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 0.5
        _WaveHeight ("Wave Height", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf BlinnPhong alpha:fade vertex:vert
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        fixed4 _Color;
        half _Shininess;
        half _WaveSpeed;
        half _WaveHeight;

        void vert (inout appdata_full v)
        {
            // Animate vertices for wave effect
            float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * 2.0 + v.vertex.z * 2.0) * _WaveHeight;
            v.vertex.y += wave;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
            o.Specular = _Shininess;
            o.Gloss = 1.0;
        }
        ENDPROGRAM
    }

    FallBack "Transparent/Diffuse"
}
