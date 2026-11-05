// Lo que le falta a la imagen para parecer pelicula vieja y no sale del post-processing de URP:
// rayas, polvo, el parpadeo del proyector y la vineta que tiembla. Se dibuja encima del juego
// (una imagen de UI a pantalla completa, debajo del HUD) y no lee la pantalla: solo oscurece o aclara.
Shader "Zombineta/UI/PeliculaVieja"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tinte", Color) = (1, 1, 1, 1)
        _Fuerza ("Fuerza", Range(0, 1)) = 1
        _Cuadros ("Cuadros por segundo del proyector", Float) = 18
        _Aspecto ("Ancho / alto de la pantalla", Float) = 1.7777
        _Rayas ("Rayas", Range(0, 1)) = 0.6
        _Polvo ("Polvo", Range(0, 1)) = 0.6
        _Parpadeo ("Parpadeo", Range(0, 1)) = 0.5
        _Vineta ("Vineta", Range(0, 1)) = 0.55
        _ColorClaro ("Color de las rayas", Color) = (0.95, 0.9, 0.78, 1)
        _ColorOscuro ("Color del polvo y la vineta", Color) = (0.08, 0.05, 0.02, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
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
            float _Fuerza, _Cuadros, _Aspecto, _Rayas, _Polvo, _Parpadeo, _Vineta;
            fixed4 _ColorClaro, _ColorOscuro;

            float Azar(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

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
                // Todo cambia de a cuadros, como el proyector: no de forma continua.
                float cuadro = floor(_Time.y * _Cuadros);
                float2 uv = i.uv;

                // Vineta que respira con el parpadeo de la lampara.
                float2 c = (uv - 0.5) * float2(_Aspecto, 1.0);
                float parpadeo = (Azar(float2(cuadro, 3.1)) - 0.5) * _Parpadeo;
                float vineta = smoothstep(0.45, 1.05, length(c)) * (_Vineta + parpadeo * 0.3);
                float oscuro = saturate(vineta + max(0.0, parpadeo) * 0.18);

                // Rayas verticales finas: aparecen unos cuadros y se van, en lugares al azar.
                float claro = 0.0;
                for (int k = 0; k < 3; k++)
                {
                    float semilla = floor(cuadro / (3.0 + k)) + k * 17.0;
                    if (Azar(float2(semilla, 1.7)) < 0.55 * _Rayas)
                    {
                        float x = Azar(float2(semilla, 9.3));
                        float ancho = 0.0008 + 0.0012 * Azar(float2(semilla, 4.4));
                        float corte = step(Azar(float2(semilla, floor(uv.y * 6.0))), 0.85);
                        claro = max(claro, (1.0 - smoothstep(0.0, ancho, abs(uv.x - x))) * corte * 0.55);
                    }
                }

                // Polvo: motas oscuras que duran un cuadro.
                float2 celda = floor(uv * float2(48.0 * _Aspecto, 48.0));
                float2 dentro = frac(uv * float2(48.0 * _Aspecto, 48.0));
                float hay = step(1.0 - 0.006 * _Polvo, Azar(celda + cuadro * 1.31));
                float2 punto = float2(Azar(celda + cuadro * 2.7), Azar(celda + cuadro * 5.3));
                float mota = hay * (1.0 - smoothstep(0.05, 0.18, length(dentro - punto)));
                oscuro = max(oscuro, mota * 0.85);

                fixed4 col = claro > oscuro ? _ColorClaro : _ColorOscuro;
                col.a = max(claro, oscuro) * _Fuerza * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
