using System.Collections.Generic;
using UnityEngine;
using Zombineta.Core;

namespace Zombineta.Tutorial
{
    /// <summary>
    /// Lo que paso en un cuadro, tal como lo necesita TutorialProgreso para decidir si el paso
    /// actual se cumplio. Lo arma TutorialSesion a partir del RunState, los eventos del tick y los
    /// items del recorrido.
    /// </summary>
    public struct EntradaPaso
    {
        public float dt;
        public RunEvent eventos;
        public DriveMode modo;
        public bool faroPrendido;

        /// <summary>+1 / -1 cuando el carril cambio este cuadro; 0 si no.</summary>
        public int deltaCarril;

        /// <summary>
        /// Cuantos items de nafta, bateria y municion se consumieron este cuadro (pasaron de
        /// Consumed false a true). Se cuenta el item agarrado y no que el recurso haya subido:
        /// con el recurso al maximo (la bateria arranca llena) el valor no sube, y el pickup
        /// tiene que contar igual.
        /// </summary>
        public int pickNafta, pickBateria, pickMunicion;

        public bool meta;

        /// <summary>Un disparo pego: Shot sin ShotMissed.</summary>
        public bool acierto;
    }

    /// <summary>
    /// C# plano: recorre la lista de pasos del tutorial cuadro a cuadro. Sin MonoBehaviour ni
    /// dependencia de escena; TutorialSesion es quien la alimenta y actua sobre el resultado.
    /// </summary>
    public sealed class TutorialProgreso
    {
        readonly IReadOnlyList<Paso> pasos;

        int indice;

        // --- Acumuladores del paso actual: se reinician cuando el paso cambia ---
        float sostenidoAcumulado;
        bool vioCambioArriba;
        bool vioCambioAbajo;
        bool lanzada;
        int contador;

        public TutorialProgreso(IReadOnlyList<Paso> pasos)
        {
            this.pasos = pasos;
            indice = 0;
        }

        public int Indice => indice;
        public Paso Actual => Terminado ? null : pasos[indice];
        public bool Terminado => pasos == null || indice >= pasos.Count;

        /// <summary>Consume un cuadro. Devuelve true si el paso actual se cumplio y se avanzo.</summary>
        public bool Avanzar(in EntradaPaso e)
        {
            if (Terminado)
                return false;

            if (!EvaluarPasoActual(e))
                return false;

            indice++;
            ReiniciarAcumuladores();
            return true;
        }

        bool EvaluarPasoActual(in EntradaPaso e)
        {
            var paso = pasos[indice];

            switch (paso.condicion)
            {
                case CondicionPaso.TurboSostenido:
                    return AcumularSostenido(e.modo == DriveMode.Turbo, e.dt, paso.cantidad);

                case CondicionPaso.ReversaSostenida:
                    return AcumularSostenido(e.modo == DriveMode.Reverse, e.dt, paso.cantidad);

                case CondicionPaso.FaroSostenido:
                    return AcumularSostenido(e.faroPrendido, e.dt, paso.cantidad);

                case CondicionPaso.CambioCarrilAmbos:
                    if (e.deltaCarril > 0) vioCambioArriba = true;
                    if (e.deltaCarril < 0) vioCambioAbajo = true;
                    return vioCambioArriba && vioCambioAbajo;

                case CondicionPaso.Salto:
                    if ((e.eventos & RunEvent.Launched) != 0)
                        lanzada = true;
                    if (lanzada && (e.eventos & RunEvent.Landed) != 0)
                    {
                        lanzada = false;
                        contador++;
                    }
                    return contador >= Veces(paso.cantidad);

                case CondicionPaso.PickupNafta:
                    contador += Mathf.Max(0, e.pickNafta);
                    return contador >= Veces(paso.cantidad);

                case CondicionPaso.PickupBateria:
                    contador += Mathf.Max(0, e.pickBateria);
                    return contador >= Veces(paso.cantidad);

                case CondicionPaso.PickupMunicion:
                    contador += Mathf.Max(0, e.pickMunicion);
                    return contador >= Veces(paso.cantidad);

                case CondicionPaso.DisparoAcertado:
                    if (e.acierto)
                        contador++;
                    return contador >= Veces(paso.cantidad);

                case CondicionPaso.LlegarMeta:
                    return e.meta;

                default:
                    return false;
            }
        }

        bool AcumularSostenido(bool activo, float dt, float cantidad)
        {
            sostenidoAcumulado = activo ? sostenidoAcumulado + dt : 0f;
            return sostenidoAcumulado >= cantidad;
        }

        /// <summary>Condiciones contables: al menos 1 vez, redondeado.</summary>
        static int Veces(float cantidad) => Mathf.Max(1, Mathf.RoundToInt(cantidad));

        void ReiniciarAcumuladores()
        {
            sostenidoAcumulado = 0f;
            vioCambioArriba = false;
            vioCambioAbajo = false;
            lanzada = false;
            contador = 0;
        }
    }
}
