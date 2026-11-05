// La transicion de iris: la pantalla en negro con un circulo transparente que se agranda o se achica.
// Va en la imagen del fundido (UI). El centro y el radio los pone ScreenFader.
Shader "Zombineta/UI/Iris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tinte", Color) = (0, 0, 0, 1)
        _Centro ("Centro (0 a 1 de la pantalla)", Vector) = (0.5, 0.5, 0, 0)
        _Radio ("Radio (en altos de pantalla)", Float) = 0
        _Aspecto ("Ancho / alto de la pantalla", Float) = 1.7777
        _Borde ("Suavidad del borde", Float) = 0.004
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _Centro;
            float _Radio;
            float _Aspecto;
            float _Borde;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv - _Centro.xy;
                d.x *= _Aspecto;
                float tapa = smoothstep(_Radio - _Borde, _Radio + _Borde, length(d));
                fixed4 c = i.color;
                c.a *= tapa;
                return c;
            }
            ENDCG
        }
    }
}
