using NUnit.Framework;
using Zombineta.Core;
using Zombineta.Telemetria;

namespace Zombineta.Tests
{
    /// <summary>La telemetria de los playtests: filas del CSV y acumuladores por fase.</summary>
    public class TelemetriaTests
    {
        static RunState Estado() => new RunState { Phase = RunPhase.Running, Fuel = 40f, Ammo = 3, Vida = 4, PlayerX = 123.45f };

        [Test]
        public void ARow_HasOneColumnPerHeaderField_AndTheDetailInQuotes()
        {
            var sesion = new SesionTelemetria("Level_01", 2);
            string fila = sesion.Fila(12.34f, Estado(), "oleada", "n=2;x=1");

            Assert.AreEqual("12.3;Level_01;2;corriendo;123.5;40.0;3;4;oleada;\"n=2;x=1\"", fila);
            Assert.AreEqual(SesionTelemetria.Cabecera.Split(';').Length, 10);
        }

        [Test]
        public void TheSeparatorInsideNamesAndEvents_DoesNotBreakTheColumns()
        {
            string fila = SesionTelemetria.Fila(0f, "a;b", 1, "pelea", 0f, 0f, 0, 0, "ev;x", "");
            Assert.AreEqual("0.0;a,b;1;pelea;0.0;0.0;0;0;ev,x;\"\"", fila);
        }

        [Test]
        public void Numbers_UseADotWhateverTheComputerLanguage()
        {
            var cultura = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");
                string fila = new SesionTelemetria("n", 1).Fila(1.5f, Estado(), "inicio", "");
                StringAssert.StartsWith("1.5;", fila);
                StringAssert.Contains(";123.5;", fila);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultura;
            }
        }

        [Test]
        public void TimeIsAddedToThePhaseTheRunIsIn()
        {
            var sesion = new SesionTelemetria("n", 1);
            var s = Estado();
            sesion.Tiempo(s, 2f);

            s.FrenadoRestante = 1f;
            sesion.Tiempo(s, 0.5f);

            s.FrenadoRestante = -1f;
            s.Phase = RunPhase.Pelea;
            sesion.Tiempo(s, 30f);

            s.Phase = RunPhase.Won;
            sesion.Tiempo(s, 99f);   // ya termino: no suma

            Assert.AreEqual(2f, sesion.SegundosCorriendo, 0.001f);
            Assert.AreEqual(0.5f, sesion.SegundosFrenando, 0.001f);
            Assert.AreEqual(30f, sesion.SegundosPeleando, 0.001f);
            Assert.AreEqual(32.5f, sesion.SegundosTotales, 0.001f);
        }

        [Test]
        public void TheEvents_LeaveARowEach_AndTheCountersGoUp()
        {
            var sesion = new SesionTelemetria("n", 1);
            var s = Estado();
            s.Nota = NotaPelea.A;
            s.PeleaBajas = 7;

            var filas = sesion.Eventos(5f, s, RunEvent.Crashed | RunEvent.Lastimada | RunEvent.AuxilioTermina);

            Assert.AreEqual(3, filas.Count);
            StringAssert.Contains(";choque;", filas[0]);
            StringAssert.Contains(";golpe_recibido;", filas[1]);
            StringAssert.Contains("nota=A", filas[2]);
            Assert.AreEqual(1, sesion.Choques);
            Assert.AreEqual(1, sesion.GolpesRecibidos);
            Assert.AreEqual(7, sesion.Bajas);
            Assert.AreEqual(0, sesion.Eventos(6f, s, RunEvent.None).Count);
        }

        [Test]
        public void TheSummary_GathersWhatHappened()
        {
            var sesion = new SesionTelemetria("n", 1);
            var s = Estado();
            sesion.Tiempo(s, 60f);
            sesion.Eventos(1f, s, RunEvent.PeleaEmpieza);
            sesion.Eventos(2f, s, RunEvent.PeleaReintenta);
            s.Phase = RunPhase.Won;

            string resumen = sesion.Resumen(70f, s, "ganada");

            StringAssert.Contains(";resumen;", resumen);
            StringAssert.Contains("resultado=ganada", resumen);
            StringAssert.Contains("duracion=60.0", resumen);
            StringAssert.Contains("auxilios=1", resumen);
            StringAssert.Contains("reintentos=1", resumen);
        }

        [Test]
        public void TheFileName_HasTheDateAndTime()
        {
            var nombre = SesionTelemetria.NombreDeArchivo(new System.DateTime(2026, 10, 18, 9, 5, 3));
            Assert.AreEqual("sesion_20261018_090503.csv", nombre);
        }

        [Test]
        public void TheWeaponsAndTheWaves_LeaveTheirRows_AndTheCountersGoUp()
        {
            var sesion = new SesionTelemetria("n", 1);
            var s = Estado();
            s.Granadas = 2;
            s.UltimaExplosionMuertos = 4;
            s.Oleada = 1; s.Oleadas = 4; s.OleadaTotal = 5; s.PeleaTiempo = 40f;
            s.Bombas = 1;

            var filas = sesion.Eventos(3f, s,
                RunEvent.GranadaLanzada | RunEvent.GranadaExplota | RunEvent.BombaPuesta | RunEvent.BombaExplota |
                RunEvent.OleadaNueva | RunEvent.PeleaGanada);
            string todas = string.Join(" | ", filas);

            StringAssert.Contains(";granada_lanzada;", todas);
            StringAssert.Contains(";granada_explota;", todas);
            StringAssert.Contains("muertos=4", todas);
            StringAssert.Contains(";bomba_puesta;", todas);
            StringAssert.Contains(";bomba_explota;", todas);
            StringAssert.Contains("n=2;de=4;zombies=5", todas);
            StringAssert.Contains(";pelea_ganada;", todas);
            Assert.AreEqual(1, sesion.GranadasLanzadas);
            Assert.AreEqual(1, sesion.BombasUsadas);
            StringAssert.Contains("granadas_lanzadas=1;bombas_usadas=1", sesion.Resumen(9f, s, "ganada"));
        }
    }
}
