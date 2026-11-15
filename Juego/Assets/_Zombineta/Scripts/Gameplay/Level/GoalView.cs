using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Level
{
    /// <summary>
    /// La llegada: un tramo de calle con conos en las dos veredas y una linea rosa que la cruza.
    /// La condicion de victoria la resuelve la simulacion; esto solo lo dibuja, para que la meta se
    /// vea llegar y no aparezca de la nada.
    ///
    /// El dibujo es la misma calle del escenario (mismas lineas, misma diagonal), asi que se apoya
    /// encima de la que haya: sus bordes se calzan con los de la calle y la linea rosa cae, en el
    /// carril del medio, justo en el metro de la meta. Los conos de la vereda de adelante van aparte,
    /// recortados del mismo dibujo, porque tienen que tapar a la moto y a la horda del carril de abajo.
    /// </summary>
    public sealed class GoalView : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] Transform gate;

        [Tooltip("El tramo de calle con los conos y la linea de llegada (tile_meta_v1).")]
        [SerializeField] Sprite dibujo;

        [Tooltip("Solo los conos de la vereda de adelante, recortados del mismo dibujo y del mismo tamano " +
                 "(tile_meta_conos_frente_v1).")]
        [SerializeField] Sprite conosDeAdelante;

        // Medidas del dibujo, en pixeles contados desde su borde de arriba: la raya negra de arriba de la
        // calle y la de abajo, donde empieza la vereda.
        public const float FilaBordeDeArriba = 195f;
        public const float FilaBordeDeAbajo = 620f;

        // La linea rosa va en diagonal: su centro esta en x = PendienteDeLaLinea * fila + OrigenDeLaLinea.
        public const float PendienteDeLaLinea = -0.9277f;
        public const float OrigenDeLaLinea = 1035.76f;

        // Donde quedan esas dos rayas en la calle del escenario (capa Calle de Escenario y de
        // Escenario_Nivel2, que comparten alto y base). GoalViewTests lo compara con los dos.
        public const float YBordeDeArriba = 1.38377f;
        public const float YBordeDeAbajo = -1.45279f;

        // El dibujo va en la capa de la calle: sobre sus tiles (-20) y debajo de las sombras del
        // escenario (60), la niebla y las salpicaduras, que tambien lo tienen que pisar.
        const string CapaDelDibujo = "Calle";
        const int OrdenDelDibujo = 0;

        // Los conos de adelante, en la capa del juego por encima de todo lo que dibuja LaneSorting en
        // el carril de abajo (hasta 1049): estan mas cerca de la camara que la moto.
        const int OrdenDeLosConos = 1100;

        float corrimientoX;
        bool armado;

        // Con un jefe vivo la llegada no se ve, y al vencerlo aparece adelante de la moto.
        void LateUpdate()
        {
            if (!armado || run.Sim == null)
                return;

            bool visible = !run.Sim.Jefe.Peleando;
            if (gate.gameObject.activeSelf != visible)
                gate.gameObject.SetActive(visible);
            var p = gate.position;
            gate.position = new Vector3(run.ToWorldX(run.Sim.Meta) + corrimientoX, p.y, 0f);
        }

        void Start()
        {
            if (run == null || gate == null || run.Config == null || dibujo == null)
                return;

            var renderer = gate.GetComponent<SpriteRenderer>();
            if (renderer == null)
                return;

            renderer.sprite = dibujo;
            renderer.color = Color.white;
            renderer.sortingLayerName = CapaDelDibujo;
            renderer.sortingOrder = OrdenDelDibujo;

            float escala = Escala(dibujo);
            gate.localScale = new Vector3(escala, escala, 1f);

            // La linea se mide en el carril del medio, que es donde anda la moto la mayor parte del tiempo.
            float x0 = DesdeLaLinea(dibujo, run.LaneToWorldY(1f), out float y);
            corrimientoX = x0;
            gate.position = new Vector3(run.ToWorldX(run.Config.goalDistance) + corrimientoX, y, 0f);

            if (conosDeAdelante != null)
            {
                var conos = new GameObject("Conos de adelante").AddComponent<SpriteRenderer>();
                conos.transform.SetParent(gate, false);
                conos.sprite = conosDeAdelante;
                conos.sharedMaterial = renderer.sharedMaterial;
                conos.sortingLayerName = LaneSorting.GameLayer;
                conos.sortingOrder = OrdenDeLosConos;
            }

            armado = true;
        }

        /// <summary>Escala que lleva los bordes de la calle del dibujo a los de la calle del escenario.</summary>
        public static float Escala(Sprite s) =>
            (YBordeDeArriba - YBordeDeAbajo) / ((FilaBordeDeAbajo - FilaBordeDeArriba) / s.pixelsPerUnit);

        /// <summary>
        /// Donde va el dibujo para que su linea rosa, a la altura yLinea, caiga en x = 0 (la meta se
        /// suma despues): devuelve el corrimiento en x y, en y, la altura que calza los bordes.
        /// </summary>
        public static float DesdeLaLinea(Sprite s, float yLinea, out float y)
        {
            float escala = Escala(s);
            float ppu = s.pixelsPerUnit;
            Bounds b = s.bounds;

            // Una fila del dibujo esta a (b.max.y - fila / ppu) en unidades locales del sprite.
            y = YBordeDeArriba - (b.max.y - FilaBordeDeArriba / ppu) * escala;

            float fila = FilaBordeDeArriba + (YBordeDeArriba - yLinea) / escala * ppu;
            float columna = PendienteDeLaLinea * fila + OrigenDeLaLinea;
            return -(b.min.x + columna / ppu) * escala;
        }
    }
}
