Shader "Custom/Xray"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        //xrayTex
        _XrayTex("Xray Texture", 2D) = "white" {}
        _XrayScale("Xray Scale", Range(0,100)) = 0.5
        //XrayColor
        _XrayColor("XrayColor", Color) = (1,1,1,1)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Pass
        {
        Name "Xray"
      
          ZTest Greater
          ZWrite off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

           
            struct v2f
            {
               
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD0;
            };

          
            sampler2D _XrayTex;
            float _XrayScale;
            fixed4 _XrayColor;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.scrPos = ComputeScreenPos(o.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
               float2 screenUV = i.scrPos.xy / i.scrPos.w;//turen screenUV to 0-1 range UV coordinates
               screenUV.y += _Time.y * 0.1;//animate the texture by scrolling it vertically over time
               fixed4 Color = tex2D(_XrayTex, screenUV * _XrayScale);
                 if(Color.r >= 0.1)
                   clip(-1);//discard the pixel
                return _XrayColor;
                
            }
        ENDCG
        }
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
