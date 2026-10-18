using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Player;

namespace Zombineta.Auxilio
{
    /// <summary>
    /// La pelea a pie mientras llega el auxilio. C# plano, como el runner: se le hace Step con
    /// un dt fijo y se verifica. Mueve a los mismos zombies de la horda (asi la vista de la
    /// horda los dibuja tal cual), a la protagonista a pie y, al final, a la furgoneta.
    ///
    /// La arena es lo que se ve en pantalla alrededor de donde quedo parada la moto: de un
    /// lado llega la horda, del otro algunos que vienen de frente. Solo pelean unos pocos a la
    /// vez; el resto de la horda queda estacionada fuera de cuadro.
    /// </summary>
    public sealed class PeleaSimulation
    {
        // Distancia a la que un zombie de otro carril se decide a cambiarse al de ella.
        const float DistanciaParaCambiarCarril = 6f;
        // Donde esperan, fuera de cuadro, los zombies que no estan peleando.
        const float Estacionamiento = 80f;
        // Media anchura de la furgoneta al arrollar, en metros.
        const float AnchoFurgoneta = 2.5f;

        readonly GameConfig c;
        readonly RunState s;
        readonly HordeSimulation h;
        readonly System.Random rng;

        bool[] activo;
        float centro;
        int lado = 1;
        bool cargo;

        public PeleaSimulation(GameConfig config, RunState state, HordeSimulation horde, int seed)
        {
            c = config;
            s = state;
            h = horde;
            rng = new System.Random(seed);
        }

        /// <summary>Metros: el centro de la arena (donde quedo la moto).</summary>
        public float Centro => centro;

        /// <summary>Si el zombie de ese indice esta peleando (los demas esperan fuera de cuadro).</summary>
        public bool Activo(int indice) => activo != null && indice < activo.Length && activo[indice];

        public bool PuedeEmpezar => c.ayudas > 0 && s.Ayudas > 0;

        /// <summary>Se gasta un auxilio y la protagonista se baja: la horda la alcanza y la rodea.</summary>
        public RunEvent Empezar()
        {
            s.Ayudas--;
            s.Phase = RunPhase.Pelea;
            s.Vida = c.vidaPelea;
            s.PeleaRestante = c.peleaSegundos;
            s.Invulnerable = 0f;
            s.GolpeEnfriamiento = 0f;
            s.Golpeando = 0f;
            s.FurgonetaEnCamino = false;
            s.HeadlightOn = false;
            s.StunRemaining = 0f;
            s.Mode = DriveMode.Normal;
            s.Mira = 1;
            s.Lane = Mathf.Clamp(s.Lane, 0, RunSimulation.LaneCount - 1);

            centro = s.PlayerX;
            s.PeatonX = centro;
            cargo = false;

            var units = h.Units;
            activo = new bool[units.Length];
            int enArena = Mathf.Min(c.zombiesEnArena, units.Length);
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (i < enArena)
                {
                    activo[i] = true;
                    // La mayoria llega con la horda, por atras; uno de cada tres viene de frente.
                    int desde = i % 3 == 2 ? 1 : -1;
                    float x = desde < 0
                        ? centro - 5f - (float)rng.NextDouble() * 9f
                        : centro + c.arenaMedioAncho + 1f + (float)rng.NextDouble() * 4f;
                    Preparar(u, x, rng.Next(RunSimulation.LaneCount), nuevo: !u.Alive);
                }
                else
                {
                    u.X = centro - Estacionamiento;
                    u.Stagger = 0f;
                }
            }
            h.RecomputeFront();
            s.HordeX = h.FrontX;
            return RunEvent.PeleaEmpieza;
        }

        public RunEvent Step(PlayerIntent intent, float dt)
        {
            if (s.FurgonetaEnCamino)
                return PasoFurgoneta(dt);

            var ev = RunEvent.None;

            s.PeleaRestante = Mathf.Max(0f, s.PeleaRestante - dt);
            s.Invulnerable = Mathf.Max(0f, s.Invulnerable - dt);
            s.GolpeEnfriamiento = Mathf.Max(0f, s.GolpeEnfriamiento - dt);
            s.Golpeando = Mathf.Max(0f, s.Golpeando - dt);

            ev |= MoverProtagonista(intent, dt);
            ev |= Pegar(intent);
            ev |= Disparar(intent);
            ev |= MoverZombies(dt);
            if (s.Phase != RunPhase.Pelea)
                return ev; // la derribaron

            if (s.PeleaRestante <= 0f)
            {
                s.FurgonetaEnCamino = true;
                s.FurgonetaX = centro - c.furgonetaDesde;
                s.Invulnerable = float.MaxValue; // con la furgoneta en camino ya no la lastiman
                ev |= RunEvent.AuxilioLlega;
            }
            return ev;
        }

        // --- La protagonista ---------------------------------------------------

        RunEvent MoverProtagonista(PlayerIntent intent, float dt)
        {
            var ev = RunEvent.None;
            if (Mathf.Abs(intent.MoveX) > 0.01f)
            {
                s.Mira = intent.MoveX > 0f ? 1 : -1;
                s.PeatonX = Mathf.Clamp(s.PeatonX + intent.MoveX * c.peatonVelocidad * dt,
                    centro - c.arenaMedioAncho, centro + c.arenaMedioAncho);
            }

            if (intent.LaneDelta != 0)
            {
                int destino = Mathf.Clamp(s.Lane + System.Math.Sign(intent.LaneDelta), 0, RunSimulation.LaneCount - 1);
                if (destino != s.Lane)
                {
                    s.Lane = destino;
                    ev |= RunEvent.LaneChanged;
                }
            }
            return ev;
        }

        RunEvent Pegar(PlayerIntent intent)
        {
            if (!intent.Golpe || s.GolpeEnfriamiento > 0f)
                return RunEvent.None;

            s.GolpeEnfriamiento = c.golpeEnfriamiento;
            s.Golpeando = c.golpeDuracion;

            var u = MasCercano(s.PeatonX, s.Lane, s.Mira, c.golpeAlcance);
            if (u == null)
                return RunEvent.GolpeAlAire;

            h.Golpear(u, s.Mira, c.golpeEmpuje, c.golpeFreno);
            return RunEvent.Golpe;
        }

        RunEvent Disparar(PlayerIntent intent)
        {
            if (!intent.Fire)
                return RunEvent.None;
            if (s.Ammo <= 0)
                return RunEvent.ShotDenied;

            s.Ammo--;
            var u = MasCercano(s.PeatonX, s.Lane, s.Mira, c.shotRangeMeters);
            if (u != null)
            {
                h.HitZombie(u, s.PeatonX);
                return RunEvent.Shot;
            }
            h.MissToward(s.PeatonX, s.PeatonX + s.Mira * c.shotRangeMeters, s.Lane);
            return RunEvent.Shot | RunEvent.ShotMissed;
        }

        /// <summary>El zombie vivo y peleando mas cercano en ese carril, hacia donde mira, dentro del alcance.</summary>
        ZombieUnit MasCercano(float desde, int carril, int mira, float alcance)
        {
            ZombieUnit mejor = null;
            float mejorDist = float.MaxValue;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (!activo[i] || !u.Alive || u.Lane != carril)
                    continue;
                float d = (u.X - desde) * mira;
                // Un poco hacia atras tambien cuenta: pegado a ella, aunque este apenas detras.
                if (d < -0.3f || d > alcance || d >= mejorDist)
                    continue;
                mejor = u;
                mejorDist = d;
            }
            return mejor;
        }

        // --- Los zombies -------------------------------------------------------

        RunEvent MoverZombies(float dt)
        {
            var ev = RunEvent.None;
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                if (!activo[i])
                    continue;
                var u = units[i];

                if (!u.Alive)
                {
                    // El cadaver queda un rato y otro entra por un costado.
                    u.CorpseLeft -= dt;
                    if (u.CorpseLeft <= 0f)
                    {
                        lado = -lado;
                        float x = lado < 0
                            ? centro - c.arenaMedioAncho - 2f - (float)rng.NextDouble() * 3f
                            : centro + c.arenaMedioAncho + 2f + (float)rng.NextDouble() * 3f;
                        Preparar(u, x, rng.Next(RunSimulation.LaneCount), nuevo: true);
                    }
                    continue;
                }

                if (u.Stagger > 0f)
                {
                    u.Stagger = Mathf.Max(0f, u.Stagger - dt);
                    continue;
                }

                float dx = s.PeatonX - u.X;
                u.Mira = dx >= 0f ? 1 : -1;

                if (u.Lane != s.Lane)
                {
                    u.CambioCarril -= dt;
                    if (Mathf.Abs(dx) < DistanciaParaCambiarCarril && u.CambioCarril <= 0f)
                    {
                        u.Lane += System.Math.Sign(s.Lane - u.Lane);
                        u.CambioCarril = 0.6f + (float)rng.NextDouble() * 0.6f;
                    }
                }

                bool enRango = u.Lane == s.Lane && Mathf.Abs(dx) <= c.zombieAlcance;

                if (u.Preparando >= 0f)
                {
                    u.Preparando += dt;
                    if (u.Preparando >= c.zombiePreparacion)
                    {
                        u.Preparando = -1f;
                        u.Enfriamiento = c.zombieEnfriamiento;
                        if (enRango && s.Invulnerable <= 0f)
                            ev |= Lastimar(System.Math.Sign(dx));
                        if (s.Phase != RunPhase.Pelea)
                            return ev;
                    }
                    continue;
                }

                if (enRango)
                {
                    u.Enfriamiento -= dt;
                    if (u.Enfriamiento <= 0f)
                        u.Preparando = 0f;
                    continue;
                }

                // Se acerca: en su carril hasta quedar a tiro; en otro, hasta ponerse a la par.
                float objetivo = u.Lane == s.Lane ? c.zombieAlcance * 0.8f : 0.4f;
                if (Mathf.Abs(dx) > objetivo)
                {
                    float v = c.zombieVelocidadPelea * h.Type(u.Type).speedMultiplier;
                    u.X += Mathf.Sign(dx) * Mathf.Min(v * dt, Mathf.Abs(dx) - objetivo);
                }
            }
            return ev;
        }

        RunEvent Lastimar(int desde)
        {
            s.Vida--;
            s.Invulnerable = c.invulnerableTrasGolpe;
            // El golpe la corre un poco, alejandola del que pego.
            s.PeatonX = Mathf.Clamp(s.PeatonX + desde * 0.8f, centro - c.arenaMedioAncho, centro + c.arenaMedioAncho);
            if (s.Vida > 0)
                return RunEvent.Lastimada;

            s.Phase = RunPhase.Lost;
            s.Loss = LossReason.Derribada;
            return RunEvent.Lastimada | RunEvent.Lost;
        }

        void Preparar(ZombieUnit u, float x, int carril, bool nuevo)
        {
            if (nuevo)
                u.Generation++;
            u.Alive = true;
            u.Cause = DeathCause.None;
            u.CorpseLeft = 0f;
            u.Stagger = 0f;
            u.X = x;
            u.Lane = carril;
            u.Golpes = h.Type(u.Type).golpesParaMorir;
            u.Preparando = -1f;
            u.Enfriamiento = 0.3f + (float)rng.NextDouble() * 0.5f;
            u.CambioCarril = (float)rng.NextDouble() * 0.8f;
            u.Mira = x < s.PeatonX ? 1 : -1;
        }

        // --- La furgoneta ------------------------------------------------------

        RunEvent PasoFurgoneta(float dt)
        {
            var ev = RunEvent.None;
            float antes = s.FurgonetaX;
            s.FurgonetaX += c.furgonetaVelocidad * dt;

            // Arrolla a todo lo que cruza, en los tres carriles.
            var units = h.Units;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (!activo[i] || !u.Alive)
                    continue;
                if (u.X >= antes - AnchoFurgoneta && u.X <= s.FurgonetaX + AnchoFurgoneta)
                {
                    h.Kill(u, DeathCause.RunOver);
                    ev |= RunEvent.RanOver;
                }
            }

            // Al pasar por al lado de ella le deja el bidon.
            if (!cargo && s.FurgonetaX >= s.PeatonX)
            {
                cargo = true;
                s.Fuel = c.fuelMax;
                ev |= RunEvent.PickedUp;
            }

            if (s.FurgonetaX >= centro + c.furgonetaDesde)
                ev |= Terminar();
            return ev;
        }

        RunEvent Terminar()
        {
            s.FurgonetaEnCamino = false;
            s.Invulnerable = 0f;
            s.Phase = RunPhase.Running;
            s.PlayerX = s.PeatonX;
            s.Fuel = c.fuelMax;
            s.Vida = 0;
            h.Reset(s.PlayerX - c.gapTrasAuxilio);
            s.HordeX = h.FrontX;
            activo = null;
            return RunEvent.AuxilioTermina;
        }
    }
}
