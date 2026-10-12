using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;

namespace Zombineta.Fx
{
    /// <summary>
    /// Traduce los eventos de la horda en cosas que se ven: fogonazo y traza del disparo,
    /// particulas de impacto, muerte, atropello y explosion, y manchas en el asfalto. Todo con
    /// pool: en partida no se instancia nada.
    ///
    /// Cada efecto es un prefab por capas (destello, nucleo, humo, chispas, gotas...): el
    /// sistema raiz y sus hijos se reproducen juntos, y cada capa conserva el orden de dibujo
    /// relativo con el que se armo (el humo detras, el destello adelante).
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
        [Tooltip("Fogonazo en la boca del arma, en cada disparo.")]
        [SerializeField] ParticleSystem muzzlePrefab;
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
        [Tooltip("Opcional: forma de la mancha de sangre. Vacio: la del prefab.")]
        [SerializeField] Sprite bloodSprite;
        [Tooltip("Opcional: quemadura que deja la explosion en el asfalto.")]
        [SerializeField] Sprite scorchSprite;
        [SerializeField] Color scorchColor = new Color(0.05f, 0.04f, 0.04f, 0.8f);
        [SerializeField] float scorchScale = 2.2f;

        /// <summary>Un efecto del pool: sus capas, el color y el orden de dibujo con que se armaron.</summary>
        sealed class Instancia
        {
            public ParticleSystem raiz;
            public ParticleSystem[] capas;
            public ParticleSystem.MinMaxGradient[] colores;
            public Renderer[] renderers;
            public int[] ordenes;
        }

        readonly List<Instancia> impacts = new List<Instancia>();
        readonly List<Instancia> deaths = new List<Instancia>();
        readonly List<Instancia> runOvers = new List<Instancia>();
        readonly List<Instancia> explosions = new List<Instancia>();
        readonly List<Instancia> muzzles = new List<Instancia>();

        SpriteRenderer[] decals;
        Sprite decalSprite;
        int nextDecal;
        int lastEpoch = -1;
        float tracerLeft;
        MaterialPropertyBlock tracerBlock;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            Fill(impacts, impactPrefab);
            Fill(deaths, deathPrefab);
            Fill(runOvers, runOverPrefab);
            Fill(explosions, explosionPrefab);
            Fill(muzzles, muzzlePrefab);

            if (decalPrefab != null)
            {
                decalSprite = decalPrefab.sprite;
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

        void Fill(List<Instancia> pool, ParticleSystem prefab)
        {
            if (prefab == null)
                return;
            for (int i = 0; i < poolPerKind; i++)
            {
                var raiz = Instantiate(prefab, transform);
                var capas = raiz.GetComponentsInChildren<ParticleSystem>(true);
                var colores = new ParticleSystem.MinMaxGradient[capas.Length];
                for (int c = 0; c < capas.Length; c++)
                    colores[c] = capas[c].main.startColor;

                var renderers = raiz.GetComponentsInChildren<ParticleSystemRenderer>(true);
                var ordenes = new int[renderers.Length];
                for (int r = 0; r < renderers.Length; r++)
                    ordenes[r] = renderers[r].sortingOrder;

                pool.Add(new Instancia
                {
                    raiz = raiz, capas = capas, colores = colores, renderers = renderers, ordenes = ordenes,
                });
            }
        }

        void LateUpdate()
        {
            UpdateTracer();

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
            // Sin sangre alta, la sangre sale como polvo gris; el resto conserva sus colores.
            Color? tinte = gore ? (Color?)null : dustColor;

            switch (e.Kind)
            {
                case HordeEventKind.Tracer:
                    ShowTracer(e);
                    Emit(muzzles, new Vector3(run.ToWorldX(e.FromX), run.LaneToWorldY(e.Lane) + tracerHeight, 0f),
                         null, e.Lane);
                    break;

                case HordeEventKind.Impact:
                    Emit(impacts, at, tinte, e.Lane);
                    break;

                case HordeEventKind.Death:
                    Emit(e.Cause == DeathCause.RunOver ? runOvers : deaths, at, tinte, e.Lane);
                    if (gore)
                        Stain(new Vector3(at.x, run.LaneToWorldY(e.Lane), 0f), e.Lane,
                              bloodSprite != null ? bloodSprite : decalSprite, bloodColor, 1f);
                    break;

                case HordeEventKind.Explosion:
                    Emit(explosions, at, null, e.Lane);
                    if (scorchSprite != null)
                        Stain(new Vector3(at.x, run.LaneToWorldY(e.Lane), 0f), e.Lane, scorchSprite, scorchColor, scorchScale);
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
            tracer.sortingLayerID = LaneSorting.GameLayerId;
            tracer.enabled = true;
            tracerLeft = tracerSeconds;
            SetTracerAlpha(1f);
        }

        /// <summary>La traza se desvanece en vez de apagarse de golpe.</summary>
        void UpdateTracer()
        {
            if (tracerLeft <= 0f || tracer == null)
                return;

            tracerLeft -= Time.unscaledDeltaTime;
            if (tracerLeft <= 0f)
            {
                tracer.enabled = false;
                return;
            }
            float t = tracerSeconds > 0f ? tracerLeft / tracerSeconds : 0f;
            SetTracerAlpha(t * t);
        }

        void SetTracerAlpha(float alpha)
        {
            tracerBlock ??= new MaterialPropertyBlock();
            tracer.GetPropertyBlock(tracerBlock);
            tracerBlock.SetColor(BaseColorId, new Color(1f, 1f, 1f, alpha));
            tracer.SetPropertyBlock(tracerBlock);
        }

        void Emit(List<Instancia> pool, Vector3 at, Color? tinte, int lane)
        {
            if (pool.Count == 0)
                return;

            // El que este libre (ninguna capa con particulas vivas: el humo dura mas que el
            // destello); si estan todos ocupados, se reusa el primero.
            Instancia chosen = pool[0];
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].raiz.IsAlive(true))
                {
                    chosen = pool[i];
                    break;
                }

            chosen.raiz.transform.position = at;

            for (int c = 0; c < chosen.capas.Length; c++)
            {
                var main = chosen.capas[c].main;
                main.startColor = tinte.HasValue ? new ParticleSystem.MinMaxGradient(tinte.Value) : chosen.colores[c];
            }

            int baseOrder = LaneSorting.Order(lane, SortSlot.Effect);
            for (int r = 0; r < chosen.renderers.Length; r++)
            {
                chosen.renderers[r].sortingOrder = baseOrder + chosen.ordenes[r];
                chosen.renderers[r].sortingLayerID = LaneSorting.GameLayerId;
            }

            chosen.raiz.Clear(true);
            chosen.raiz.Play(true);
        }

        void Stain(Vector3 at, int lane, Sprite sprite, Color color, float scale)
        {
            if (decals == null || decals.Length == 0)
                return;
            var d = decals[nextDecal];
            nextDecal = (nextDecal + 1) % decals.Length;
            d.transform.position = at;
            // La escala del prefab es la base (acompana el tamano de los personajes); el azar solo la varia.
            // Otra forma de mancha se lleva al ancho de la del prefab, sea cual sea su resolucion.
            float ajuste = sprite != null && decalSprite != null && sprite.bounds.size.x > 0f
                ? decalSprite.bounds.size.x / sprite.bounds.size.x
                : 1f;
            d.transform.localScale = decalPrefab.transform.localScale * (ajuste * scale * Random.Range(0.7f, 1.3f));
            d.flipX = Random.value < 0.5f;
            if (sprite != null)
                d.sprite = sprite;
            d.color = color;
            // Mancha en el asfalto: se pisa, como la rampa.
            d.sortingOrder = LaneSorting.Order(lane, SortSlot.Shadow) + 1;
            d.sortingLayerID = LaneSorting.GameLayerId;
            d.enabled = true;
        }
    }
}
