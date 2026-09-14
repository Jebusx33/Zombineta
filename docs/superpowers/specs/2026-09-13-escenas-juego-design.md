# Escenas del juego definitivo — Diseño

Fecha: 13 de septiembre de 2026. Aprobado por José en la sesión del 13/09.
Sub-proyecto 2 de 5 del juego definitivo. Proyecto: `Juego/`.

## Alcance

**Esqueleto navegable:** el flujo completo funciona de punta a punta en escenas separadas, con
transiciones. Pantallas placeholder prolijas; niveles de prueba que se ganan con G y se pierden
con P. Fuera: gameplay real en los niveles (tramos 3 a 5), arte final, música, guardado.

## Escenas (`Juego/Assets/_Zombineta/Scenes/`)

| Escena | Rol | Carga |
|---|---|---|
| `Boot` | `GameRoot` (flujo + director de escenas), EventSystem, fundido | Siempre, nunca se descarga |
| `MainMenu`, `CharacterSelect`, `Cinematic`, `LevelComplete`, `GameOver`, `Ending` | Pantalla | Reemplaza a la base anterior, con fundido |
| `Level_01`, `Level_02` | Nivel (de prueba) | Reemplaza a la base anterior, con fundido |
| `Options`, `Pause` | Capa | Encima de la base, sin descargarla ni fundir |

Cada pantalla base trae su cámara y su `AudioListener`; las capas no. Orden de canvas: base 0,
`Pause` 100, `Options` 200, fundido 1000.

## Flujo (`GameFlow`, simulación compartida)

Cambios que solo agregan (el prototipo no los usa y sigue igual):
- `GameScreen.Paused`. `Pause()`: Playing → Paused. `Resume()`: Paused → Playing.
- `OpenOptions()` también desde Paused; `Back()` desde Options vuelve a donde se abrió.
- `Retry()` también desde Paused.
- `ToMainMenu()` también desde Paused.
- `Attempt`: sube cada vez que un nivel arranca de cero (fin de cinemática o reintento). Volver de
  la pausa no lo cambia. Así el director sabe si recargar la escena del nivel.
- `JumpTo(screen, levelIndex)`: pone el flujo en una pantalla sin evento. Solo para arrancar en
  Play desde cualquier escena.

`LevelSequence.LevelInfo` suma `sceneName` y `comicPanels` (lista de `ComicPanel`: `Sprite image`,
`float seconds`). El título de la cinemática es el `displayName` existente.

`GameSettings` suma `Volume` y `Fullscreen`.

## Director de escenas (`Juego/`)

- **`SceneRoutePlanner`** (C# plano, testeado): guarda la escena base y la pila de capas; para cada
  cambio de pantalla devuelve un `ScenePlan` (`Unload`, `Load`, `Active`, `Fade`).
  - Pantalla o nivel distinto → descarga capas y base, carga la nueva base, con fundido.
  - `Options` → apila la capa. `Paused` → capa `Pause` (y saca `Options` si estaba).
  - `Playing` con el mismo nivel → saca las capas; si `Attempt` cambió, además recarga el nivel.
- **`GameRoot`** (en `Boot`, singleton): crea el `GameFlow` con `Niveles.asset`, escucha `Changed`,
  ejecuta los planes en cola con el fundido (tiempo sin escalar), congela el tiempo mientras hay
  capa `Pause`, aplica volumen y pantalla completa, y expone `Flow`, `Levels`, `TopScene` y
  `LastChangeFrame`.
- **`Bootstrapper`**: al entrar a Play, si `Boot` no está cargada la carga al lado. Si se entró por
  una pantalla o nivel, `GameRoot` pone el flujo en ese estado (`JumpTo`) sin recargar nada.
- La recarga de dominio está desactivada en el proyecto: los estáticos se reinician en
  `SubsystemRegistration`.

## Pantallas

- Menús con `Button` de uGUI y `EventSystem` + `InputSystemUIInputModule`: teclado y joystick
  navegan sin código propio. Cada pantalla selecciona su primer botón al abrirse y lo recupera si
  la selección se pierde, solo si es la escena de arriba (`GameRoot.TopScene`).
- `MainMenu`: Jugar, Opciones, Salir. `CharacterSelect`: dos repartidoras, Volver.
  `Options`: volumen, pantalla completa, efectos de cámara, sacudidas, sangre, Volver.
  `LevelComplete`: Continuar. `GameOver`: Reintentar, Menú. `Ending`: Menú.
  `Pause`: Seguir, Reintentar, Opciones, Salir al menú (la acción Pausa también cierra).
- **`CinematicPlayer`**: título del nivel y viñetas en secuencia con fundido; Submit pasa a la
  siguiente, Cancel saltea todo. Viñetas placeholder generadas.
- **Nivel de prueba (`LevelStub`)**: muestra el nivel y las teclas; G gana, P pierde, la acción
  Pausa (Esc / Start) pausa. Ignora la entrada en el mismo frame de un cambio de pantalla, para
  que un mismo Esc no pause y despause a la vez.
- Input: acción nueva **`Pause`** en el mapa `Player` de `InputSystem_Actions` (Esc y Start);
  `UI/Submit` y `UI/Cancel` para la cinemática.

## Construcción

Las escenas se generan con una herramienta de editor (`Zombineta > Esqueleto > Construir
escenas`), idempotente: crea escenas, canvas, botones con sus listeners, `Niveles.asset`, las
viñetas placeholder y Build Settings. Así el esqueleto se puede regenerar y revisar como código.

## Pruebas

- EditMode: lo nuevo de `GameFlow` y `SceneRoutePlanner`.
- Recorrido en Play por MCP verificando escenas cargadas en cada paso: Boot → menú → personaje →
  cinemática → nivel 1 → pausa → opciones → volver → seguir → ganar → nivel completo → cinemática
  → nivel 2 → perder → reintentar → ganar → final → menú. Más: Play directo desde `Level_02`.
