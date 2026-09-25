using System.Collections.Generic;
using NUnit.Framework;
using Zombineta.Core;
using Zombineta.Tutorial;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// TutorialProgreso y TutorialRecursos son C# plano: nada de escena, prefab ni asset.
    /// TutorialSesion los alimenta cuadro a cuadro; aca se prueba la logica sola (la sesion
    /// entera, con la simulacion real, esta en TutorialSesionTests).
    /// </summary>
    public class TutorialLogicTests
    {
        // --- Helpers ----------------------------------------------------------

        static Paso NuevoPaso(CondicionPaso condicion, float cantidad = 0f) =>
            new Paso { condicion = condicion, cantidad = cantidad };

        static TutorialProgreso Progreso(params Paso[] pasos) =>
            new TutorialProgreso(new List<Paso>(pasos));

        static EntradaPaso Entrada(
            float dt = 0f,
            RunEvent eventos = RunEvent.None,
            DriveMode modo = DriveMode.Normal,
            bool faroPrendido = false,
            int deltaCarril = 0,
            int pickNafta = 0, int pickBateria = 0, int pickMunicion = 0,
            bool meta = false,
            bool acierto = false) =>
            new EntradaPaso
            {
                dt = dt,
                eventos = eventos,
                modo = modo,
                faroPrendido = faroPrendido,
                deltaCarril = deltaCarril,
                pickNafta = pickNafta,
                pickBateria = pickBateria,
                pickMunicion = pickMunicion,
                meta = meta,
                acierto = acierto,
            };

        // --- TurboSostenido / ReversaSostenida / FaroSostenido: acumulan dt ----

        [Test]
        public void TurboSostenido_AvanzaAlAcumularElTiempoPedido()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.TurboSostenido, 1.5f));

            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, modo: DriveMode.Turbo)));
            Assert.IsFalse(p.Terminado);
            Assert.IsTrue(p.Avanzar(Entrada(dt: 0.5f, modo: DriveMode.Turbo)));
            Assert.IsTrue(p.Terminado);
        }

        [Test]
        public void TurboSostenido_SeReiniciaAlSoltarYNoAcumulaEntreCortes()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.TurboSostenido, 1.5f));

            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, modo: DriveMode.Turbo)));
            // Suelta el turbo: el acumulado se reinicia a 0.
            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, modo: DriveMode.Normal)));
            // Con 1s no alcanza los 1.5s pedidos porque el corte reinicio el acumulado.
            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, modo: DriveMode.Turbo)));
            Assert.IsTrue(p.Avanzar(Entrada(dt: 0.5f, modo: DriveMode.Turbo)));
        }

        [Test]
        public void ReversaSostenida_AvanzaAlAcumularElTiempoPedido()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.ReversaSostenida, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(dt: 0.6f, modo: DriveMode.Reverse)));
            Assert.IsTrue(p.Avanzar(Entrada(dt: 0.4f, modo: DriveMode.Reverse)));
        }

        [Test]
        public void ReversaSostenida_NoAvanzaConTurbo()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.ReversaSostenida, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(dt: 2f, modo: DriveMode.Turbo)));
            Assert.IsFalse(p.Terminado);
        }

        [Test]
        public void FaroSostenido_AvanzaAlAcumularYSeReiniciaAlApagar()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.FaroSostenido, 2f));

            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, faroPrendido: true)));
            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, faroPrendido: false)));
            Assert.IsFalse(p.Avanzar(Entrada(dt: 1f, faroPrendido: true)));
            Assert.IsTrue(p.Avanzar(Entrada(dt: 1f, faroPrendido: true)));
        }

        // --- CambioCarrilAmbos: uno para cada lado, en cualquier orden ---------

        [Test]
        public void CambioCarrilAmbos_AvanzaConUnCambioParaCadaLado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.CambioCarrilAmbos));

            Assert.IsFalse(p.Avanzar(Entrada(deltaCarril: 1)));
            Assert.IsTrue(p.Avanzar(Entrada(deltaCarril: -1)));
        }

        [Test]
        public void CambioCarrilAmbos_OrdenInversoTambienCuenta()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.CambioCarrilAmbos));

            Assert.IsFalse(p.Avanzar(Entrada(deltaCarril: -1)));
            Assert.IsTrue(p.Avanzar(Entrada(deltaCarril: 1)));
        }

        [Test]
        public void CambioCarrilAmbos_UnSoloLadoNoAlcanza()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.CambioCarrilAmbos));

            Assert.IsFalse(p.Avanzar(Entrada(deltaCarril: 1)));
            Assert.IsFalse(p.Avanzar(Entrada(deltaCarril: 1)));
            Assert.IsFalse(p.Terminado);
        }

        // --- Salto: Launched y despues Landed (con o sin LandedPerfect) --------

        [Test]
        public void Salto_LanzadoYAterrizadoCuenta()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.Salto, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Launched)));
            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.Landed)));
        }

        [Test]
        public void Salto_LanzadoYAterrizajePerfectoCuenta()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.Salto, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Launched)));
            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.Landed | RunEvent.LandedPerfect)));
        }

        [Test]
        public void Salto_LanzamientoYAterrizajeEnElMismoCuadroCuentan()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.Salto, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.Launched | RunEvent.Landed)));
        }

        [Test]
        public void Salto_AterrizajeSinLanzamientoPrevioNoCuenta()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.Salto, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Landed)));
            Assert.IsFalse(p.Terminado);
        }

        [Test]
        public void Salto_PideVariosSaltosSiCantidadEsMayor()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.Salto, 2f));

            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Launched)));
            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Landed)));
            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Launched)));
            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.Landed)));
        }

        // --- Pickups: cuentan por item agarrado (Consumed false -> true), no porque el recurso suba

        [Test]
        public void PickupNafta_CuentaConUnItemAgarrado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.PickupNafta, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickNafta: 1)));
        }

        [Test]
        public void PickupNafta_NoCuentaSinItemAgarrado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.PickupNafta, 1f));

            // Un PickedUp de otra cosa (sin item de nafta consumido) no alcanza.
            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.PickedUp)));
            Assert.IsFalse(p.Terminado);
        }

        [Test]
        public void PickupBateria_CuentaConUnItemAgarrado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.PickupBateria, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickBateria: 1)));
        }

        [Test]
        public void PickupConElRecursoAlMaximo_CuentaIgual()
        {
            // La bateria arranca llena y recien el faro (paso 8) la gasta: agarrar una en el paso
            // 6 no sube el valor. Con la regla vieja ("el recurso subio") el paso nunca se cumplia.
            // La entrada ya no trae el valor del recurso: alcanza con el item consumido.
            var p = Progreso(
                NuevoPaso(CondicionPaso.PickupBateria, 1f),
                NuevoPaso(CondicionPaso.PickupMunicion, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickBateria: 1)));
            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickMunicion: 1)));
            Assert.IsTrue(p.Terminado);
        }

        [Test]
        public void PickupMunicion_CuentaConUnItemAgarrado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.PickupMunicion, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickMunicion: 1)));
        }

        [Test]
        public void PickupPideVariosSiCantidadEsMayor_YCuentaVariosEnElMismoCuadro()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.PickupNafta, 2f));

            Assert.IsFalse(p.Avanzar(Entrada(pickNafta: 1)));
            Assert.IsTrue(p.Avanzar(Entrada(pickNafta: 1)));

            var q = Progreso(NuevoPaso(CondicionPaso.PickupNafta, 2f));
            Assert.IsTrue(q.Avanzar(Entrada(pickNafta: 2)));
        }

        [Test]
        public void PasoNoAvanzaConEntradaDeOtroPaso_PickupDeNaftaDuranteElPasoDeBateria()
        {
            var p = Progreso(
                NuevoPaso(CondicionPaso.PickupNafta, 1f),
                NuevoPaso(CondicionPaso.PickupBateria, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickNafta: 1)));
            Assert.AreEqual(1, p.Indice);

            // Ahora esta en el paso de bateria: un pickup de nafta no lo cumple.
            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.PickedUp, pickNafta: 1)));
            Assert.AreEqual(1, p.Indice);
        }

        // --- DisparoAcertado: no avanza con un tiro errado ----------------------

        [Test]
        public void DisparoAcertado_AvanzaConUnAcierto()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.DisparoAcertado, 1f));

            Assert.IsTrue(p.Avanzar(Entrada(eventos: RunEvent.Shot, acierto: true)));
        }

        [Test]
        public void DisparoAcertado_NoAvanzaConUnFallo()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.DisparoAcertado, 1f));

            Assert.IsFalse(p.Avanzar(Entrada(eventos: RunEvent.Shot | RunEvent.ShotMissed, acierto: false)));
            Assert.IsFalse(p.Terminado);
        }

        // --- LlegarMeta ----------------------------------------------------------

        [Test]
        public void LlegarMeta_AvanzaCuandoLlegaLaMeta()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.LlegarMeta));

            Assert.IsFalse(p.Avanzar(Entrada(meta: false)));
            Assert.IsTrue(p.Avanzar(Entrada(meta: true)));
        }

        // --- Terminado y avance de a un paso por llamada ------------------------

        [Test]
        public void Terminado_EsTrueTrasElUltimoPaso()
        {
            var p = Progreso(
                NuevoPaso(CondicionPaso.LlegarMeta),
                NuevoPaso(CondicionPaso.LlegarMeta));

            Assert.IsFalse(p.Terminado);
            Assert.IsNotNull(p.Actual);

            p.Avanzar(Entrada(meta: true));
            Assert.IsFalse(p.Terminado);
            Assert.AreEqual(1, p.Indice);

            p.Avanzar(Entrada(meta: true));
            Assert.IsTrue(p.Terminado);
            Assert.IsNull(p.Actual);
        }

        [Test]
        public void Avanzar_DevuelveFalseUnaVezTerminado()
        {
            var p = Progreso(NuevoPaso(CondicionPaso.LlegarMeta));

            Assert.IsTrue(p.Avanzar(Entrada(meta: true)));
            Assert.IsTrue(p.Terminado);
            Assert.IsFalse(p.Avanzar(Entrada(meta: true)));
        }

        [Test]
        public void Avanzar_SoloAvanzaUnPasoPorLlamadaAunqueLaMismaEntradaCumplaElSiguiente()
        {
            var p = Progreso(
                NuevoPaso(CondicionPaso.LlegarMeta),
                NuevoPaso(CondicionPaso.LlegarMeta));

            Assert.IsTrue(p.Avanzar(Entrada(meta: true)));
            // Un solo Avanzar no puede saltar dos pasos aunque la entrada siga cumpliendo el
            // siguiente paso.
            Assert.AreEqual(1, p.Indice);
            Assert.IsFalse(p.Terminado);
        }

        // --- TutorialRecursos ----------------------------------------------------

        [Test]
        public void Recursos_AvisaUnaVezAlCruzarElUmbral()
        {
            var r = new TutorialRecursos();

            var d1 = r.Evaluar(0.5f, 1f, 10, false);
            Assert.IsFalse(d1.avisoNafta);

            var d2 = r.Evaluar(0.15f, 1f, 10, false);
            Assert.IsTrue(d2.avisoNafta);
        }

        [Test]
        public void Recursos_NoAvisaDeNuevoMientrasSigaAbajoDelUmbral()
        {
            var r = new TutorialRecursos();

            r.Evaluar(0.5f, 1f, 10, false);
            var primerAviso = r.Evaluar(0.15f, 1f, 10, false);
            Assert.IsTrue(primerAviso.avisoNafta);

            var segundoAviso = r.Evaluar(0.1f, 1f, 10, false);
            Assert.IsFalse(segundoAviso.avisoNafta);
        }

        [Test]
        public void Recursos_SeRearmaAlSubirDeNuevoSobreElUmbral()
        {
            var r = new TutorialRecursos();

            r.Evaluar(0.5f, 1f, 10, false);
            r.Evaluar(0.15f, 1f, 10, false); // primer aviso, se desarma
            r.Evaluar(0.1f, 1f, 10, false); // sigue abajo, no vuelve a avisar

            var rearmado = r.Evaluar(0.5f, 1f, 10, false); // vuelve a subir: se rearma
            Assert.IsFalse(rearmado.avisoNafta);

            var segundoCruce = r.Evaluar(0.15f, 1f, 10, false);
            Assert.IsTrue(segundoCruce.avisoNafta);
        }

        [Test]
        public void Recursos_AvisaYSeRearmaTambienParaBateria()
        {
            var r = new TutorialRecursos();

            r.Evaluar(1f, 0.5f, 10, false);
            var aviso = r.Evaluar(1f, 0.1f, 10, false);
            Assert.IsTrue(aviso.avisoBateria);
            Assert.IsFalse(aviso.avisoNafta);

            var sinAviso = r.Evaluar(1f, 0.1f, 10, false);
            Assert.IsFalse(sinAviso.avisoBateria);

            r.Evaluar(1f, 0.5f, 10, false);
            var segundoCruce = r.Evaluar(1f, 0.1f, 10, false);
            Assert.IsTrue(segundoCruce.avisoBateria);
        }

        [Test]
        public void Recursos_RecargaEnCero()
        {
            var r = new TutorialRecursos();

            var d = r.Evaluar(0f, 0f, 10, false);
            Assert.IsTrue(d.recargarNafta);
            Assert.IsTrue(d.recargarBateria);
        }

        [Test]
        public void Recursos_NoRecargaPorEncimaDeCero()
        {
            var r = new TutorialRecursos();

            var d = r.Evaluar(0.01f, 0.01f, 10, false);
            Assert.IsFalse(d.recargarNafta);
            Assert.IsFalse(d.recargarBateria);
        }

        [Test]
        public void Recursos_RecargarMunicionSoloConFlagYMunicionEnCero()
        {
            var r = new TutorialRecursos();

            Assert.IsFalse(r.Evaluar(1f, 1f, 0, false).recargarMunicion);
            Assert.IsFalse(r.Evaluar(1f, 1f, 5, true).recargarMunicion);
            Assert.IsTrue(r.Evaluar(1f, 1f, 0, true).recargarMunicion);
        }

        [Test]
        public void Recursos_MunicionNuncaAvisa()
        {
            var r = new TutorialRecursos();

            var d = r.Evaluar(1f, 1f, 0, true);
            // Decision no tiene campo de aviso de municion: solo existen avisoNafta/avisoBateria.
            Assert.IsFalse(d.avisoNafta);
            Assert.IsFalse(d.avisoBateria);
        }

        [Test]
        public void Recursos_AvisoYRecargaSonIndependientes()
        {
            var r = new TutorialRecursos();

            // Caida directa a 0: dispara aviso (cruza el umbral) y recarga (llega a 0) a la vez.
            var d = r.Evaluar(0f, 1f, 10, false);
            Assert.IsTrue(d.avisoNafta);
            Assert.IsTrue(d.recargarNafta);
        }

        [Test]
        public void Recursos_ExponeUmbralYRecargaDelConstructor()
        {
            var r = new TutorialRecursos(0.25f, 0.7f);
            Assert.AreEqual(0.25f, r.UmbralAviso, 1e-6f);
            Assert.AreEqual(0.7f, r.Recarga, 1e-6f);
        }
    }
}
