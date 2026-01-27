// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/Sprites/DoodleDiffuse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _NoiseScale ("Noise Scale", Float) = 0.01
        _NoiseSnap ("Noise Snap (seconds)", Float) = 0.1

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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #include "UnitySprites.cginc"

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        };
        float _NoiseScale;
        float _NoiseSnap;

        inline float SnapTime (float x, float step)
        {
            return step * round(x / step);
        }

        float3 random3(float3 c)
        {
            float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
            float3 r;
            r.z = frac(512.0 * j);
            j *= 0.125;
            r.x = frac(512.0 * j);
            j *= 0.125;
            r.y = frac(512.0 * j);
            return r - 0.5;
        }


        void vert (inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
    
            float time = SnapTime(_Time.y, _NoiseSnap);
            // Используем UV + время для шума (менее эффективно для анимации, но стабильнее)
            float2 noise = random3(float3(v.texcoord.xy, time)).xy * _NoiseScale;
    
            v.vertex.xy += noise;
    
        #if defined(PIXELSNAP_ON)
            v.vertex = UnityPixelSnap(v.vertex);
        #endif
    
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture (IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb * c.a;
            o.Alpha = c.a;
        }
        ENDCG
    }

Fallback "Transparent/VertexLit"
}
