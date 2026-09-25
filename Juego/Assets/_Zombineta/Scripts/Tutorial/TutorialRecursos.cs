namespace Zombineta.Tutorial
{
    /// <summary>Avisos y recargas que decidio TutorialRecursos para este cuadro.</summary>
    public struct Decision
    {
        public bool avisoNafta;
        public bool avisoBateria;
        public bool recargarNafta;
        public bool recargarBateria;
        public bool recargarMunicion;
    }

    /// <summary>
    /// C# plano: mira nafta, bateria y municion del paso actual y decide si hay que avisar poco
    /// recurso o recargar. TutorialSesion es quien aplica la recarga sobre el RunState (antes del
    /// Tick del cuadro); esta clase solo decide.
    /// </summary>
    public sealed class TutorialRecursos
    {
        readonly float umbralAviso;
        readonly float recarga;

        bool armadoNafta = true;
        bool armadoBateria = true;

        public TutorialRecursos(float umbralAviso = 0.2f, float recarga = 0.6f)
        {
            this.umbralAviso = umbralAviso;
            this.recarga = recarga;
        }

        public float UmbralAviso => umbralAviso;
        public float Recarga => recarga;

        public Decision Evaluar(float nafta01, float bateria01, int municion, bool recargaMunicion)
        {
            var d = new Decision
            {
                avisoNafta = Avisar(nafta01, ref armadoNafta),
                avisoBateria = Avisar(bateria01, ref armadoBateria),
                recargarNafta = nafta01 <= 0f,
                recargarBateria = bateria01 <= 0f,
                recargarMunicion = recargaMunicion && municion <= 0,
            };
            return d;
        }

        bool Avisar(float valor01, ref bool armado)
        {
            if (valor01 < umbralAviso)
            {
                if (!armado)
                    return false;

                armado = false;
                return true;
            }

            // Volvio a estar arriba del umbral: se rearma para el proximo cruce.
            armado = true;
            return false;
        }
    }
}
