using UnityEngine;
using UnityEngine.InputSystem;
using Zombineta.Core;

namespace Zombineta.UI
{
    /// <summary>
    /// HUD de trabajo para la Fase 1, con OnGUI: cero prefabs, cero wiring, cero
    /// dependencias. Existe para poder balancear mirando numeros crudos.
    /// En la Fase 2 lo reemplaza HudView con barras de verdad.
    /// </summary>
    public sealed class DebugHudView : MonoBehaviour
    {
        [SerializeField] RunController run;

        [Tooltip("Arranca oculto: son numeros para balancear, no para jugar. F1 lo muestra.")]
        [SerializeField] bool visible;

        GUIStyle style;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible || run == null || run.Sim == null)
                return;

            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white },
            };

            var s = run.Sim.State;

            GUILayout.BeginArea(new Rect(16f, 16f, 460f, 320f));
            GUI.Box(new Rect(0f, 0f, 440f, 240f), GUIContent.none);
            GUILayout.Space(8f);

            GUILayout.Label($"NAFTA     {s.Fuel,6:0.0}   ({run.Config.fuelMax:0})", style);
            GUILayout.Label(
                $"BATERIA   {s.Battery,6:0.0}   faro: {(s.HeadlightOn ? "ON" : "off")}", style);
            GUILayout.Label($"BALAS     {s.Ammo}", style);
            GUILayout.Label($"HORDA a   {s.Gap,6:0.0} m", style);
            GUILayout.Label(
                $"AVANCE    {s.PlayerX,7:0} / {run.Config.goalDistance:0} m " +
                $"({run.Sim.Progress01 * 100f:0}%)", style);
            GUILayout.Label(
                $"MODO      {s.Mode}   carril {s.Lane}   " +
                $"{run.Sim.PlayerSpeed:0.0} m/s  vs horda {run.Sim.HordeSpeed:0.0} m/s", style);

            if (s.Phase == RunPhase.Lost)
            {
                string why = s.Loss == LossReason.OutOfFuel
                    ? "te quedaste sin nafta"
                    : "te alcanzo la horda";
                GUILayout.Label($"PERDISTE: {why}.  R para reiniciar.", style);
            }
            else if (s.Phase == RunPhase.Won)
            {
                GUILayout.Label($"LLEGASTE en {s.Elapsed:0.0} s.  R para reiniciar.", style);
            }

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(16f, Screen.height - 44f, 900f, 40f));
            GUILayout.Label(
                "W/S o flechas: carril   |   D o -> : turbo   |   A o <- : retroceso   " +
                "|   Espacio: faro   |   X o click: disparar", style);
            GUILayout.EndArea();
        }
    }
}
