using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Zombineta.Juego.EditorTools.Audio
{
    /// <summary>
    /// Sintetiza a mano (cabecera RIFF + PCM, sin librerias externas) los WAV provisorios del
    /// sistema de sonido: musica, ambiente y efectos. Cada archivo es distinto por clave y por
    /// variante (frecuencia, timbre o envolvente). Nunca pisa un archivo que ya exista, para no
    /// borrar el trabajo de sonido real cuando lo reemplacen. Los .meta (y sus ajustes de
    /// importacion, ver AudioImportRules) se conservan tal cual estan si el WAV ya existia.
    /// </summary>
    public static class PlaceholderAudioGenerator
    {
        const int SampleRate = 44100;
        const float MusicSeconds = 16f;
        const float AmbienceSeconds = 10f;
        const float LoopSfxSeconds = 4f;
        const float EdgeFadeSeconds = 0.005f; // 5 ms: evita el clic al empalmar el loop.

        const string Root = "Assets/_Zombineta/Audio/";
        const string Musica = Root + "Musica/";
        const string Sfx = Root + "Sfx/";
        const string Ambiente = Root + "Ambiente/";

        static int created;
        static int skipped;

        [MenuItem("Zombineta/Audio/Generar placeholders")]
        public static void Generar()
        {
            created = 0;
            skipped = 0;

            GenerarMusica();
            GenerarAmbiente();
            GenerarEfectos();

            AssetDatabase.Refresh();
            Debug.Log("PlaceholderAudioGenerator: " + created + " archivo(s) creado(s), " +
                skipped + " ya existian y no se tocaron.");
        }

        // ---------------------------------------------------------------- Musica (16 s, tema+tension con el mismo largo)

        static void GenerarMusica()
        {
            // rootFreq, semitonos del arpegio (sobre una octava arriba de la raiz), segundos por nota.
            Escribir(Musica + "mus_menu_loop.wav", Tema(110.00f, new[] { 0, 4, 7, 12 }, 2f));
            Escribir(Musica + "mus_character_loop.wav", Tema(146.83f, new[] { 0, 3, 7, 10 }, 1.6f));
            Escribir(Musica + "mus_cinematic_loop.wav", Tema(123.47f, new[] { 0, 3, 7, 10 }, 2.667f));
            Escribir(Musica + "mus_levelcomplete_loop.wav", Tema(196.00f, new[] { 0, 4, 7, 12, 16 }, 1f));
            Escribir(Musica + "mus_gameover_loop.wav", Tema(82.41f, new[] { 0, 3, 6, 10 }, 4f));
            Escribir(Musica + "mus_ending_loop.wav", Tema(174.61f, new[] { 0, 4, 7, 11 }, 2f));
            Escribir(Musica + "mus_credits_loop.wav", Tema(220.00f, new[] { 0, 4, 7, 12 }, 2f));

            Escribir(Musica + "mus_level1_loop.wav", Tema(65.41f, new[] { 0, 3, 5, 7, 10 }, 1.6f));
            Escribir(Musica + "mus_level1_tension.wav", Tension(101));

            Escribir(Musica + "mus_level2_loop.wav", Tema(73.42f, new[] { 0, 3, 7, 9 }, 2f));
            Escribir(Musica + "mus_level2_tension.wav", Tension(202));
        }

        /// <summary>Un dron grave (raiz + quinta suave) mas un arpegio lento una octava arriba. Distinto por raiz/patron/tempo.</summary>
        static float[] Tema(float rootFreq, int[] semitonos, float segundosPorNota)
        {
            var s = NuevoBuffer(MusicSeconds);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;

                float droneLfo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.1f * t);
                float drone = (float)Math.Sin(2.0 * Math.PI * rootFreq * t) +
                    (float)Math.Sin(2.0 * Math.PI * rootFreq * 1.5 * t) * 0.3f;
                drone *= droneLfo;

                int notaIdx = ((int)(t / segundosPorNota)) % semitonos.Length;
                float tn = t - (int)(t / segundosPorNota) * segundosPorNota;
                float notaFreq = rootFreq * 2f * Mathf.Pow(2f, semitonos[notaIdx] / 12f);
                float arpEnv = EnvAD(tn, 0.02f, 3f);
                float arp = (float)Math.Sin(2.0 * Math.PI * notaFreq * tn) * arpEnv;

                s[i] = drone * 0.5f + arp * 0.4f;
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        /// <summary>Rafagas de ruido en el pulso (percutivo), con un golpe grave debajo. Mismo largo exacto que Tema().</summary>
        static float[] Tension(int seed, float pulsoSegundos = 0.5f)
        {
            var s = NuevoBuffer(MusicSeconds); // mismo SamplesFor(MusicSeconds) que Tema(): largo identico.
            var rng = new System.Random(seed);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float tb = t % pulsoSegundos;
                float env = EnvAD(tb, 0.003f, 18f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float sub = (float)Math.Sin(2.0 * Math.PI * 70.0 * t) * 0.3f;
                s[i] = (noise * 0.7f + sub) * env;
            }
            NormalizarPico(s, 0.7f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        // ---------------------------------------------------------------- Ambiente (10 s)

        static void GenerarAmbiente()
        {
            Escribir(Ambiente + "amb_level1_city.wav", AmbienteCiudad(301));
            Escribir(Ambiente + "amb_level2_bridge.wav", AmbientePuente(302));
        }

        static float[] AmbienteCiudad(int seed)
        {
            var s = NuevoBuffer(AmbienceSeconds);
            var rng = new System.Random(seed);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float hum = (float)Math.Sin(2.0 * Math.PI * 55.0 * t) * 0.15f;
                float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.05f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = (hum + noise * 0.25f) * lfo;
            }
            s = PasaBajos(s, 5);
            NormalizarPico(s, 0.65f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        static float[] AmbientePuente(int seed)
        {
            int n = SamplesFor(AmbienceSeconds);
            var rng = new System.Random(seed);
            var raw = new float[n];
            for (int i = 0; i < n; i++)
                raw[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            var s = PasaAltos(raw);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.08f * t);
                float drone = (float)Math.Sin(2.0 * Math.PI * 40.0 * t) * 0.08f;
                s[i] = s[i] * 0.5f * lfo + drone;
            }
            NormalizarPico(s, 0.65f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        // ---------------------------------------------------------------- Efectos (0,1-1,5 s; loops de 4 s)

        static void GenerarEfectos()
        {
            // Disparo: rafaga percutiva con golpe grave.
            Escribir(Sfx + "sfx_player_shot_01.wav", RafagaRuido(1, 0.15f, 25f, 90f, 0.4f, 0));
            Escribir(Sfx + "sfx_player_shot_02.wav", RafagaRuido(2, 0.13f, 30f, 100f, 0.35f, 0));

            // Sin balas: click seco y corto.
            Escribir(Sfx + "sfx_player_noammo_01.wav", Click(1200f, 0.12f, 40f, true));
            Escribir(Sfx + "sfx_player_noammo_02.wav", Click(1400f, 0.10f, 45f, true));

            // Cambio de carril: barrido rapido.
            Escribir(Sfx + "sfx_player_lane_01.wav", Barrido(500f, 900f, 0.15f, false));
            Escribir(Sfx + "sfx_player_lane_02.wav", Barrido(550f, 1000f, 0.13f, false));

            // Choque: rafaga larga con golpe grave, algo enmuflada.
            Escribir(Sfx + "sfx_player_crash_01.wav", RafagaRuido(3, 0.6f, 6f, 70f, 0.5f, 4));
            Escribir(Sfx + "sfx_player_crash_02.wav", RafagaRuido(4, 0.55f, 7f, 80f, 0.45f, 4));

            // Atropello: golpe corto y sordo.
            Escribir(Sfx + "sfx_zombie_runover_01.wav", RafagaRuido(5, 0.3f, 12f, 55f, 0.6f, 2));
            Escribir(Sfx + "sfx_zombie_runover_02.wav", RafagaRuido(6, 0.28f, 13f, 60f, 0.55f, 2));

            // Explosion de barril: boom grande. Variante 3 exclusiva del banco de Nivel 2.
            Escribir(Sfx + "sfx_barrel_explosion_01.wav", RafagaRuido(7, 1.2f, 3f, 45f, 0.7f, 8));
            Escribir(Sfx + "sfx_barrel_explosion_02.wav", RafagaRuido(8, 1.1f, 3.3f, 50f, 0.65f, 8));
            Escribir(Sfx + "sfx_barrel_explosion_03.wav", RafagaRuido(9, 1.3f, 2.8f, 40f, 0.75f, 10));

            // Pickups: ping ascendente, un par de notas cortas.
            Escribir(Sfx + "sfx_pickup_fuel_01.wav", ArpegioCorto(new[] { 440f, 660f }, 0.12f, 20f));
            Escribir(Sfx + "sfx_pickup_fuel_02.wav", ArpegioCorto(new[] { 470f, 700f }, 0.11f, 22f));
            Escribir(Sfx + "sfx_pickup_battery_01.wav", ArpegioCorto(new[] { 523f, 784f }, 0.10f, 22f));
            Escribir(Sfx + "sfx_pickup_battery_02.wav", ArpegioCorto(new[] { 560f, 840f }, 0.09f, 24f));
            Escribir(Sfx + "sfx_pickup_ammo_01.wav", ArpegioCorto(new[] { 392f, 587f }, 0.10f, 22f));
            Escribir(Sfx + "sfx_pickup_ammo_02.wav", ArpegioCorto(new[] { 420f, 630f }, 0.09f, 24f));

            // Faros: barrido subiendo (on) o bajando (off).
            Escribir(Sfx + "sfx_headlight_on_01.wav", Barrido(300f, 900f, 0.12f, false));
            Escribir(Sfx + "sfx_headlight_on_02.wav", Barrido(340f, 950f, 0.11f, false));
            Escribir(Sfx + "sfx_headlight_off_01.wav", Barrido(900f, 300f, 0.12f, false));
            Escribir(Sfx + "sfx_headlight_off_02.wav", Barrido(950f, 340f, 0.11f, false));

            // Sin nafta: sputter descendente, pulsado.
            Escribir(Sfx + "sfx_player_nofuel_01.wav", Sputter(220f, 70f, 0.5f, 8f));
            Escribir(Sfx + "sfx_player_nofuel_02.wav", Sputter(200f, 60f, 0.45f, 9f));

            // Salto: whoosh subiendo.
            Escribir(Sfx + "sfx_player_jump_01.wav", Barrido(300f, 700f, 0.2f, false));
            Escribir(Sfx + "sfx_player_jump_02.wav", Barrido(330f, 760f, 0.18f, false));

            // Aterrizaje: golpe corto.
            Escribir(Sfx + "sfx_player_land_01.wav", RafagaRuido(10, 0.15f, 20f, 90f, 0.5f, 0));
            Escribir(Sfx + "sfx_player_land_02.wav", RafagaRuido(11, 0.13f, 22f, 100f, 0.45f, 0));

            // Aterrizaje perfecto: campanita brillante.
            Escribir(Sfx + "sfx_player_landperfect_01.wav", Campana(880f, 0.25f, 3, 8f));
            Escribir(Sfx + "sfx_player_landperfect_02.wav", Campana(960f, 0.22f, 3, 9f));

            // Stingers de victoria/derrota.
            Escribir(Sfx + "sfx_stinger_win_01.wav", Arpegio(new[] { 523f, 659f, 784f, 1046f }, 0.18f, 5f));
            Escribir(Sfx + "sfx_stinger_win_02.wav", Arpegio(new[] { 549f, 692f, 823f, 1098f }, 0.16f, 5.5f));
            Escribir(Sfx + "sfx_stinger_lose_01.wav", Arpegio(new[] { 440f, 392f, 349f, 293f }, 0.25f, 4f));
            Escribir(Sfx + "sfx_stinger_lose_02.wav", Arpegio(new[] { 418f, 372f, 332f, 278f }, 0.23f, 4.5f));

            // Zombie gimiendo, con vibrato.
            Escribir(Sfx + "sfx_zombie_groan_01.wav", Gemido(21, 110f, 1.0f, 4f, 8f, 0.3f));
            Escribir(Sfx + "sfx_zombie_groan_02.wav", Gemido(22, 125f, 0.9f, 5f, 7f, 0.35f));

            // UI: variante unica, muy cortos.
            Escribir(Sfx + "sfx_ui_move_01.wav", Click(1000f, 0.10f, 55f, false));
            Escribir(Sfx + "sfx_ui_confirm_01.wav", Arpegio(new[] { 784f, 1046f }, 0.06f, 40f));
            Escribir(Sfx + "sfx_ui_back_01.wav", Arpegio(new[] { 784f, 523f }, 0.07f, 35f));

            // Loops continuos (4 s, variante unica).
            Escribir(Sfx + "sfx_engine_loop_01.wav", LoopMotor(60f, 4, 0.25f, 401));
            Escribir(Sfx + "sfx_horde_loop_01.wav", LoopHorda(0.5f, 140f, 402));

            // Aleatorios de pantalla.
            Escribir(Sfx + "sfx_screen_groan_01.wav", Gemido(23, 95f, 1.4f, 3f, 6f, 0.35f));
            Escribir(Sfx + "sfx_screen_groan_02.wav", Gemido(24, 105f, 1.5f, 3.5f, 7f, 0.3f));
            Escribir(Sfx + "sfx_screen_wind_01.wav", RafagaViento(403, 1.5f));
        }

        static float[] RafagaRuido(int seed, float duracion, float decayRate, float subFreq, float subAmt, int pasaBajos)
        {
            var rng = new System.Random(seed);
            var s = NuevoBuffer(duracion);
            double subPhase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = EnvAD(t, 0.003f, decayRate);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                subPhase += subFreq / SampleRate;
                float sub = (float)Math.Sin(subPhase * 2.0 * Math.PI);
                s[i] = (noise * (1f - subAmt) + sub * subAmt) * env;
            }
            if (pasaBajos > 1)
                s = PasaBajos(s, pasaBajos);
            NormalizarPico(s, 0.8f);
            AplicarFade(s, 0.003f);
            return s;
        }

        static float[] Click(float freq, float duracion, float decayRate, bool cuadrada)
        {
            var s = NuevoBuffer(duracion);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                phase += freq / SampleRate;
                float onda = cuadrada
                    ? Mathf.Sign((float)Math.Sin(phase * 2.0 * Math.PI))
                    : (float)Math.Sin(phase * 2.0 * Math.PI);
                s[i] = onda * EnvAD(t, 0.002f, decayRate);
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, 0.003f);
            return s;
        }

        static float[] Barrido(float freqInicio, float freqFin, float duracion, bool cuadrada)
        {
            var s = NuevoBuffer(duracion);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(freqInicio, freqFin, t / duracion);
                phase += f / SampleRate;
                float onda = cuadrada
                    ? Mathf.Sign((float)Math.Sin(phase * 2.0 * Math.PI))
                    : (float)Math.Sin(phase * 2.0 * Math.PI);
                s[i] = onda * EnvASR(t, duracion, 0.01f, duracion * 0.6f);
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, 0.003f);
            return s;
        }

        static float[] Sputter(float freqInicio, float freqFin, float duracion, float pulsoHz)
        {
            var s = NuevoBuffer(duracion);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(freqInicio, freqFin, t / duracion);
                phase += f / SampleRate;
                float onda = Mathf.Sign((float)Math.Sin(phase * 2.0 * Math.PI));
                float pulso = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * pulsoHz * t));
                s[i] = onda * pulso * EnvASR(t, duracion, 0.01f, duracion * 0.3f);
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, 0.003f);
            return s;
        }

        static float[] Campana(float freq, float duracion, int armonicos, float decayRate)
        {
            var s = NuevoBuffer(duracion);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int h = 1; h <= armonicos; h++)
                    v += (float)Math.Sin(2.0 * Math.PI * freq * h * t) * (1f / h) * EnvAD(t, 0.005f, decayRate * h * 0.4f + decayRate);
                s[i] = v / armonicos;
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, 0.003f);
            return s;
        }

        /// <summary>Un par de notas cortas ascendentes: el "ding" de un pickup.</summary>
        static float[] ArpegioCorto(float[] freqs, float notaSegundos, float decayRate) => Arpegio(freqs, notaSegundos, decayRate);

        static float[] Arpegio(float[] freqs, float notaSegundos, float decayRate, float release = 0.2f)
        {
            float total = freqs.Length * notaSegundos + release;
            var s = NuevoBuffer(total);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                int nota = Mathf.Min((int)(t / notaSegundos), freqs.Length - 1);
                float tn = t - nota * notaSegundos;
                float v = (float)Math.Sin(2.0 * Math.PI * freqs[nota] * tn);
                s[i] = v * EnvAD(tn, 0.005f, decayRate);
            }
            NormalizarPico(s, 0.75f);
            AplicarFade(s, 0.003f);
            return s;
        }

        static float[] Gemido(int seed, float freq, float duracion, float vibratoHz, float vibratoDepth, float noiseMix)
        {
            var rng = new System.Random(seed);
            var s = NuevoBuffer(duracion);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float vib = Mathf.Sin(2f * Mathf.PI * vibratoHz * t) * vibratoDepth;
                float f = freq + vib;
                float tono = (float)Math.Sin(2.0 * Math.PI * f * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float env = EnvASR(t, duracion, 0.08f, 0.3f);
                s[i] = (tono * (1f - noiseMix) + noise * noiseMix) * env;
            }
            NormalizarPico(s, 0.7f);
            AplicarFade(s, 0.005f);
            return s;
        }

        static float[] RafagaViento(int seed, float duracion)
        {
            int n = SamplesFor(duracion);
            var rng = new System.Random(seed);
            var raw = new float[n];
            for (int i = 0; i < n; i++)
                raw[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            var s = PasaAltos(raw);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                s[i] *= EnvASR(t, duracion, duracion * 0.3f, duracion * 0.4f);
            }
            NormalizarPico(s, 0.7f);
            AplicarFade(s, 0.005f);
            return s;
        }

        /// <summary>Loop continuo de motor: unos pocos armonicos de una fundamental grave, algo de ruido.</summary>
        static float[] LoopMotor(float freq, int armonicos, float noiseMix, int seed)
        {
            var rng = new System.Random(seed);
            var s = NuevoBuffer(LoopSfxSeconds);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = 0f;
                for (int h = 1; h <= armonicos; h++)
                    v += (float)Math.Sin(2.0 * Math.PI * freq * h * t) * (1f / h);
                v /= armonicos;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = v * (1f - noiseMix) + noise * noiseMix * 0.3f;
            }
            s = PasaBajos(s, 6);
            NormalizarPico(s, 0.7f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        /// <summary>Loop continuo de la horda: ruido con un tono grave modulado lentamente.</summary>
        static float[] LoopHorda(float noiseMix, float groanFreq, int seed)
        {
            var rng = new System.Random(seed);
            var s = NuevoBuffer(LoopSfxSeconds);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float tono = (float)Math.Sin(2.0 * Math.PI * groanFreq * t) * 0.4f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = (tono * (1f - noiseMix) + noise * noiseMix) * lfo;
            }
            s = PasaBajos(s, 4);
            NormalizarPico(s, 0.7f);
            AplicarFade(s, EdgeFadeSeconds);
            return s;
        }

        // ---------------------------------------------------------------- Utilidades de sintesis

        static int SamplesFor(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
        static float[] NuevoBuffer(float seconds) => new float[SamplesFor(seconds)];

        /// <summary>Ataque lineal y despues caida exponencial: percusion, clicks, plucks.</summary>
        static float EnvAD(float t, float attack, float decayRate)
        {
            if (attack > 0f && t < attack)
                return t / attack;
            return Mathf.Exp(-decayRate * Mathf.Max(0f, t - attack));
        }

        /// <summary>Ataque y caida lineales con una meseta al medio: tonos sostenidos, gemidos, viento.</summary>
        static float EnvASR(float t, float duracion, float attack, float release)
        {
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            float r = release > 0f ? Mathf.Clamp01((duracion - t) / release) : 1f;
            return Mathf.Min(a, r);
        }

        static void AplicarFade(float[] s, float fadeSeconds)
        {
            int n = Mathf.Min(SamplesFor(fadeSeconds), s.Length / 2);
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / n;
                s[i] *= f;
                s[s.Length - 1 - i] *= f;
            }
        }

        static void NormalizarPico(float[] s, float objetivo)
        {
            float max = 0f;
            for (int i = 0; i < s.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(s[i]));
            if (max < 1e-5f)
                return;
            float k = objetivo / max;
            for (int i = 0; i < s.Length; i++)
                s[i] *= k;
        }

        /// <summary>Promedio movil simple: suaviza (pasabajos) el ruido crudo.</summary>
        static float[] PasaBajos(float[] s, int ventana)
        {
            var o = new float[s.Length];
            var buf = new float[ventana];
            int idx = 0;
            float suma = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                suma -= buf[idx];
                buf[idx] = s[i];
                suma += s[i];
                idx = (idx + 1) % ventana;
                o[i] = suma / ventana;
            }
            return o;
        }

        /// <summary>Diferencia entre muestras consecutivas: realza los agudos (viento).</summary>
        static float[] PasaAltos(float[] s)
        {
            var o = new float[s.Length];
            float prev = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                o[i] = (s[i] - prev) * 0.9f;
                prev = s[i];
            }
            return o;
        }

        // ---------------------------------------------------------------- Escritura del WAV

        /// <summary>Escribe el WAV solo si 'assetPath' todavia no existe: nunca pisa un archivo real.</summary>
        static void Escribir(string assetPath, float[] samples)
        {
            string abs = ARutaAbsoluta(assetPath);
            if (File.Exists(abs))
            {
                skipped++;
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            EscribirWav(abs, samples, SampleRate);
            created++;
        }

        static string ARutaAbsoluta(string assetPath)
        {
            string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>PCM 16 bit mono, cabecera RIFF/WAVE escrita a mano.</summary>
        static void EscribirWav(string path, float[] samples, int sampleRate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                const int bitsPerSample = 16;
                const int channels = 1;
                int blockAlign = channels * bitsPerSample / 8;
                int byteRate = sampleRate * blockAlign;
                int dataSize = samples.Length * blockAlign;

                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));

                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // tamanio del chunk fmt para PCM
                bw.Write((short)1); // 1 = PCM
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write((short)bitsPerSample);

                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < samples.Length; i++)
                {
                    short v = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(v);
                }
            }
        }
    }
}
