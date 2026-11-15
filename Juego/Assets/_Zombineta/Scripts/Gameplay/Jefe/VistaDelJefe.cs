using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Luz;

namespace Zombineta.Jefe
{
    /// <summary>
    /// Lo que se ve del duelo: el jefe, sus proyectiles con el impacto, las marcas en el piso y lo que hay
    /// que recoger en el camino. Solo lee la simulacion: el cuadro de cada animacion sale del tiempo de la fase, asi que la
    /// pausa la congela y el reintento la limpia sin hacer nada aca.
    /// </summary>
    public sealed class VistaDelJefe : MonoBehaviour
    {
        const float AlturaSobreElCarril = 0.45f;

        [SerializeField] RunController run;
        [SerializeField] AnimacionesJefe animaciones;
        [Tooltip("El mismo material iluminado que usan los zombies: asi le pegan las luces de la autopista.")]
        [SerializeField] Material material;

        [Header("Tamano")]
        [Tooltip("Escala del dibujo. La logica y las colisiones no dependen de esto.")]
        [SerializeField] float escala = 1f;
        [SerializeField] float escalaProyectil = 1.5f;
        [Tooltip("Cuanto queda el apoyo de las orugas por debajo de la linea del carril, en unidades de mundo.")]
        [SerializeField] float apoyoBajoElCarril = 0.3f;

        [Header("Proyectil")]
        [Tooltip("Altura sobre el carril a la que llega el proyectil frontal, en unidades de mundo: el cuerpo de la moto.")]
        [SerializeField] float alturaDeContacto = 0.8f;
        [Tooltip("Altura del arco del proyectil elevado, en unidades de mundo.")]
        [SerializeField] float alturaDelArco = 5f;

        [Header("Avisos")]
        [SerializeField] Color colorAviso = new Color(1f, 0.15f, 0.1f, 0.55f);
        [Tooltip("Parpadeos por segundo de la marca en el piso.")]
        [SerializeField] float pulsoAviso = 3f;
        [Tooltip("Ancho de la marca del proyectil elevado, en unidades de mundo.")]
        [SerializeField] float anchoMarcaElevado = 2.6f;
        [Tooltip("Tinte que late mientras se lo puede lastimar.")]
        [SerializeField] Color colorAturdido = new Color(1f, 0.9f, 0.7f);
        [SerializeField] Color colorDano = new Color(1f, 0.45f, 0.4f);

        [Header("Recolectables")]
        [SerializeField] Sprite cajaDeBalas;
        [SerializeField] Sprite granadaSuelta;
        [SerializeField] float altoRecolectable = 0.8f;

        SpriteRenderer cuerpo, marcaCarril, caja, granada;
        // Uno por lugar de JefeSimulation.Proyectiles; las marcas de caida, una por mortero mas la del que apunta.
        readonly SpriteRenderer[] proyectiles = new SpriteRenderer[JefeSimulation.MaxProyectiles];
        readonly SpriteRenderer[] impactos = new SpriteRenderer[JefeSimulation.MaxProyectiles];
        readonly SpriteRenderer[] marcasPunto = new SpriteRenderer[JefeSimulation.MaxProyectiles + 1];
        readonly Vector2[] ultimaPosicion = new Vector2[JefeSimulation.MaxProyectiles];
        readonly int[] serieDibujada = new int[JefeSimulation.MaxProyectiles];
        SombraProyectada sombra;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            cuerpo = Nuevo("Cuerpo");
            for (int i = 0; i < proyectiles.Length; i++)
            {
                proyectiles[i] = Nuevo("Proyectil " + i);
                impactos[i] = Nuevo("Impacto " + i);
            }
            marcaCarril = Nuevo("Marca de carril");
            for (int i = 0; i < marcasPunto.Length; i++)
            {
                marcasPunto[i] = Nuevo("Marca de caida " + i);
                marcasPunto[i].sprite = FormasDeAviso.Aro;
            }
            caja = Nuevo("Caja de balas");
            caja.sprite = cajaDeBalas;
            granada = Nuevo("Granada");
            granada.sprite = granadaSuelta;
            marcaCarril.sprite = FormasDeAviso.Capsula;
            sombra = SombraProyectada.Poner(cuerpo.gameObject, cuerpo, "", 0, true);
        }

        SpriteRenderer Nuevo(string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (material != null)
                sr.sharedMaterial = material;
            sr.sortingLayerID = LaneSorting.GameLayerId;
            sr.enabled = false;
            return sr;
        }

        void LateUpdate()
        {
            if (run == null || run.Sim == null || animaciones == null)
                return;

            var jefe = run.Sim.Jefe;
            bool enDuelo = jefe.EnDuelo;
            cuerpo.enabled = enDuelo;
            if (sombra != null)
                sombra.Visible = enDuelo;
            if (enDuelo)
                DibujarCuerpo(jefe);

            DibujarAvisos(jefe);
            for (int i = 0; i < proyectiles.Length; i++)
            {
                DibujarProyectil(i, jefe.Proyectiles[i]);
                DibujarImpacto(impactos[i], jefe.Proyectiles[i]);
            }
            DibujarRecurso(caja, jefe.CajaDeBalas);
            DibujarRecurso(granada, jefe.GranadaSuelta);
        }

        void DibujarCuerpo(JefeSimulation jefe)
        {
            var (clip, tiempo) = ClipActual(jefe);
            int i = clip.Indice(tiempo);
            cuerpo.sprite = clip.cuadros[i];
            float k = escala * clip.escala[i];
            cuerpo.transform.localScale = new Vector3(k, k, 1f);

            // Las orugas nunca se detienen del todo: un temblor corto mientras persigue.
            float temblor = jefe.Fase == FaseDeJefe.Derrotado || jefe.Vulnerable ? 0f :
                Mathf.Sin(run.Sim.State.Elapsed * 26f) * 0.025f;
            cuerpo.transform.position = new Vector3(
                run.ToWorldX(jefe.X),
                run.LaneToWorldY(jefe.CarrilVisual) - apoyoBajoElCarril + temblor, 0f);

            int orden = LaneSorting.Order(jefe.CarrilVisual, SortSlot.Zombie);
            cuerpo.sortingOrder = orden;
            if (sombra != null)
                sombra.Orden = LaneSorting.Order(jefe.CarrilVisual, SortSlot.Shadow);

            cuerpo.color = Tinte(jefe);
        }

        Color Tinte(JefeSimulation jefe)
        {
            // El golpe se apaga solo: no tapa el dibujo aunque le disparen seguido.
            if (jefe.DanoRestante > 0f)
                return Color.Lerp(Color.white, colorDano, jefe.DanoRestante / run.Config.jefe.dano);
            if (!jefe.Vulnerable)
                return Color.white;
            float latido = 0.5f + 0.5f * Mathf.Sin(jefe.TiempoFase * Mathf.PI * 2f * 2.5f);
            return Color.Lerp(Color.white, colorAturdido, latido);
        }

        (ClipDeJefe clip, float tiempo) ClipActual(JefeSimulation jefe)
        {
            var config = run.Config.jefe;
            if (jefe.DanoRestante > 0f)
                return (animaciones.dano, config.dano - jefe.DanoRestante);

            float t = jefe.TiempoFase;
            var embestida = animaciones.embestida;
            float preparacion = embestida.segundos[0];
            float avance = embestida.segundos[1] + embestida.segundos[2];
            switch (jefe.Fase)
            {
                case FaseDeJefe.Anticipacion:
                    return (animaciones.anticipacion, t);
                case FaseDeJefe.Disparo:
                    return (jefe.AtaqueElevado ? animaciones.ataqueElevado : animaciones.ataqueFrontal, t);
                case FaseDeJefe.AvisoEmbestida:
                    return (embestida, 0f);   // el aviso dura lo que diga el encuentro: se sostiene la preparacion
                case FaseDeJefe.Embestida:
                    return (embestida, preparacion + t);
                case FaseDeJefe.Regreso:
                    return (embestida, preparacion + avance + t);
                case FaseDeJefe.Aturdido:
                    return config.segundosAturdido - t <= animaciones.recuperacion.Duracion
                        ? (animaciones.recuperacion, 0f)
                        : (animaciones.aturdido, t);
                case FaseDeJefe.Derrotado:
                    return (animaciones.muerte, t);
                default:
                    return (animaciones.quieto, 0f);
            }
        }

        void DibujarAvisos(JefeSimulation jefe)
        {
            var color = colorAviso;
            color.a *= 0.55f + 0.45f * Mathf.Sin(run.Sim.State.Elapsed * pulsoAviso * Mathf.PI * 2f);

            // El ataque en curso: el elevado marca donde va a caer; el resto, todo el carril.
            bool activo = jefe.AvisoDeCarril;
            bool elevado = activo && jefe.AtaqueElevado && jefe.Fase != FaseDeJefe.AvisoEmbestida && jefe.Fase != FaseDeJefe.Embestida;
            int marcas = 0;
            if (elevado)
                MarcarCaida(marcasPunto[marcas++], jefe.Carril, color);
            // Cada mortero en el aire, hasta que cae.
            foreach (var p in jefe.Proyectiles)
                if (p.Activo && p.Elevado && marcas < marcasPunto.Length)
                    MarcarCaida(marcasPunto[marcas++], p.Carril, color);
            for (int i = marcas; i < marcasPunto.Length; i++)
                marcasPunto[i].enabled = false;

            marcaCarril.enabled = activo && !elevado;
            if (!marcaCarril.enabled)
                return;

            // Desde el jefe hasta pasar la moto: todo el carril por donde va a venir.
            float y = run.LaneToWorldY(jefe.Carril);
            float desde = run.ToWorldX(jefe.X);
            float hasta = run.ToWorldX(run.Sim.State.PlayerX + 8f);
            marcaCarril.color = color;
            marcaCarril.sortingOrder = LaneSorting.Order(jefe.Carril, SortSlot.Shadow) - 2;
            marcaCarril.transform.position = new Vector3((desde + hasta) * 0.5f, y, 0f);
            var tam = marcaCarril.sprite.bounds.size;
            marcaCarril.transform.localScale = new Vector3(
                (hasta - desde) / tam.x, run.Config.laneSpacing * 0.6f / tam.y, 1f);
        }

        void MarcarCaida(SpriteRenderer marca, int carril, Color color)
        {
            color.a = Mathf.Clamp01(color.a * 1.8f);
            marca.enabled = true;
            marca.color = color;
            marca.sortingOrder = LaneSorting.Order(carril, SortSlot.Shadow) - 2;
            marca.transform.position = new Vector3(run.ToWorldX(run.Sim.State.PlayerX), run.LaneToWorldY(carril), 0f);
            float ancho = anchoMarcaElevado / marca.sprite.bounds.size.x;
            marca.transform.localScale = new Vector3(ancho, ancho * 0.45f, 1f);
        }

        void DibujarProyectil(int i, ProyectilDelJefe p)
        {
            var proyectil = proyectiles[i];
            proyectil.enabled = p.Activo;
            if (!p.Activo)
                return;

            var clip = animaciones.proyectilVuelo;
            proyectil.sprite = clip.cuadros[clip.Indice(run.Sim.State.Elapsed)];
            proyectil.sortingOrder = LaneSorting.Order(p.Carril, SortSlot.Effect);
            proyectil.transform.localScale = new Vector3(escalaProyectil, escalaProyectil, 1f);

            var boca = (p.Elevado ? animaciones.bocaElevado : animaciones.bocaFrontal) * escala;
            float suelo = run.LaneToWorldY(p.Carril);
            float x = run.ToWorldX(run.Sim.State.PlayerX + p.Relativo);
            float y;
            if (p.Elevado)
            {
                float t = p.Avance;
                y = suelo + Mathf.Lerp(boca.y, 0f, t) + 4f * alturaDelArco * t * (1f - t);
            }
            else
            {
                float t = Mathf.InverseLerp(p.Salida, 0f, p.Relativo);
                y = suelo + Mathf.Lerp(boca.y, alturaDeContacto, t);
            }

            // La cola queda atras: el dibujo mira hacia donde va. La moto corre con el proyectil,
            // asi que se mide contra ella y no contra el piso. Uno nuevo arranca mirando hacia donde sale.
            var posicion = new Vector2(x - run.ToWorldX(run.Sim.State.PlayerX), y);
            if (serieDibujada[i] != p.Serie)
            {
                serieDibujada[i] = p.Serie;
                ultimaPosicion[i] = posicion;
                proyectil.transform.rotation = Quaternion.Euler(0f, 0f, p.Elevado ? 60f : 0f);
            }
            var paso = posicion - ultimaPosicion[i];
            ultimaPosicion[i] = posicion;
            if (paso.sqrMagnitude > 0.000001f && paso.sqrMagnitude < 4f)
                proyectil.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(paso.y, paso.x) * Mathf.Rad2Deg);
            proyectil.transform.position = new Vector3(x, y, 0f);
        }

        void DibujarImpacto(SpriteRenderer impacto, ProyectilDelJefe p)
        {
            impacto.enabled = p.ImpactoRestante > 0f;
            if (!impacto.enabled)
                return;

            var clip = animaciones.proyectilImpacto;
            impacto.sprite = clip.cuadros[clip.Indice(run.Config.jefe.impacto - p.ImpactoRestante)];
            impacto.sortingOrder = LaneSorting.Order(p.Carril, SortSlot.Effect);
            impacto.transform.localScale = new Vector3(escalaProyectil, escalaProyectil, 1f);
            impacto.transform.position = PosicionDelImpacto(p);
            // El pivote es el punto de contacto: el frente del dibujo va contra la superficie.
            impacto.transform.rotation = Quaternion.Euler(0f, 0f, p.ImpactoEnElPiso ? -90f : 0f);
        }

        // --- Para los efectos (EfectosDelJefe), que corren despues de esta vista ---

        public bool CuerpoVisible => cuerpo != null && cuerpo.enabled;
        public int OrdenDelCuerpo => cuerpo.sortingOrder;

        /// <summary>Un punto del dibujo del jefe, medido desde el apoyo de las orugas con la escala del dibujo.</summary>
        public Vector3 EnElCuerpo(Vector2 desdeElApoyo) =>
            cuerpo.transform.position + (Vector3)(desdeElApoyo * cuerpo.transform.localScale.x);

        /// <summary>Donde se dibujo el proyectil de ese lugar en este cuadro y hacia donde mira, en grados.</summary>
        public bool ProyectilDibujado(int i, out Vector3 posicion, out float angulo)
        {
            var t = proyectiles[i].transform;
            posicion = t.position;
            angulo = t.eulerAngles.z;
            return proyectiles[i].enabled;
        }

        public Vector3 PosicionDelImpacto(ProyectilDelJefe p)
        {
            float y = run.LaneToWorldY(p.Carril) + (p.ImpactoEnElPiso ? 0.1f : alturaDeContacto);
            return new Vector3(run.ToWorldX(run.Sim.State.PlayerX + p.ImpactoRelativo), y, 0f);
        }

        void DibujarRecurso(SpriteRenderer sr, RecursoDelDuelo recurso)
        {
            sr.enabled = recurso.Activo && sr.sprite != null;
            if (!sr.enabled)
                return;

            float vaiven = Mathf.Sin(run.Sim.State.Elapsed * 3f) * 0.1f;
            float k = altoRecolectable / sr.sprite.bounds.size.y;
            sr.transform.localScale = new Vector3(k, k, 1f);
            sr.transform.position = new Vector3(
                run.ToWorldX(recurso.X), run.LaneToWorldY(recurso.Carril) + AlturaSobreElCarril + vaiven, 0f);
            sr.sortingOrder = LaneSorting.Order(recurso.Carril, SortSlot.Item);
        }
    }

    /// <summary>Las marcas del piso, hechas por codigo: no necesitan arte.</summary>
    static class FormasDeAviso
    {
        static Sprite capsula, aro;

        public static Sprite Capsula => capsula != null ? capsula : capsula = Armar(128, 32, "CapsulaDeAviso", (x, y, w, h) =>
        {
            float dx = Mathf.Max(0f, Mathf.Abs(x - (w - 1) * 0.5f) - (w - h) * 0.5f) / (h * 0.5f);
            float dy = Mathf.Abs(y - (h - 1) * 0.5f) / (h * 0.5f);
            return Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 2.5f);
        });

        public static Sprite Aro => aro != null ? aro : aro = Armar(64, 64, "AroDeCaida", (x, y, w, h) =>
        {
            float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w / 2f, h / 2f)) / (w / 2f);
            float borde = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.12f);
            return Mathf.Max(borde, r < 0.86f ? 0.5f : 0f);
        });

        static Sprite Armar(int w, int h, string nombre, System.Func<int, int, int, int, float> alfa)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(alfa(x, y, w, h) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = nombre;
            return sprite;
        }
    }
}
