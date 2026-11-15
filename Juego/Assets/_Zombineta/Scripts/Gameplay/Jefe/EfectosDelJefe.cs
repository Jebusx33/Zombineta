using UnityEngine;
using Zombineta.Core;
using Zombineta.Fx;

namespace Zombineta.Jefe
{
    /// <summary>
    /// Las particulas del duelo: el humo del tanque (bastante mas, y hollin, cuando acelera para la
    /// embestida) y el plasma de sus proyectiles: la salpicadura en cada disparo, la estela de gotas en
    /// vuelo y el reventon con salpicadura y charco donde pegan. Solo lee la simulacion y la vista,
    /// que ya dibujo este cuadro (orden 20, como FxManager): nada de esto cambia el juego.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class EfectosDelJefe : MonoBehaviour
    {
        // Las capas del prefab del humo y de la estela, en el orden en que las arma FxBuilder.
        const int CapaHumo = 0, CapaHollin = 1;
        const int CapaGotas = 0, CapaVaho = 1;

        [SerializeField] RunController run;
        [SerializeField] VistaDelJefe vista;

        [Header("Humo del tanque")]
        [Tooltip("Raiz: el humo de siempre. Hijo Hollin: bocanadas negras al acelerar.")]
        [SerializeField] ParticleSystem humoPrefab;
        [Tooltip("De donde sale el humo, desde el apoyo de las orugas, en unidades de mundo del dibujo: arriba de la parte de atras del tanque.")]
        [SerializeField] Vector2 escape = new Vector2(-1.45f, 1.05f);
        [Tooltip("Bocanadas por segundo mientras rueda.")]
        [SerializeField] float humoRodando = 14f;
        [Tooltip("Bocanadas por segundo con el motor a fondo: al final del aviso y en la embestida.")]
        [SerializeField] float humoAFondo = 42f;
        [Tooltip("Bocanadas de hollin por segundo a fondo.")]
        [SerializeField] float hollinAFondo = 26f;

        [Header("Plasma")]
        [Tooltip("Salpicadura en la boca del brazo con cada disparo.")]
        [SerializeField] ParticleSystem disparoPrefab;
        [Tooltip("Gotas que va soltando el proyectil en vuelo.")]
        [SerializeField] ParticleSystem estelaPrefab;
        [Tooltip("Reventon donde pega: gotas, salpicadura y charco.")]
        [SerializeField] ParticleSystem impactoPrefab;
        [Tooltip("Gotas por segundo de la estela.")]
        [SerializeField] float gotasPorSegundo = 80f;
        [Tooltip("Metros detras del centro del proyectil de donde caen las gotas: la cola.")]
        [SerializeField] float colaDelProyectil = 0.35f;
        [Tooltip("Sin sangre alta, el plasma sale de este color.")]
        [SerializeField] Color sinSangre = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        EfectoEnCapas humo;
        readonly EfectoEnCapas[] disparos = new EfectoEnCapas[JefeSimulation.MaxProyectiles];
        readonly EfectoEnCapas[] estelas = new EfectoEnCapas[JefeSimulation.MaxProyectiles];
        readonly EfectoEnCapas[] impactos = new EfectoEnCapas[JefeSimulation.MaxProyectiles];
        readonly int[] serieVista = new int[JefeSimulation.MaxProyectiles];
        readonly int[] impactoVisto = new int[JefeSimulation.MaxProyectiles];

        void Start()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();
            if (vista == null)
                vista = FindAnyObjectByType<VistaDelJefe>();

            if (humoPrefab != null)
                humo = new EfectoEnCapas(humoPrefab, transform);
            for (int i = 0; i < JefeSimulation.MaxProyectiles; i++)
            {
                if (disparoPrefab != null) disparos[i] = new EfectoEnCapas(disparoPrefab, transform);
                if (estelaPrefab != null) estelas[i] = new EfectoEnCapas(estelaPrefab, transform);
                if (impactoPrefab != null) impactos[i] = new EfectoEnCapas(impactoPrefab, transform);
            }
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || vista == null)
                return;

            var jefe = run.Sim.Jefe;
            Color? tinte = GameSettings.Gore ? (Color?)null : sinSangre;
            Humear(jefe);
            for (int i = 0; i < JefeSimulation.MaxProyectiles; i++)
                Plasma(i, jefe.Proyectiles[i], tinte);
        }

        void Humear(JefeSimulation jefe)
        {
            if (humo == null)
                return;

            bool visible = vista.CuerpoVisible;
            var (bocanadas, hollin) = visible
                ? Humo(jefe.Fase, jefe.TiempoFase, run.Config.jefe.avisoEmbestida, humoRodando, humoAFondo, hollinAFondo)
                : (0f, 0f);
            humo.Tasa(CapaHumo, bocanadas);
            humo.Tasa(CapaHollin, hollin);
            if (!visible)
                return;

            humo.Encender();
            humo.Transform.position = vista.EnElCuerpo(escape);
            // Detras del jefe: el humo sale de atras del tanque y no le tapa el cuerpo.
            humo.Ordenar(vista.OrdenDelCuerpo - LaneSorting.SlotWidth);
        }

        /// <summary>
        /// Bocanadas por segundo del humo y del hollin en cada momento del duelo. En el aviso de la
        /// embestida el motor sube de a poco hasta el maximo, que dura la embestida; aturdido apenas
        /// humea y derrotado queda echando humo mientras sigue en pantalla.
        /// </summary>
        public static (float humo, float hollin) Humo(FaseDeJefe fase, float tiempoFase, float segundosAviso,
                                                      float rodando, float aFondo, float hollinAFondo)
        {
            switch (fase)
            {
                case FaseDeJefe.Inactivo:
                    return (0f, 0f);
                case FaseDeJefe.Entrada:
                    return (Mathf.Lerp(rodando, aFondo, 0.4f), 0f);
                case FaseDeJefe.AvisoEmbestida:
                    float k = segundosAviso > 0f ? Mathf.Clamp01(tiempoFase / segundosAviso) : 1f;
                    return (Mathf.Lerp(rodando, aFondo, k), hollinAFondo * k * k);
                case FaseDeJefe.Embestida:
                    return (aFondo, hollinAFondo);
                case FaseDeJefe.Regreso:
                    return (Mathf.Lerp(rodando, aFondo, 0.3f), 0f);
                case FaseDeJefe.Aturdido:
                    return (rodando * 0.35f, 0f);
                case FaseDeJefe.Derrotado:
                    return (aFondo * 0.6f, hollinAFondo * 0.5f);
                default:
                    return (rodando, 0f);
            }
        }

        void Plasma(int i, ProyectilDelJefe p, Color? tinte)
        {
            bool dibujado = vista.ProyectilDibujado(i, out var posicion, out float angulo);
            int orden = LaneSorting.Order(p.Carril, SortSlot.Effect);

            // Disparo nuevo: salpica en la boca, que es donde la vista lo dibujo en su primer cuadro.
            if (serieVista[i] != p.Serie)
            {
                bool nuevo = p.Serie > serieVista[i] && dibujado;
                serieVista[i] = p.Serie;
                if (nuevo && disparos[i] != null)
                {
                    var d = disparos[i];
                    d.Tenir(tinte);
                    d.Ordenar(orden);
                    d.Reproducir(posicion, angulo, run.LaneToWorldY(p.Carril));
                }
            }

            var estela = estelas[i];
            if (estela != null)
            {
                estela.Tasa(CapaGotas, dibujado ? gotasPorSegundo : 0f);
                estela.Tasa(CapaVaho, dibujado ? gotasPorSegundo * 0.3f : 0f);
                if (dibujado)
                {
                    var atras = Quaternion.Euler(0f, 0f, angulo) * Vector3.left * colaDelProyectil;
                    estela.Transform.SetPositionAndRotation(posicion + atras, Quaternion.Euler(0f, 0f, angulo));
                    estela.Tenir(tinte);
                    estela.Ordenar(orden - 1);
                    estela.Encender();
                }
            }

            if (impactoVisto[i] != p.SerieImpacto)
            {
                bool nuevo = p.SerieImpacto > impactoVisto[i] && p.ImpactoRestante > 0f;
                impactoVisto[i] = p.SerieImpacto;
                if (nuevo && impactos[i] != null)
                {
                    var im = impactos[i];
                    im.Tenir(tinte);
                    im.Ordenar(orden);
                    // En el piso salpica hacia arriba; contra la moto, hacia adelante y arriba.
                    im.Reproducir(vista.PosicionDelImpacto(p), p.ImpactoEnElPiso ? 0f : -35f, run.LaneToWorldY(p.Carril));
                }
            }
        }
    }
}
