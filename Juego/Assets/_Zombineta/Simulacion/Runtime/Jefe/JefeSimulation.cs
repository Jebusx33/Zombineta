using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Jefe
{
    public enum FaseDeJefe
    {
        /// <summary>Todavia no empezo el duelo, o este nivel no tiene jefe.</summary>
        Inactivo,
        Entrada,
        /// <summary>Persigue sin atacar, entre una accion y la siguiente.</summary>
        Persecucion,
        Anticipacion,
        Disparo,
        AvisoEmbestida,
        Embestida,
        Regreso,
        Aturdido,
        Derrotado,
    }

    /// <summary>Algo que aparece en el camino durante el duelo.</summary>
    public sealed class RecursoDelDuelo
    {
        public bool Activo;
        public float X;
        public int Carril;
    }

    /// <summary>Un proyectil del jefe y el impacto que deja donde pega.</summary>
    public sealed class ProyectilDelJefe
    {
        public bool Activo;
        public bool Elevado;
        /// <summary>El carril por donde va, y donde queda el impacto.</summary>
        public int Carril;
        /// <summary>Metros respecto de la moto: negativo, todavia detras.</summary>
        public float Relativo;
        /// <summary>Donde salio, respecto de la moto.</summary>
        public float Salida;
        /// <summary>Del recorrido del elevado: 0 al salir, 1 al caer.</summary>
        public float Avance;
        internal float vida;
        /// <summary>Sube con cada disparo: la vista reconoce uno nuevo aunque reuse este lugar.</summary>
        public int Serie;

        public float ImpactoRestante;
        public float ImpactoRelativo;
        public bool ImpactoEnElPiso;
        /// <summary>Sube con cada impacto, como Serie.</summary>
        public int SerieImpacto;
    }

    /// <summary>
    /// El duelo en moto contra el jefe. C# plano, como el resto de las reglas: la vista solo lee este
    /// estado. El jefe persigue a una distancia fija, repite su patron y solo se lo puede lastimar
    /// aturdido. Fija el carril al empezar cada ataque y ya no lo corrige. Mientras dura el duelo la
    /// nafta no baja y la horda se queda atras: lo que se juega son los corazones de la moto.
    /// </summary>
    public sealed class JefeSimulation
    {
        readonly GameConfig config;
        readonly RunSimulation run;
        readonly RunState s;

        JefeConfig Jefe => config.jefe;

        int paso;
        bool segundaMitad;
        bool emitido;
        bool embestidaPego;
        AccionDeJefe accion;
        int elevados;
        int disparosRestantes;

        public FaseDeJefe Fase { get; private set; }
        /// <summary>Segundos desde que empezo la fase. La vista saca de aca el cuadro de la animacion.</summary>
        public float TiempoFase { get; private set; }
        public int Vida { get; private set; }
        /// <summary>Golpes que le quedan a la moto.</summary>
        public int Corazones { get; private set; }
        public float Invulnerable { get; private set; }
        /// <summary>Metros detras de la moto. Negativo: la paso.</summary>
        public float Separacion { get; private set; }
        /// <summary>El carril donde esta, que es el del ataque en curso.</summary>
        public int Carril { get; private set; }
        public float CarrilVisual { get; private set; }
        /// <summary>Si el disparo en curso es el elevado.</summary>
        public bool AtaqueElevado => accion == AccionDeJefe.Elevado;
        /// <summary>Segundos que le quedan a la animacion de dano.</summary>
        public float DanoRestante { get; private set; }

        // --- Proyectiles: en el mortero doble hay dos en el aire, y un frontal que ya paso a la moto
        // puede seguir viaje mientras empieza el ataque siguiente ---
        public const int MaxProyectiles = 3;
        public ProyectilDelJefe[] Proyectiles { get; } =
            { new ProyectilDelJefe(), new ProyectilDelJefe(), new ProyectilDelJefe() };
        public ProyectilDelJefe UltimoDisparo { get; private set; }
        public ProyectilDelJefe UltimoImpacto { get; private set; }

        public bool ProyectilActivo
        {
            get
            {
                foreach (var p in Proyectiles)
                    if (p.Activo)
                        return true;
                return false;
            }
        }

        // --- Lo que hay que ir recogiendo para vencerlo ---
        public RecursoDelDuelo CajaDeBalas { get; } = new RecursoDelDuelo();
        public RecursoDelDuelo GranadaSuelta { get; } = new RecursoDelDuelo();

        public JefeSimulation(GameConfig config, RunSimulation run)
        {
            this.config = config;
            this.run = run;
            s = run.State;
        }

        public float X => s.PlayerX - Separacion;

        /// <summary>Hay jefe y empezo el duelo. Sigue siendolo con el jefe derrotado, hasta el refugio.</summary>
        public bool EnDuelo => Fase != FaseDeJefe.Inactivo;

        /// <summary>Queda un jefe por vencer: mientras tanto cruzar la meta no termina el nivel.</summary>
        public bool Pendiente => Jefe != null && Fase != FaseDeJefe.Derrotado;

        /// <summary>El duelo esta en curso, con el jefe vivo.</summary>
        public bool Peleando => EnDuelo && Fase != FaseDeJefe.Derrotado;

        public bool Vulnerable => Fase == FaseDeJefe.Aturdido;

        /// <summary>
        /// Hay un carril fijado para el ataque en curso: la vista lo marca en el piso. Cada mortero en
        /// el aire marca ademas el suyo (ver Proyectiles).
        /// </summary>
        public bool AvisoDeCarril =>
            Fase == FaseDeJefe.Anticipacion || Fase == FaseDeJefe.AvisoEmbestida ||
            (Fase == FaseDeJefe.Disparo && !emitido) ||
            (Fase == FaseDeJefe.Embestida && !embestidaPego);

        public void Reset()
        {
            Fase = FaseDeJefe.Inactivo;
            TiempoFase = 0f;
            Vida = Jefe != null ? Jefe.vida : 0;
            Corazones = Jefe != null ? Jefe.corazones : 0;
            Invulnerable = 0f;
            Separacion = Jefe != null ? Jefe.separacionEntrada : 0f;
            Carril = 1;
            CarrilVisual = 1f;
            DanoRestante = 0f;
            // Las series no vuelven a cero: la vista las compara con las que ya vio.
            foreach (var p in Proyectiles)
            {
                p.Activo = false;
                p.ImpactoRestante = 0f;
            }
            UltimoDisparo = null;
            UltimoImpacto = null;
            CajaDeBalas.Activo = false;
            GranadaSuelta.Activo = false;
            paso = 0;
            segundaMitad = false;
            emitido = false;
            embestidaPego = false;
            elevados = 0;
            disparosRestantes = 0;
        }

        public RunEvent Step(float dt)
        {
            if (Jefe == null)
                return RunEvent.None;

            if (Fase == FaseDeJefe.Inactivo)
                return PuedeEmpezar ? Empezar() : RunEvent.None;

            TiempoFase += dt;
            Invulnerable = Mathf.Max(0f, Invulnerable - dt);
            DanoRestante = Mathf.Max(0f, DanoRestante - dt);
            foreach (var p in Proyectiles)
                p.ImpactoRestante = Mathf.Max(0f, p.ImpactoRestante - dt);
            CarrilVisual = Mathf.MoveTowards(CarrilVisual, Carril, Jefe.velocidadCarril * dt);

            var events = AvanzarFase(dt);
            events |= AvanzarProyectil(dt);
            events |= AvanzarRecursos();
            return events;
        }

        // Sin nafta se baja a pelear como siempre, y el duelo espera a que vuelva a la moto.
        bool PuedeEmpezar =>
            s.PlayerX >= Jefe.metroEntrada && s.Phase == RunPhase.Running &&
            !s.Airborne && s.Fuel > 0f && s.FrenadoRestante < 0f;

        RunEvent Empezar()
        {
            Pasar(FaseDeJefe.Entrada);
            Carril = s.Lane;
            CarrilVisual = s.Lane;
            Separacion = Jefe.separacionEntrada;
            s.Fuel = config.fuelMax;
            return RunEvent.JefeEntra;
        }

        void Pasar(FaseDeJefe fase)
        {
            Fase = fase;
            TiempoFase = 0f;
        }

        RunEvent AvanzarFase(float dt)
        {
            switch (Fase)
            {
                case FaseDeJefe.Entrada:
                    Separacion = Mathf.Lerp(Jefe.separacionEntrada, Jefe.separacion, Mathf.SmoothStep(0f, 1f, TiempoFase / Jefe.segundosEntrada));
                    if (TiempoFase >= Jefe.segundosEntrada)
                        return EmpezarCiclo();
                    return RunEvent.None;

                case FaseDeJefe.Persecucion:
                    // Nunca empieza otro ataque con un proyectil que todavia pueda pegar: podria cerrar
                    // la salida segura. Un frontal que ya paso a la moto no cuenta.
                    if (TiempoFase >= Jefe.pausaEntreAcciones && !HayProyectilQueAmenaza)
                        return SiguienteAccion();
                    return RunEvent.None;

                case FaseDeJefe.Anticipacion:
                    if (TiempoFase >= Jefe.anticipacion)
                    {
                        Pasar(FaseDeJefe.Disparo);
                        emitido = false;
                        return EmitirSiToca();
                    }
                    return RunEvent.None;

                case FaseDeJefe.Disparo:
                    var emision = EmitirSiToca();
                    if (TiempoFase >= (AtaqueElevado ? Jefe.disparoElevado : Jefe.disparoFrontal))
                    {
                        if (disparosRestantes > 0)
                        {
                            // Mortero doble: repite el disparo hacia otro carril, sin nueva anticipacion.
                            disparosRestantes--;
                            Carril = OtroCarril(Carril);
                            Pasar(FaseDeJefe.Disparo);
                            emitido = false;
                        }
                        else
                        {
                            Pasar(FaseDeJefe.Persecucion);
                        }
                    }
                    return emision;

                case FaseDeJefe.AvisoEmbestida:
                    if (TiempoFase >= Jefe.avisoEmbestida)
                    {
                        Pasar(FaseDeJefe.Embestida);
                        embestidaPego = false;
                        return RunEvent.JefeEmbiste;
                    }
                    return RunEvent.None;

                case FaseDeJefe.Embestida:
                    float avance = Mathf.Clamp01(TiempoFase / Jefe.avanceEmbestida);
                    Separacion = Mathf.Lerp(Jefe.separacion, Jefe.separacionEmbestida, avance);
                    var choque = ChocarConLaMoto();
                    if (avance >= 1f)
                        Pasar(FaseDeJefe.Regreso);
                    return choque;

                case FaseDeJefe.Regreso:
                    float vuelta = Mathf.Clamp01(TiempoFase / Jefe.regresoEmbestida);
                    Separacion = Mathf.Lerp(Jefe.separacionEmbestida, Jefe.separacion, Mathf.SmoothStep(0f, 1f, vuelta));
                    if (vuelta >= 1f)
                        Pasar(FaseDeJefe.Persecucion);
                    return RunEvent.None;

                case FaseDeJefe.Aturdido:
                    if (TiempoFase >= Jefe.segundosAturdido)
                        Pasar(FaseDeJefe.Persecucion);
                    return RunEvent.None;

                case FaseDeJefe.Derrotado:
                    // Sigue rodando por inercia mientras cae, para que se vea. Despues la moto lo deja atras.
                    if (TiempoFase >= Jefe.caidaEnPantalla)
                        Separacion += run.PlayerSpeed * dt;
                    return RunEvent.None;

                default:
                    return RunEvent.None;
            }
        }

        AccionDeJefe[] Patron => segundaMitad ? Jefe.patronSegundaMitad : Jefe.patronPrimeraMitad;

        // Cada vuelta del patron arranca aca: aparece lo que le falte, asi el duelo nunca queda sin
        // solucion por haber gastado las armas.
        RunEvent EmpezarCiclo()
        {
            paso = 0;
            segundaMitad |= Vida <= Jefe.vidaSegundaMitad;
            Aparecer(CajaDeBalas, s.Ammo < config.ammoMax, Jefe.cajaAdelante, 1);
            Aparecer(GranadaSuelta, s.Granadas < config.granadaMax, Jefe.granadaAdelante, 2);
            return EjecutarPaso();
        }

        void Aparecer(RecursoDelDuelo recurso, bool hace, float adelante, int carrilesDeLaMoto)
        {
            if (!hace || recurso.Activo)
                return;
            recurso.Activo = true;
            recurso.X = s.PlayerX + adelante;
            recurso.Carril = (s.Lane + carrilesDeLaMoto) % RunSimulation.LaneCount;
        }

        RunEvent SiguienteAccion()
        {
            paso++;
            return paso >= Patron.Length ? EmpezarCiclo() : EjecutarPaso();
        }

        RunEvent EjecutarPaso()
        {
            accion = Patron[paso];
            if (accion == AccionDeJefe.Aturdido)
            {
                Pasar(FaseDeJefe.Aturdido);
                return RunEvent.JefeAturdido;
            }

            // El objetivo se fija aca y ya no sigue a la moto.
            Carril = s.Lane;
            disparosRestantes = 0;
            if (accion == AccionDeJefe.Elevado)
                disparosRestantes = DisparosDelElevado(Jefe.disparosElevados, elevados++) - 1;
            Pasar(accion == AccionDeJefe.Embestida ? FaseDeJefe.AvisoEmbestida : FaseDeJefe.Anticipacion);
            return RunEvent.JefeApunta;
        }

        /// <summary>Morteros seguidos del ataque elevado numero "indice" (desde 0): la serie se repite.</summary>
        public static int DisparosDelElevado(int[] serie, int indice)
        {
            if (serie == null || serie.Length == 0)
                return 1;
            return Mathf.Max(1, serie[indice % serie.Length]);
        }

        /// <summary>
        /// El carril del segundo mortero: el de la moto si ya salio del primero; si no, uno vecino. Asi
        /// los dos caen en carriles distintos y siempre queda uno libre.
        /// </summary>
        int OtroCarril(int primero)
        {
            if (s.Lane != primero)
                return s.Lane;
            if (primero != 1)
                return 1;
            return elevados % 2 == 0 ? 0 : 2;
        }

        bool HayProyectilQueAmenaza
        {
            get
            {
                foreach (var p in Proyectiles)
                    if (p.Activo && (p.Elevado || p.Relativo - Jefe.radioProyectil <= config.motoNariz))
                        return true;
                return false;
            }
        }

        RunEvent EmitirSiToca()
        {
            if (emitido || TiempoFase < (AtaqueElevado ? Jefe.emisionElevado : 0f))
                return RunEvent.None;

            emitido = true;
            var p = LugarLibre();
            p.Activo = true;
            p.Elevado = AtaqueElevado;
            p.Carril = Carril;
            p.Salida = -Separacion + (AtaqueElevado ? Jefe.bocaElevado : Jefe.bocaFrontal);
            p.Relativo = p.Salida;
            p.Avance = 0f;
            p.vida = 0f;
            p.Serie++;
            UltimoDisparo = p;
            return RunEvent.JefeDispara;
        }

        // Mejor uno que ya no muestre su impacto. Con todos en el aire (no deberia pasar) se pisa el primero.
        ProyectilDelJefe LugarLibre()
        {
            foreach (var p in Proyectiles)
                if (!p.Activo && p.ImpactoRestante <= 0f)
                    return p;
            foreach (var p in Proyectiles)
                if (!p.Activo)
                    return p;
            return Proyectiles[0];
        }

        RunEvent AvanzarProyectil(float dt)
        {
            var events = RunEvent.None;
            foreach (var p in Proyectiles)
            {
                if (!p.Activo)
                    continue;

                p.vida += dt;
                if (p.vida >= Jefe.vidaProyectil)
                {
                    p.Activo = false;
                    continue;
                }

                events |= p.Elevado ? AvanzarElevado(p, dt) : AvanzarFrontal(p, dt);
                if (s.Phase == RunPhase.Lost)
                    break;
            }
            return events;
        }

        RunEvent AvanzarFrontal(ProyectilDelJefe p, float dt)
        {
            p.Relativo += Jefe.velocidadProyectil * dt;

            if (p.Relativo > Jefe.salidaProyectil)
            {
                p.Activo = false;
                return RunEvent.None;
            }

            bool toca = p.Relativo + Jefe.radioProyectil >= -config.motoCola &&
                        p.Relativo - Jefe.radioProyectil <= config.motoNariz;
            if (!toca || !EnElCarril(p.Carril) || s.Airborne || Invulnerable > 0f)
                return RunEvent.None;

            Impactar(p, p.Relativo, false);
            return Golpear();
        }

        RunEvent AvanzarElevado(ProyectilDelJefe p, float dt)
        {
            p.Avance = Mathf.Min(1f, p.Avance + dt / Jefe.vueloElevado);
            p.Relativo = Mathf.Lerp(p.Salida, 0f, p.Avance);
            if (p.Avance < 1f)
                return RunEvent.None;

            // Cae en un solo punto: el efecto no deja una zona que siga lastimando.
            Impactar(p, 0f, true);
            bool pega = EnElCarril(p.Carril) && !s.Airborne && Invulnerable <= 0f;
            return RunEvent.JefeImpacto | (pega ? Golpear() : RunEvent.None);
        }

        void Impactar(ProyectilDelJefe p, float relativo, bool enElPiso)
        {
            p.Activo = false;
            p.ImpactoRestante = Jefe.impacto;
            p.ImpactoRelativo = relativo;
            p.ImpactoEnElPiso = enElPiso;
            p.SerieImpacto++;
            UltimoImpacto = p;
        }

        bool EnElCarril(int carril) => Mathf.Abs(s.LaneVisual - carril) <= 0.5f;

        RunEvent ChocarConLaMoto()
        {
            bool llega = Separacion - Jefe.frente <= config.motoCola;
            if (embestidaPego || !llega || !EnElCarril(Carril) || s.Airborne || Invulnerable > 0f)
                return RunEvent.None;

            embestidaPego = true;
            return Golpear();
        }

        RunEvent Golpear()
        {
            Corazones = Mathf.Max(0, Corazones - 1);
            Invulnerable = Jefe.invulnerable;
            s.StunRemaining = Mathf.Max(s.StunRemaining, Jefe.frenoPorGolpe);

            var events = RunEvent.Crashed | RunEvent.JefeGolpea;
            if (Corazones > 0)
                return events;

            s.Phase = RunPhase.Lost;
            s.Loss = LossReason.AlcanzadaPorJefe;
            return events | RunEvent.Lost;
        }

        RunEvent AvanzarRecursos()
        {
            var events = RunEvent.None;
            if (Recoger(CajaDeBalas))
            {
                s.Ammo = Mathf.Min(config.ammoMax, s.Ammo + Jefe.balasPorCaja);
                events |= RunEvent.PickedUp;
            }
            if (Recoger(GranadaSuelta))
            {
                s.Granadas = Mathf.Min(config.granadaMax, s.Granadas + 1);
                events |= RunEvent.PickedUp;
            }
            return events;
        }

        // Se alcanza igual que los recolectables del recorrido. Lo que se pasa de largo se pierde.
        bool Recoger(RecursoDelDuelo recurso)
        {
            if (!recurso.Activo || s.PlayerX + config.motoNariz < recurso.X)
                return false;

            recurso.Activo = false;
            return Mathf.Abs(s.LaneVisual - recurso.Carril) <= config.imanCarriles + 0.01f && !s.Airborne;
        }

        /// <summary>
        /// Una bala hacia atras por ese carril. Devuelve false si no lo alcanza: sigue viaje hacia la
        /// horda. Si lo alcanza fuera del aturdimiento rebota: se gasta la bala y no lastima.
        /// </summary>
        public bool RecibirBala(int carril, float alcance, out RunEvent events)
        {
            events = RunEvent.None;
            if (!Peleando || Fase == FaseDeJefe.Entrada || carril != Carril || Separacion > alcance)
                return false;

            events = Vulnerable ? Lastimar(Jefe.danoBala) : RunEvent.JefeRebota;
            return true;
        }

        /// <summary>Una explosion en esos metros. Como con la horda, alcanza con el radio.</summary>
        public RunEvent RecibirExplosion(float x)
        {
            if (!Peleando || Fase == FaseDeJefe.Entrada || Mathf.Abs(X - x) > config.explosionRadius)
                return RunEvent.None;

            return Vulnerable ? Lastimar(Jefe.danoGranada) : RunEvent.JefeRebota;
        }

        RunEvent Lastimar(int dano)
        {
            Vida = Mathf.Max(0, Vida - dano);
            DanoRestante = Jefe.dano;
            if (Vida > 0)
                return RunEvent.JefeHerido;

            Pasar(FaseDeJefe.Derrotado);
            foreach (var p in Proyectiles)
                p.Activo = false;
            disparosRestantes = 0;
            CajaDeBalas.Activo = false;
            GranadaSuelta.Activo = false;
            DanoRestante = 0f;
            run.FijarMeta(s.PlayerX + Jefe.metrosAlRefugio);
            return RunEvent.JefeHerido | RunEvent.JefeDerrotado;
        }
    }
}
