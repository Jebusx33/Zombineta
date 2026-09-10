using System;
using UnityEngine;

namespace Zombineta.CameraFx
{
    public enum CameraMode
    {
        /// <summary>Juego normal: el encuadre lo maneja la tension.</summary>
        Follow,

        /// <summary>La horda te alcanzo: plano cerrado sobre la moto.</summary>
        Catch,

        /// <summary>Llegaste al refugio: el plano se abre.</summary>
        Victory,
    }

    /// <summary>Lo que la camara necesita saber del juego en este frame.</summary>
    public struct CameraInput
    {
        public float PlayerX;    // mundo
        public float PlayerY;    // mundo: altura del carril
        public float GapMeters;  // distancia a la horda
        public bool Turbo;
        public float GoalX;      // mundo: el refugio
    }

    /// <summary>Donde y como poner la camara.</summary>
    public struct CameraPose
    {
        public float X;
        public float Y;
        public float Size;
        public float Roll;
    }

    /// <summary>
    /// El lenguaje de camara, en C# plano: recibe el estado del juego y devuelve una pose.
    /// No toca ninguna camara, asi que se prueba sin escena.
    ///
    /// Una sola senal maneja el encuadre: la distancia a la horda. Si se acerca, el plano
    /// se cierra; si se aleja (turbo, disparo, faro), se abre solo, porque cambio la
    /// distancia. Encima van los golpes de cada evento (trauma y resortes).
    ///
    /// Al cerrarse, la moto conserva siempre su distancia al borde derecho: el plano se
    /// cierra desde atras, sobre la horda. Si se cerrara al centro, en peligro verias menos
    /// calle adelante y chocarias mas, lo que acercaria mas la horda.
    /// </summary>
    public sealed class CameraDirector
    {
        const float MaxSpringStep = 1f / 120f;

        readonly CameraConfig cfg;

        float size;
        float x;
        float y;
        float turbo;
        float trauma;
        float time;
        float springX, springXVel;
        float springZoom, springZoomVel;

        public CameraDirector(CameraConfig config)
        {
            cfg = config != null ? config : throw new ArgumentNullException(nameof(config));
            size = cfg.baseSize;
            y = cfg.viewBottomY + size;
        }

        /// <summary>Interruptor general. Apagado: la camara de siempre, sin tension ni golpes.</summary>
        public bool EffectsEnabled { get; set; } = true;

        /// <summary>Apaga solo sacudidas y golpes; el encuadre por tension sigue.</summary>
        public bool ShakeEnabled { get; set; } = true;

        public float Aspect { get; set; } = 16f / 9f;

        public CameraMode Mode { get; private set; } = CameraMode.Follow;

        public float Trauma => trauma;

        /// <summary>0 = horda lejos, 1 = horda encima. Ya suavizada (smoothstep).</summary>
        public float Threat { get; private set; }

        public float HalfWidth(float orthoSize) => orthoSize * Aspect;

        /// <summary>
        /// Tension a partir de la distancia a la horda, con smoothstep: sin cortes al
        /// entrar ni al salir del rango.
        /// </summary>
        public static float ThreatFromGap(float gap, float safeGap, float dangerGap)
        {
            if (safeGap <= dangerGap)
                return gap <= dangerGap ? 1f : 0f;
            float t = Mathf.Clamp01((safeGap - gap) / (safeGap - dangerGap));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Salta directo al encuadre ideal, sin transicion (largada, reintento).</summary>
        public void Snap(CameraInput input)
        {
            Mode = CameraMode.Follow;
            trauma = 0f;
            springX = springXVel = springZoom = springZoomVel = 0f;
            turbo = EffectsEnabled && input.Turbo ? 1f : 0f;
            Threat = ThreatFromGap(input.GapMeters, cfg.safeGap, cfg.dangerGap);
            size = FollowSize();
            x = FollowX(input.PlayerX);
            y = cfg.viewBottomY + size;
        }

        /// <summary>Suma trauma (0..1). La sacudida crece con el cuadrado: los golpes chicos casi no se notan.</summary>
        public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));

        /// <summary>Impulso a los resortes: adelante (+) o atras (-), y zoom adentro (-) o afuera (+).</summary>
        public void Punch(float forward, float zoom)
        {
            float w = Omega;
            springXVel += forward * w;
            springZoomVel += zoom * w;
        }

        public void BeginCatch() => Mode = CameraMode.Catch;

        public void BeginVictory() => Mode = CameraMode.Victory;

        public CameraPose Step(CameraInput input, float dt)
        {
            if (dt < 0f) dt = 0f;
            time += dt;

            Threat = ThreatFromGap(input.GapMeters, cfg.safeGap, cfg.dangerGap);
            turbo = Approach(turbo, EffectsEnabled && input.Turbo ? 1f : 0f, cfg.turboResponseTime, dt);

            var mode = EffectsEnabled ? Mode : CameraMode.Follow;
            switch (mode)
            {
                case CameraMode.Catch:
                    size = Approach(size, cfg.catchSize, cfg.catchZoomTime, dt);
                    x = Approach(x, input.PlayerX, cfg.catchZoomTime, dt);
                    // La moto un poco abajo del centro: se ve la horda que se le viene encima.
                    y = Approach(y, input.PlayerY + 0.6f, cfg.catchZoomTime, dt);
                    break;

                case CameraMode.Victory:
                    size = Approach(size, cfg.victorySize, cfg.victoryOpenTime, dt);
                    x = Approach(x, input.GoalX, cfg.victoryOpenTime, dt);
                    y = cfg.viewBottomY + size;   // la calle no se mueve: se abre hacia el cielo
                    break;

                default:
                    float target = FollowSize();
                    // Asimetrico: cerrarse lento, abrirse rapido.
                    float tau = target < size ? cfg.zoomInTime : cfg.zoomOutTime;
                    size = Approach(size, target, tau, dt);
                    x = Approach(x, FollowX(input.PlayerX), cfg.followTime, dt);
                    y = cfg.viewBottomY + size;
                    break;
            }

            StepSprings(dt);
            trauma = Mathf.Max(0f, trauma - cfg.traumaDecay * dt);

            bool shake = EffectsEnabled && ShakeEnabled;
            float t2 = shake ? trauma * trauma : 0f;

            return new CameraPose
            {
                X = x + (shake ? springX : 0f) + t2 * cfg.maxShakeOffset * Noise(1f),
                Y = y + t2 * cfg.maxShakeOffset * Noise(2f),
                Size = Mathf.Max(0.5f, size + (shake ? springZoom : 0f)),
                Roll = t2 * cfg.maxShakeRoll * Noise(3f),
            };
        }

        // --- Interno -----------------------------------------------------------

        float FollowSize()
        {
            if (!EffectsEnabled)
                return cfg.baseSize;
            return Mathf.Lerp(cfg.wideSize, cfg.tightSize, Threat) + turbo * cfg.turboExtraSize;
        }

        /// <summary>X de camara que deja a la moto a lookAhead del borde derecho, con el zoom actual.</summary>
        float FollowX(float playerX)
        {
            float look = cfg.lookAhead + turbo * cfg.turboExtraLookAhead;
            return playerX + look - HalfWidth(size);
        }

        float Omega => 2f * Mathf.PI * Mathf.Max(0.1f, cfg.punchFrequency);

        void StepSprings(float dt)
        {
            float w = Omega;
            float z = cfg.punchDamping;
            int n = Mathf.Max(1, Mathf.CeilToInt(dt / MaxSpringStep));
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                springXVel += (-w * w * springX - 2f * z * w * springXVel) * h;
                springX += springXVel * h;
                springZoomVel += (-w * w * springZoom - 2f * z * w * springZoomVel) * h;
                springZoom += springZoomVel * h;
            }
        }

        // Ruido suave y no aleatorio cuadro a cuadro: un temblor que se lee como golpe, no como error de imagen.
        float Noise(float channel) =>
            (Mathf.PerlinNoise(channel * 17.3f, time * cfg.shakeFrequency) - 0.5f) * 2f;

        /// <summary>Suavizado exponencial: el mismo resultado a 30 o a 144 cuadros por segundo.</summary>
        static float Approach(float current, float target, float tau, float dt) =>
            tau <= 0f ? target : target + (current - target) * Mathf.Exp(-dt / tau);
    }
}
