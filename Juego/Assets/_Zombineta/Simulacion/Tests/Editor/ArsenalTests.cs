using NUnit.Framework;
using UnityEngine;
using Zombineta.Core;
using Zombineta.Enemies;
using Zombineta.Level;
using Zombineta.Player;

namespace Zombineta.Tests
{
    /// <summary>Las granadas: arma secundaria de la moto, se recogen en el camino y se lanzan hacia atras.</summary>
    public class ArsenalTests
    {
        const float Dt = 1f / 60f;

        static GameConfig MakeConfig()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.goalDistance = 5000f;
            c.startingGap = 60f;
            c.normalSpeed = 10f;
            c.fuelMax = 100f;
            c.fuelBurnPerSecond = 1f;
            c.batteryMax = 100f;
            c.ammoMax = 6;
            c.ammoAtStart = 3;
            c.hordeBaseSpeed = 10f;
            c.rubberBandMaxBonus = 0f;
            c.hordeCount = 8;
            c.hordeDepthMeters = 12f;
            c.corpseSeconds = 1f;
            c.hordeSeed = 11;
            c.shotRangeMeters = 60f;
            c.explosionRadius = 8f;
            c.explosionScareRadius = 16f;
            c.explosionScareSeconds = 1f;
            c.deathScareRadius = 4f;
            c.deathScareSeconds = 0.5f;
            c.retrocesoAciertos = 0;
            c.granadaMax = 3;
            c.granadaInicial = 2;
            c.granadaDistancia = 10f;
            c.granadaVuelo = 0.45f;
            c.hordaSinRegenerar = 5f;
            c.bombaMax = 2;
            c.bombaInicial = 0;
            c.fuelPickupAmount = 25f;
            return c;
        }

        static RunSimulation Make(GameConfig c = null)
        {
            c = c ?? MakeConfig();
            var r = ScriptableObject.CreateInstance<ZombieRoster>();
            r.types.Add(new ZombieType { name = "Test", speedMultiplier = 1f, deathChance = 1f, spawnWeight = 1f });
            c.zombies = r;
            return new RunSimulation(c);
        }

        static ZombieUnit Poner(RunSimulation sim, int i, float x, int lane)
        {
            var u = sim.Horde.Units[i];
            u.Alive = true; u.X = x; u.Lane = lane; u.Stagger = 0f; u.CorpseLeft = 0f; u.Type = 0;
            return u;
        }

        static void SinHorda(RunSimulation sim)
        {
            foreach (var u in sim.Horde.Units) { u.Alive = false; u.CorpseLeft = 999f; }
        }

        static RunEvent Avanzar(RunSimulation sim, float segundos, PlayerIntent intent)
        {
            var todo = RunEvent.None;
            for (int i = 0; i < Mathf.RoundToInt(segundos / Dt); i++)
                todo |= sim.Tick(intent, Dt);
            return todo;
        }

        static PlayerIntent Lanzar() => new PlayerIntent { Mode = DriveMode.Normal, Granada = true };

        static PlayerIntent Quieta => new PlayerIntent { Mode = DriveMode.Normal };

        [Test]
        public void TheRunStartsWithTheConfiguredGrenades_AndNeverMoreThanTheMaximum()
        {
            var c = MakeConfig();
            c.granadaInicial = 9;
            var sim = Make(c);
            Assert.AreEqual(3, sim.State.Granadas);
        }

        [Test]
        public void ThrowingAGrenade_SpendsOne_AndItFliesForAWhile()
        {
            var sim = Make();
            var ev = sim.Tick(Lanzar(), Dt);

            Assert.IsTrue((ev & RunEvent.GranadaLanzada) != 0);
            Assert.AreEqual(1, sim.State.Granadas);
            Assert.Greater(sim.State.GranadaVuelo, 0f);
        }

        [Test]
        public void TheGrenade_ExplodesTenMetersBehindTheBike_AndTakesTheHordeAroundThere()
        {
            var sim = Make();
            SinHorda(sim);
            float px = sim.State.PlayerX;
            // Se mueven igual que la moto: al caer la granada siguen donde estaban respecto de ella.
            var cerca = Poner(sim, 0, px - 11f, 1);
            var otroCarril = Poner(sim, 1, px - 9f, 2);
            var lejos = Poner(sim, 2, px - 45f, 1);
            sim.Horde.RecomputeFront();

            var ev = Avanzar(sim, 0.2f, Lanzar());
            ev |= Avanzar(sim, 0.5f, Quieta);

            Assert.IsTrue((ev & RunEvent.GranadaExplota) != 0);
            Assert.IsTrue((ev & RunEvent.Explosion) != 0);
            Assert.IsFalse(cerca.Alive, "a pocos metros de donde cae");
            Assert.IsFalse(otroCarril.Alive, "la explosion no respeta carriles");
            Assert.IsTrue(lejos.Alive, "a 35 m no le llega");
            Assert.GreaterOrEqual(sim.State.UltimaExplosionMuertos, 2);
        }

        [Test]
        public void TheGrenadeLandsWhereItWasAimed_RelativeToTheBikeAtTheMomentItLands()
        {
            var sim = Make();
            sim.Tick(Lanzar(), Dt);
            Avanzar(sim, 0.2f, Quieta);

            Assert.AreEqual(sim.State.PlayerX - 10f, sim.State.GranadaX, 0.01f);
        }

        [Test]
        public void AfterAGrenade_TheFallenAreNotReplacedForAWhile_ButTheyAreAfterwards()
        {
            var sim = Make();
            SinHorda(sim);
            float px = sim.State.PlayerX;
            var caido = Poner(sim, 0, px - 12f, 1);
            Poner(sim, 1, px - 20f, 0);
            sim.Horde.RecomputeFront();

            Avanzar(sim, 0.2f, Lanzar());
            Avanzar(sim, 0.5f, Quieta);
            Assert.IsFalse(caido.Alive);
            Assert.Greater(sim.Horde.SinRegenerar, 0f);

            Avanzar(sim, 3.0f, Quieta);   // el cadaver ya paso su segundo de cuerpo, pero no hay reposicion
            Assert.IsFalse(caido.Alive, "durante hordaSinRegenerar nadie lo repone");

            Avanzar(sim, 3.0f, Quieta);   // pasados los 5 s vuelve a entrar por el fondo
            Assert.IsTrue(caido.Alive, "despues si");
        }

        [Test]
        public void WithoutGrenades_ThereIsNothingToThrow()
        {
            var c = MakeConfig();
            c.granadaInicial = 0;
            var sim = Make(c);

            var ev = sim.Tick(Lanzar(), Dt);

            Assert.IsTrue((ev & RunEvent.GranadaDenegada) != 0);
            Assert.AreEqual(0, sim.State.Granadas);
            Assert.AreEqual(0f, sim.State.GranadaVuelo);
        }

        [Test]
        public void OnlyOneGrenadeFliesAtATime()
        {
            var sim = Make();
            sim.Tick(Lanzar(), Dt);
            var ev = sim.Tick(Lanzar(), Dt);   // todavia en el aire

            Assert.IsTrue((ev & RunEvent.GranadaDenegada) != 0);
            Assert.AreEqual(1, sim.State.Granadas, "la segunda no se gasto");
        }

        [Test]
        public void TheGrenadeItem_IsPickedUpOnTheRoad_UpToTheMaximum()
        {
            var c = MakeConfig();
            c.granadaInicial = 2;
            var sim = Make(c);
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.Add(new LevelEntry(30f, 1, LevelEntryKind.Granada));
            def.entries.Add(new LevelEntry(60f, 1, LevelEntryKind.Granada));
            var level = new LevelRuntime(def);

            for (int i = 0; i < 60 * 8; i++)
            {
                float antes = sim.State.PlayerX;
                sim.Tick(Quieta, Dt);
                level.Collect(sim, antes, sim.State.PlayerX);
            }

            Assert.IsTrue(level.Items[0].Consumed && level.Items[1].Consumed);
            Assert.AreEqual(3, sim.State.Granadas, "dos mas, pero el maximo es 3");
        }

        [Test]
        public void TheBombItem_IsPickedUpOnTheRoad_ForTheFightOnFoot()
        {
            var sim = Make();
            var def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.entries.Add(new LevelEntry(30f, 1, LevelEntryKind.BombaTemporal));
            var level = new LevelRuntime(def);

            for (int i = 0; i < 60 * 5; i++)
            {
                float antes = sim.State.PlayerX;
                sim.Tick(Quieta, Dt);
                level.Collect(sim, antes, sim.State.PlayerX);
            }

            Assert.AreEqual(1, sim.State.Bombas);
            Assert.AreEqual(2, sim.State.Granadas, "las granadas no se tocan");
        }

        [Test]
        public void TheNewItems_AreResources_SoTheyAlsoMagnetizeAndHideInTheFight()
        {
            Assert.IsTrue(LevelRuntime.EsRecurso(LevelEntryKind.Granada));
            Assert.IsTrue(LevelRuntime.EsRecurso(LevelEntryKind.BombaTemporal));
            Assert.IsFalse(LevelRuntime.EsRecurso(LevelEntryKind.Barrel));
        }

        [Test]
        public void Reset_RestoresTheStartingArsenal()
        {
            var sim = Make();
            sim.Tick(Lanzar(), Dt);
            sim.State.Bombas = 2;

            sim.Reset();

            Assert.AreEqual(2, sim.State.Granadas);
            Assert.AreEqual(0, sim.State.Bombas);
            Assert.AreEqual(0f, sim.State.GranadaVuelo);
        }
    }
}
