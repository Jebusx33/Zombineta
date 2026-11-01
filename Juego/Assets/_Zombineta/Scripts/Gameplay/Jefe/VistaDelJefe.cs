using UnityEngine;
using Zombineta.Core;
using Zombineta.Level;
using Zombineta.Luz;

namespace Zombineta.Jefe
{
    /// <summary>
    /// Lo que se ve del duelo: el jefe, su proyectil con el impacto, las marcas en el piso y lo que hay
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

        SpriteRenderer cuerpo, proyectil, impacto, marcaCarril, marcaPunto, caja, granada;
        SombraProyectada sombra;
        Vector2 ultimaPosicionProyectil;

        void Awake()
        {
            if (run == null)
                run = FindAnyObjectByType<RunController>();

            cuerpo = Nuevo("Cuerpo");
            proyectil = Nuevo("Proyectil");
            impacto = Nuevo("Impacto");
            marcaCarril = Nuevo("Marca de carril");
            marcaPunto = Nuevo("Marca de caida");
            caja = Nuevo("Caja de balas");
            caja.sprite = cajaDeBalas;
            granada = Nuevo("Granada");
            granada.sprite = granadaSuelta;
            marcaCarril.sprite = FormasDeAviso.Capsula;
            marcaPunto.sprite = FormasDeAviso.Aro;
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
            DibujarProyectil(jefe);
            DibujarImpacto(jefe);
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
            bool activo = jefe.AvisoDeCarril;
            bool elevado = activo && jefe.AtaqueElevado && jefe.Fase != FaseDeJefe.AvisoEmbestida && jefe.Fase != FaseDeJefe.Embestida;
            marcaPunto.enabled = elevado;
            marcaCarril.enabled = activo && !elevado;
            if (!activo)
                return;

            var color = colorAviso;
            color.a *= 0.55f + 0.45f * Mathf.Sin(run.Sim.State.Elapsed * pulsoAviso * Mathf.PI * 2f);
            float y = run.LaneToWorldY(jefe.Carril);
            int orden = LaneSorting.Order(jefe.Carril, SortSlot.Shadow) - 2;

            if (elevado)
            {
                color.a = Mathf.Clamp01(color.a * 1.8f);
                marcaPunto.color = color;
                marcaPunto.sortingOrder = orden;
                marcaPunto.transform.position = new Vector3(run.ToWorldX(run.Sim.State.PlayerX), y, 0f);
                float ancho = anchoMarcaElevado / marcaPunto.sprite.bounds.size.x;
                marcaPunto.transform.localScale = new Vector3(ancho, ancho * 0.45f, 1f);
                return;
            }

            // Desde el jefe hasta pasar la moto: todo el carril por donde va a venir.
            float desde = run.ToWorldX(jefe.X);
            float hasta = run.ToWorldX(run.Sim.State.PlayerX + 8f);
            marcaCarril.color = color;
            marcaCarril.sortingOrder = orden;
            marcaCarril.transform.position = new Vector3((desde + hasta) * 0.5f, y, 0f);
            var tam = marcaCarril.sprite.bounds.size;
            marcaCarril.transform.localScale = new Vector3(
                (hasta - desde) / tam.x, run.Config.laneSpacing * 0.6f / tam.y, 1f);
        }

        void DibujarProyectil(JefeSimulation jefe)
        {
            proyectil.enabled = jefe.ProyectilActivo;
            if (!jefe.ProyectilActivo)
                return;

            var clip = animaciones.proyectilVuelo;
            proyectil.sprite = clip.cuadros[clip.Indice(run.Sim.State.Elapsed)];
            proyectil.sortingOrder = LaneSorting.Order(jefe.ProyectilCarril, SortSlot.Effect);
            proyectil.transform.localScale = new Vector3(escalaProyectil, escalaProyectil, 1f);

            var boca = (jefe.ProyectilElevado ? animaciones.bocaElevado : animaciones.bocaFrontal) * escala;
            float suelo = run.LaneToWorldY(jefe.ProyectilCarril);
            float x = run.ToWorldX(run.Sim.State.PlayerX + jefe.ProyectilRelativo);
            float y;
            if (jefe.ProyectilElevado)
            {
                float p = jefe.ProyectilAvance;
                y = suelo + Mathf.Lerp(boca.y, 0f, p) + 4f * alturaDelArco * p * (1f - p);
            }
            else
            {
                float p = Mathf.InverseLerp(jefe.ProyectilSalida, 0f, jefe.ProyectilRelativo);
                y = suelo + Mathf.Lerp(boca.y, alturaDeContacto, p);
            }

            // La cola queda atras: el dibujo mira hacia donde va. La moto corre con el proyectil,
            // asi que se mide contra ella y no contra el piso.
            var posicion = new Vector2(x - run.ToWorldX(run.Sim.State.PlayerX), y);
            var paso = posicion - ultimaPosicionProyectil;
            ultimaPosicionProyectil = posicion;
            if (paso.sqrMagnitude > 0.000001f && paso.sqrMagnitude < 4f)
                proyectil.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(paso.y, paso.x) * Mathf.Rad2Deg);
            proyectil.transform.position = new Vector3(x, y, 0f);
        }

        void DibujarImpacto(JefeSimulation jefe)
        {
            impacto.enabled = jefe.ImpactoRestante > 0f;
            if (!impacto.enabled)
                return;

            var clip = animaciones.proyectilImpacto;
            impacto.sprite = clip.cuadros[clip.Indice(run.Config.jefe.impacto - jefe.ImpactoRestante)];
            impacto.sortingOrder = LaneSorting.Order(jefe.ImpactoCarril, SortSlot.Effect);
            impacto.transform.localScale = new Vector3(escalaProyectil, escalaProyectil, 1f);

            float y = run.LaneToWorldY(jefe.ImpactoCarril) + (jefe.ImpactoEnElPiso ? 0.1f : alturaDeContacto);
            impacto.transform.position = new Vector3(run.ToWorldX(run.Sim.State.PlayerX + jefe.ImpactoRelativo), y, 0f);
            // El pivote es el punto de contacto: el frente del dibujo va contra la superficie.
            impacto.transform.rotation = Quaternion.Euler(0f, 0f, jefe.ImpactoEnElPiso ? -90f : 0f);
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
