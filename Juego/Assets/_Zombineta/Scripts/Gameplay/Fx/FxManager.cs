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

        [Tooltip("Altura sobre el piso del carril de donde sale la traza (la mano de la jugadora), en unidades de mundo.")]
        [SerializeField] float tracerHeight = 0.55f;

        [Tooltip("Altura sobre el piso del carril donde nacen las particulas (el cuerpo del zombie), en unidades de mundo.")]
        [SerializeField] float effectHeight = 0.5f;

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
            Vector3 at = new Vector3(run.ToWorldX(e.X), run.LaneToWorldY(e.Lane) + effectHeight, 0f);

            switch (e.Kind)
            {
                case HordeEventKind.Tracer:
                    ShowTracer(e);
                    break;

                case HordeEventKind.Impact:
                    Emit(impacts, at, gore ? bloodColor : dustColor, e.Lane);
                    break;

                case HordeEventKind.Death:
                    Emit(e.Cause == DeathCause.RunOver ? runOvers : deaths, at,
                         gore ? bloodColor : dustColor, e.Lane);
                    if (gore)
                        Stain(new Vector3(at.x, run.LaneToWorldY(e.Lane), 0f), e.Lane);
                    break;

                case HordeEventKind.Explosion:
                    Emit(explosions, at, new Color(1f, 0.75f, 0.3f, 1f), e.Lane);
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
            float y = run.LaneToWorldY(e.Lane) + tracerHeight;
            tracer.SetPosition(0, new Vector3(run.ToWorldX(e.FromX), y, 0f));
            tracer.SetPosition(1, new Vector3(run.ToWorldX(e.X), y, 0f));
            tracer.sortingOrder = LaneSorting.Order(e.Lane, SortSlot.Effect);
            tracer.sortingLayerName = LaneSorting.GameLayer;
            tracer.enabled = true;
            tracerLeft = tracerSeconds;
        }

        void Emit(List<ParticleSystem> pool, Vector3 at, Color color, int lane)
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
            var renderer = chosen.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = LaneSorting.Order(lane, SortSlot.Effect);
                renderer.sortingLayerName = LaneSorting.GameLayer;
            }
            chosen.Clear();
            chosen.Play();
        }

        void Stain(Vector3 at, int lane)
        {
            if (decals == null || decals.Length == 0)
                return;
            var d = decals[nextDecal];
            nextDecal = (nextDecal + 1) % decals.Length;
            d.transform.position = at;
            // La escala del prefab es la base (acompana el tamano de los personajes); el azar solo la varia.
            d.transform.localScale = decalPrefab.transform.localScale * Random.Range(0.7f, 1.3f);
            d.color = bloodColor;
            // Mancha en el asfalto: se pisa, como la rampa.
            d.sortingOrder = LaneSorting.Order(lane, SortSlot.Shadow) + 1;
            d.sortingLayerName = LaneSorting.GameLayer;
            d.enabled = true;
        }
    }
}
