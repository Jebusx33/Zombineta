using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Fx
{
    /// <summary>
    /// Traduce los eventos de la horda en cosas que se ven: traza del disparo, particulas de
    /// impacto, muerte, atropello y explosion, y manchas en el asfalto. Todo con pool: en
    /// partida no se instancia nada.
    ///
    /// Corre despues de la simulacion y lee la lista de eventos del tick. Si no hubo tick
    /// (pausa, Game Over), el Epoch no cambia y no se repite nada.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class FxManager : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Header("Particulas")]
        [SerializeField] ParticleSystem impactPrefab;
        [SerializeField] ParticleSystem deathPrefab;
        [SerializeField] ParticleSystem runOverPrefab;
        [SerializeField] ParticleSystem explosionPrefab;
        [SerializeField] int poolPerKind = 6;

        [Header("Traza del disparo")]
        [SerializeField] LineRenderer tracer;
        [SerializeField] float tracerSeconds = 0.05f;

        [Header("Manchas")]
        [SerializeField] SpriteRenderer decalPrefab;
        [SerializeField] int decalCount = 30;
        [SerializeField] Color bloodColor = new Color(0.55f, 0.05f, 0.07f, 0.85f);
        [SerializeField] Color dustColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        readonly List<ParticleSystem> impacts = new List<ParticleSystem>();
        readonly List<ParticleSystem> deaths = new List<ParticleSystem>();
        readonly List<ParticleSystem> runOvers = new List<ParticleSystem>();
        readonly List<ParticleSystem> explosions = new List<ParticleSystem>();

        SpriteRenderer[] decals;
        int nextDecal;
        int lastEpoch = -1;
        float tracerLeft;

        void Start()
        {
            Fill(impacts, impactPrefab);
            Fill(deaths, deathPrefab);
            Fill(runOvers, runOverPrefab);
            Fill(explosions, explosionPrefab);

            if (decalPrefab != null)
            {
                decals = new SpriteRenderer[decalCount];
                for (int i = 0; i < decalCount; i++)
                {
                    decals[i] = Instantiate(decalPrefab, transform);
                    decals[i].enabled = false;
                }
            }

            if (tracer != null)
                tracer.enabled = false;
        }

        void Fill(List<ParticleSystem> pool, ParticleSystem prefab)
        {
            if (prefab == null)
                return;
            for (int i = 0; i < poolPerKind; i++)
                pool.Add(Instantiate(prefab, transform));
        }

        void LateUpdate()
        {
            if (tracerLeft > 0f)
            {
                tracerLeft -= Time.unscaledDeltaTime;
                if (tracerLeft <= 0f && tracer != null)
                    tracer.enabled = false;
            }

            if (run == null || run.Sim == null)
                return;

            var horde = run.Sim.Horde;
            if (horde.Epoch == lastEpoch)
                return;
            lastEpoch = horde.Epoch;

            bool gore = GameSettings.Gore;
            for (int i = 0; i < horde.Events.Count; i++)
                Play(horde.Events[i], gore);
        }

        void Play(HordeEvent e, bool gore)
        {
            Vector3 at = new Vector3(run.ToWorldX(e.X), run.LaneToWorldY(e.Lane) + 0.5f, 0f);

            switch (e.Kind)
            {
                case HordeEventKind.Tracer:
                    ShowTracer(e);
                    break;

                case HordeEventKind.Impact:
                    Emit(impacts, at, gore ? bloodColor : dustColor);
                    break;

                case HordeEventKind.Death:
                    Emit(e.Cause == DeathCause.RunOver ? runOvers : deaths, at,
                         gore ? bloodColor : dustColor);
                    if (gore)
                        Stain(new Vector3(at.x, run.LaneToWorldY(e.Lane), 0f));
                    break;

                case HordeEventKind.Explosion:
                    Emit(explosions, at, new Color(1f, 0.75f, 0.3f, 1f));
                    break;

                case HordeEventKind.Miss:
                    // La bala perdida no deja nada: la traza ya conto la historia.
                    break;
            }
        }

        void ShowTracer(HordeEvent e)
        {
            if (tracer == null)
                return;
            float y = run.LaneToWorldY(e.Lane) + 0.55f;
            tracer.SetPosition(0, new Vector3(run.ToWorldX(e.FromX), y, 0f));
            tracer.SetPosition(1, new Vector3(run.ToWorldX(e.X), y, 0f));
            tracer.enabled = true;
            tracerLeft = tracerSeconds;
        }

        void Emit(List<ParticleSystem> pool, Vector3 at, Color color)
        {
            if (pool.Count == 0)
                return;

            // El que este libre; si estan todos ocupados, se reusa el primero.
            ParticleSystem chosen = pool[0];
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].isPlaying)
                {
                    chosen = pool[i];
                    break;
                }

            chosen.transform.position = at;
            var main = chosen.main;
            main.startColor = color;
            chosen.Clear();
            chosen.Play();
        }

        void Stain(Vector3 at)
        {
            if (decals == null || decals.Length == 0)
                return;
            var d = decals[nextDecal];
            nextDecal = (nextDecal + 1) % decals.Length;
            d.transform.position = at;
            d.transform.localScale = Vector3.one * Random.Range(0.7f, 1.3f);
            d.color = bloodColor;
            d.enabled = true;
        }
    }
}
