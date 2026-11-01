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

        // --- Proyectil: uno solo a la vez ---
        public bool ProyectilActivo { get; private set; }
        public bool ProyectilElevado { get; private set; }
        public int ProyectilCarril { get; private set; }
        /// <summary>Metros respecto de la moto: negativo, todavia detras.</summary>
        public float ProyectilRelativo { get; private set; }
        /// <summary>Donde salio, respecto de la moto.</summary>
        public float ProyectilSalida { get; private set; }
        /// <summary>Del recorrido del proyectil elevado: 0 al salir, 1 al caer.</summary>
        public float ProyectilAvance { get; private set; }
        float proyectilVida;

        // --- Impacto: el efecto que queda donde pego ---
        public float ImpactoRestante { get; private set; }
        public float ImpactoRelativo { get; private set; }
        public int ImpactoCarril { get; private set; }
        public bool ImpactoEnElPiso { get; private set; }

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

        /// <summary>Hay un carril fijado para el ataque en curso: la vista lo marca en el piso.</summary>
        public bool AvisoDeCarril =>
            Fase == FaseDeJefe.Anticipacion || Fase == FaseDeJefe.AvisoEmbestida ||
            (Fase == FaseDeJefe.Disparo && !emitido) ||
            (Fase == FaseDeJefe.Embestida && !embestidaPego) ||
            (ProyectilActivo && ProyectilElevado);

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
            ProyectilActivo = false;
            ImpactoRestante = 0f;
            CajaDeBalas.Activo = false;
            GranadaSuelta.Activo = false;
            paso = 0;
            segundaMitad = false;
            emitido = false;
            embestidaPego = false;
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
            ImpactoRestante = Mathf.Max(0f, ImpactoRestante - dt);
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
                    // Nunca empieza otro ataque con un proyectil en el aire: podria cerrar la salida segura.
                    if (TiempoFase >= Jefe.pausaEntreAcciones && !ProyectilActivo)
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
                        Pasar(FaseDeJefe.Persecucion);
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
            Pasar(accion == AccionDeJefe.Embestida ? FaseDeJefe.AvisoEmbestida : FaseDeJefe.Anticipacion);
            return RunEvent.JefeApunta;
        }

        RunEvent EmitirSiToca()
        {
            if (emitido || TiempoFase < (AtaqueElevado ? Jefe.emisionElevado : 0f))
                return RunEvent.None;

            emitido = true;
            ProyectilActivo = true;
            ProyectilElevado = AtaqueElevado;
            ProyectilCarril = Carril;
            ProyectilSalida = -Separacion + (AtaqueElevado ? Jefe.bocaElevado : Jefe.bocaFrontal);
            ProyectilRelativo = ProyectilSalida;
            ProyectilAvance = 0f;
            proyectilVida = 0f;
            return RunEvent.JefeDispara;
        }

        RunEvent AvanzarProyectil(float dt)
        {
            if (!ProyectilActivo)
                return RunEvent.None;

            proyectilVida += dt;
            if (proyectilVida >= Jefe.vidaProyectil)
            {
                ProyectilActivo = false;
                return RunEvent.None;
            }

            return ProyectilElevado ? AvanzarElevado(dt) : AvanzarFrontal(dt);
        }

        RunEvent AvanzarFrontal(float dt)
        {
            ProyectilRelativo += Jefe.velocidadProyectil * dt;

            if (ProyectilRelativo > Jefe.salidaProyectil)
            {
                ProyectilActivo = false;
                return RunEvent.None;
            }

            bool toca = ProyectilRelativo + Jefe.radioProyectil >= -config.motoCola &&
                        ProyectilRelativo - Jefe.radioProyectil <= config.motoNariz;
            if (!toca || !EnElCarril(ProyectilCarril) || s.Airborne || Invulnerable > 0f)
                return RunEvent.None;

            Impactar(ProyectilRelativo, false);
            return Golpear();
        }

        RunEvent AvanzarElevado(float dt)
        {
            ProyectilAvance = Mathf.Min(1f, ProyectilAvance + dt / Jefe.vueloElevado);
            ProyectilRelativo = Mathf.Lerp(ProyectilSalida, 0f, ProyectilAvance);
            if (ProyectilAvance < 1f)
                return RunEvent.None;

            // Cae en un solo punto: el efecto no deja una zona que siga lastimando.
            Impactar(0f, true);
            bool pega = EnElCarril(ProyectilCarril) && !s.Airborne && Invulnerable <= 0f;
            return RunEvent.JefeImpacto | (pega ? Golpear() : RunEvent.None);
        }

        void Impactar(float relativo, bool enElPiso)
        {
            ProyectilActivo = false;
            ImpactoRestante = Jefe.impacto;
            ImpactoRelativo = relativo;
            ImpactoCarril = ProyectilCarril;
            ImpactoEnElPiso = enElPiso;
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
            ProyectilActivo = false;
            CajaDeBalas.Activo = false;
            GranadaSuelta.Activo = false;
            DanoRestante = 0f;
            run.FijarMeta(s.PlayerX + Jefe.metrosAlRefugio);
            return RunEvent.JefeHerido | RunEvent.JefeDerrotado;
        }
    }
}
