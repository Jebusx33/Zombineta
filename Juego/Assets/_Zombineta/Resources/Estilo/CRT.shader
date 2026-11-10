// Televisor viejo (CRT) para una pantalla entera dibujada en una textura: la imagen se curva hacia
// afuera como un gran angular, las esquinas quedan redondeadas, hay lineas de barrido, un poco de
// separacion de color en los bordes y una vineta. Tambien hace el encendido de la tele: una linea
// blanca que se estira de lado a lado y despues se abre en alto (_Ancho, _Alto y _Brillo los pone
// PantallaCRT con EncendidoTV).
Shader "Zombineta/UI/CRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pantalla", 2D) = "black" {}
        _Curvatura ("Curvatura (gran angular)", Range(0, 0.4)) = 0.12
        _RadioEsquina ("Radio de las esquinas (en altos de pantalla)", Range(0, 0.3)) = 0.07
        _Lineas ("Fuerza de las lineas de barrido", Range(0, 1)) = 0.22
        _LineasCantidad ("Lineas de barrido en el alto", Float) = 300
        _Aberracion ("Separacion de color en los bordes", Range(0, 0.01)) = 0.0022
        _Vineta ("Vineta", Range(0, 1)) = 0.45
        _Aspecto ("Ancho / alto de la pantalla", Float) = 1.7777
        _Ancho ("Encendido: ancho visible (0 a 1)", Range(0, 1)) = 1
        _Alto ("Encendido: alto visible (0 a 1)", Range(0, 1)) = 1
        _Brillo ("Encendido: sobreexposicion (0 a 1)", Range(0, 1)) = 0
        _Tiempo ("Tiempo (parpadeo)", Float) = 0
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

            sampler2D _MainTex;
            float _Curvatura, _RadioEsquina, _Lineas, _LineasCantidad, _Aberracion, _Vineta, _Aspecto;
            float _Ancho, _Alto, _Brillo, _Tiempo;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // Lo que hay en la imagen en ese punto; afuera de la imagen, negro.
            float Canal(float2 uv, int canal)
            {
                float adentro = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                return tex2D(_MainTex, uv)[canal] * adentro;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Centro de la pantalla en 0, bordes en -1 y 1.
                float2 c = i.uv * 2 - 1;

                // Gran angular: la imagen se infla hacia afuera. Se normaliza para que el medio de cada
                // borde quede justo en el borde (no se recorta el titulo ni la leyenda); las esquinas se pierden.
                float r2 = dot(c, c);
                float2 cd = c * (1 + _Curvatura * r2) / (1 + _Curvatura);

                // Encendido: la franja visible crece desde el centro, y la imagen entra aplastada en ella.
                float2 franja = float2(max(_Ancho, 1e-4), max(_Alto, 1e-4));
                float2 dentroFranja = cd / franja;
                float suave = 0.004;
                float visible = smoothstep(franja.x + suave, franja.x - suave, abs(cd.x)) *
                                smoothstep(franja.y + suave * 0.5, franja.y - suave * 0.5, abs(cd.y));

                float2 uv = float2(cd.x, dentroFranja.y) * 0.5 + 0.5;

                // Separacion de color: el rojo y el azul se corren un poco hacia afuera en los bordes.
                float2 corrimiento = cd * _Aberracion;
                float3 col;
                col.r = Canal(uv + corrimiento, 0);
                col.g = Canal(uv, 1);
                col.b = Canal(uv - corrimiento, 2);

                // Lineas de barrido (siguen la curva de la pantalla) y un parpadeo apenas perceptible.
                float linea = 0.5 + 0.5 * sin(uv.y * _LineasCantidad * 6.2831853);
                col *= lerp(1, linea, _Lineas);
                col *= 1 - 0.025 * frac(sin(floor(_Tiempo * 30) * 91.345) * 47453.5453);

                // Vineta: los bordes del tubo son mas oscuros.
                col *= 1 - _Vineta * pow(saturate(r2 * 0.5), 1.5);

                // Encendido: mientras abre, la imagen esta sobreexpuesta (la linea es blanca).
                col = lerp(col, float3(1, 1, 1), _Brillo);
                col *= visible;

                // Esquinas redondeadas del tubo (en la pantalla, no en la imagen).
                float2 p = abs(c) * float2(_Aspecto, 1);
                float2 caja = float2(_Aspecto, 1) - _RadioEsquina * 2;
                float d = length(max(p - caja, 0)) - _RadioEsquina * 2;
                float esquina = smoothstep(0.006, -0.006, d);

                return fixed4(col * esquina, 1) * i.color;
            }
            ENDCG
        }
    }
}
