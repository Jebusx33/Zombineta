# Jugar con joystick — Diseño

Juego definitivo (`Juego/`). El menú ya se navega con joystick (acciones del mapa `UI` del Input
System); la partida no, porque `PlayerInputReader` lee teclado y mouse directamente.

## Objetivo

Que un nivel se juegue de punta a punta con un joystick (Xbox, PlayStation o genérico reconocido
por el Input System), con vibración, atajos de depuración y ayuda de controles, sin romper
teclado y mouse.

Fuera: reasignación de controles por la jugadora (queda posible más adelante), íconos de
botones (se usa texto), soporte de más de un joystick / multijugador, cambios en `Prototipo/`.

## Decisiones tomadas

- **Esquema:** gatillos para la velocidad (tabla abajo).
- **Enfoque:** mapa de acciones del Input System, no lectura directa de dispositivos.
- **Extras:** vibración con opción, F2/F3 desde el joystick, verificar pantallas con A/B, ayuda de
  controles en la pausa según el último dispositivo usado.
- **Alcance:** solo `Juego/`. Las reglas de juego no cambian: el resultado sigue siendo un
  `PlayerIntent`.

## 1. Controles

Nuevo mapa `Moto` en `Juego/Assets/Settings/InputSystem_Actions.inputactions` (el asset que ya
usan el menú y la acción `Pause`):

| Acción | Tipo | Teclado / mouse | Joystick |
|---|---|---|---|
| `LaneUp` | Button | W, ↑ | stick izq. arriba, D-pad arriba |
| `LaneDown` | Button | S, ↓ | stick izq. abajo, D-pad abajo |
| `Turbo` | Button (mantener) | D, → | gatillo derecho |
| `Reverse` | Button (mantener) | A, ← | gatillo izquierdo |
| `Fire` | Button | X, click izquierdo | botón oeste (X / □), hombro derecho (RB / R1) |
| `Headlight` | Button | Espacio | botón norte (Y / △) |
| `DebugModifier` | Button (mantener) | — | View / Select |
| `DebugWin` | Button | F2 | hombro derecho (con `DebugModifier`) |
| `DebugLose` | Button | F3 | hombro izquierdo (con `DebugModifier`) |

La acción `Pause` existente (Esc, Start) no cambia.

- **Umbrales:** el stick para carril usa punto de presión 0,5 (`InputSystem` "Press" con
  `pressPoint 0.5`); para volver a cambiar hay que soltar por debajo del umbral de liberación
  (default del Input System ≈ 0,375), así un stick sostenido cambia un solo carril. Los gatillos
  cuentan como apretados desde 0,3.
- **`PlayerInputReader`** deja de leer `Keyboard`/`Mouse` y lee las acciones del mapa `Moto`
  (referenciadas con `InputActionReference` serializadas, habilitadas en `OnEnable`,
  deshabilitadas en `OnDisable`). La construcción del intent no cambia:
  - `LaneDelta`: +1 si `LaneUp.WasPressedThisFrame()`, −1 si `LaneDown` (ambos → 0).
  - `Mode`: Turbo si `Turbo` apretado y `Reverse` no; Reverse al revés; si no, Normal (los dos
    juntos → Normal, igual que A+D hoy).
  - `ToggleHeadlight` y `Fire`: `WasPressedThisFrame()`.
  - `RestartPressed` (reinicio rápido del prototipo, desactivado en `Juego/` por
    `LevelFlowBridge`) se conserva leyendo R por acción `QuickRestart` solo teclado, para no romper
    la interfaz.
- **F2/F3:** `LevelFlowBridge` lee `DebugWin`/`DebugLose` en lugar de `Keyboard.current`, dentro de
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. En joystick hace falta mantener `DebugModifier`
  (composite "one modifier" del Input System, para que RB solo no gane la partida mientras se
  dispara). `DebugHudView` y `LevelStub` (herramientas de depuración / escena de prueba) quedan con
  teclado.

## 2. Vibración

**`RumbleDirector`** (C# plano, `Juego/Assets/_Zombineta/Scripts/Gameplay/Fx/`, con tests):

- `void Trigger(RumbleKind kind)`; `void Tick(float unscaledDt)`; `float Low`, `float High`
  (0..1); `void Stop()`.
- Cada `RumbleKind` (`Crash`, `Fall`, `Explosion`, `RanOver`, `Shot`, `Caught`) tiene
  `low`, `high`, `seconds` en `RumbleConfig` (ScriptableObject, `Settings/Rumble.asset`).
- Varios eventos a la vez: por motor gana el valor más fuerte vigente; cada evento decae linealmente
  a 0 en sus `seconds`. Un nuevo evento del mismo tipo reinicia el suyo.
- Valores iniciales: Shot 0,05/0,15/0,08 s; Crash 0,6/0,4/0,25 s; Fall 0,7/0,5/0,35 s; RanOver
  0,4/0,6/0,2 s; Explosion 0,9/0,6/0,45 s; Caught 1/0,8/0,9 s.

**`GamepadRumble`** (MonoBehaviour en el nivel, junto a `LevelFlowBridge`):

- Se suscribe a `RunController.Stepped` y traduce `RunEvent` a `RumbleKind` (`Crashed` → Crash,
  `Fell` → Fall, `Explosion`, `RanOver`, `Shot` → Shot); el final atrapada (`RunPhase.Lost` por
  horda) → Caught una vez.
- Cada frame: `Tick(Time.unscaledDeltaTime)` y `Gamepad.current?.SetMotorSpeeds(Low, High)` solo
  si cambió.
- Corta (`Stop` + `SetMotorSpeeds(0,0)`): con `GameSettings.Vibration` apagado, cuando el flujo no
  está en `Playing` (pausa, opciones, final de nivel), en `OnDisable`/`OnDestroy` (descarga del
  nivel) y en `OnApplicationFocus(false)`.

**Opción.** `GameSettings.Vibration` (bool, default true, clave `zombineta.vibration`, mismo patrón
que `CameraShake`). Nuevo interruptor "Vibración" en la escena `Options`, cableado en
`OptionsScreen` como los otros interruptores y dentro de la navegación con teclado/joystick.

## 3. Pantallas

- Game Over, Nivel completo, Final, Cinemática, Selección de personaje, Opciones y Pausa usan
  botones de uGUI con el `EventSystem` (Submit/Cancel del mapa `UI`). Se **verifican** con un
  joystick simulado; si alguna lee una tecla suelta, se pasa a acciones.

## 4. Ayuda de controles

- **`InputDeviceTracker`** (estático, reiniciado en `SubsystemRegistration`): escucha
  `InputSystem.onActionChange` (`ActionPerformed`) y guarda si el último control usado es de un
  `Gamepad` o de teclado/mouse. Expone `ControlScheme Current` y `event Action<ControlScheme> Changed`.
  La decisión de esquema a partir del dispositivo es una función pura testeada
  (`ControlScheme SchemeOf(InputDevice)`).
- **`ControlHints`** (componente de uGUI en la escena `Pause`): un `Text` con la lista de controles
  del esquema actual; se actualiza al abrir y en `Changed`. Textos:
  - Teclado: `W/S o ↑/↓ carril · D turbo · A retroceso · X o click disparar · Espacio faro · Esc pausa`
  - Joystick: `Stick o cruceta carril · RT turbo · LT retroceso · X o RB disparar · Y faro · Start pausa`
  - En el aire (ambos): turbo/retroceso inclinan la moto.

## 5. Pruebas

**EditMode** (`Juego/Assets/_Zombineta/Tests/Editor/`, con las herramientas de test del Input
System: `InputTestFixture`; requiere `"testables": ["com.unity.inputsystem"]` en
`Juego/Packages/manifest.json` y la referencia `Unity.InputSystem.TestFramework` en el asmdef de
tests):

- `PlayerInputReaderTests`: con joystick virtual — stick arriba pasado 0,5 → `LaneDelta +1` una sola
  vez aunque se mantenga; D-pad abajo → −1; RT → Turbo; LT → Reverse; RT+LT → Normal; X y RB →
  `Fire`; Y → `ToggleHeadlight`. Con teclado virtual: los mismos casos que hoy (W/S, D/A, espacio,
  X, click) siguen funcionando.
- `RumbleDirectorTests`: un evento decae a 0 en su duración; dos eventos → gana el más fuerte por
  motor; reiniciar el mismo tipo; `Stop` pone todo en 0.
- `InputDeviceTrackerTests`: `SchemeOf(gamepad)` → Gamepad; `SchemeOf(keyboard|mouse)` →
  KeyboardMouse.
- Los tests existentes siguen en verde.

**Play por MCP:**
- Partida en `Level_01` con joystick virtual (`InputSystem.AddDevice<Gamepad>()` + estados
  encolados): cambio de carril, turbo, disparo, faro, pausa con Start, seguir, Game Over y
  reintentar con Submit.
- Vibración: tras un choque `Low/High > 0` y vuelve a 0; en pausa se corta.
- Ayuda de controles: al tocar el joystick virtual la pausa muestra el texto de joystick; al tocar
  una tecla, el de teclado.
- Consola sin errores.

**A mano (José):** una partida con joystick real, incluida la vibración.

## Riesgos

- **Dispositivos genéricos:** joysticks no reconocidos como `Gamepad` (algunos genéricos
  DirectInput) no tendrán los bindings de gatillos. Se documenta en el HANDOFF; el soporte se
  prueba con Xbox/PlayStation.
- **Vibración en Windows con controles de PlayStation:** depende del driver; si no vibra no es un
  error del juego.
