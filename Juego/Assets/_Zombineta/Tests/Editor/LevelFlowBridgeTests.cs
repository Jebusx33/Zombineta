using NUnit.Framework;
using Zombineta.Core;
using Zombineta.Juego.Levels;

namespace Zombineta.Juego.Tests
{
    /// <summary>
    /// La decision de LevelFlowBridge de arrancar el final (plano de victoria o de atrapada),
    /// como funcion pura: en los niveles normales no cambia nada; en el tutorial un Lost nunca
    /// arranca la atrapada y un Won solo cuenta en el ultimo paso.
    /// </summary>
    public class LevelFlowBridgeTests
    {
        [Test]
        public void EnUnNivelNormal_WonEsVictoriaYLostEsAtrapada()
        {
            Assert.AreEqual(FinalDePartida.Victoria, LevelFlowBridge.DecidirFinal(RunPhase.Won, false, false));
            Assert.AreEqual(FinalDePartida.Victoria, LevelFlowBridge.DecidirFinal(RunPhase.Won, false, true));
            Assert.AreEqual(FinalDePartida.Atrapada, LevelFlowBridge.DecidirFinal(RunPhase.Lost, false, false));
            Assert.AreEqual(FinalDePartida.Atrapada, LevelFlowBridge.DecidirFinal(RunPhase.Lost, false, true));
            Assert.AreEqual(FinalDePartida.Ninguno, LevelFlowBridge.DecidirFinal(RunPhase.Running, false, true));
        }

        [Test]
        public void EnElTutorial_UnLostTransitorioNoArrancaLaAtrapada()
        {
            // Si arrancara, el puente quedaria esperando un final que nunca llega (el director ya
            // devolvio la partida a Running) y el tutorial no se podria terminar.
            Assert.AreEqual(FinalDePartida.Ninguno, LevelFlowBridge.DecidirFinal(RunPhase.Lost, true, true));
            Assert.AreEqual(FinalDePartida.Ninguno, LevelFlowBridge.DecidirFinal(RunPhase.Lost, true, false));
        }

        [Test]
        public void EnElTutorial_WonSoloEsVictoriaEnElUltimoPaso()
        {
            Assert.AreEqual(FinalDePartida.Victoria, LevelFlowBridge.DecidirFinal(RunPhase.Won, true, true));
            // F2 o un salto de tiempo antes del ultimo paso: el director lo deshace y rebobina.
            Assert.AreEqual(FinalDePartida.Ninguno, LevelFlowBridge.DecidirFinal(RunPhase.Won, true, false));
            Assert.AreEqual(FinalDePartida.Ninguno, LevelFlowBridge.DecidirFinal(RunPhase.Running, true, true));
        }
    }
}
