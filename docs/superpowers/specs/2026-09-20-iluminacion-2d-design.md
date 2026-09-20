# Iluminación 2D con URP — Diseño

Sub-proyecto 5 del juego definitivo (`Juego/`).

## Objetivo

Que el juego se ilumine de verdad con luces 2D de URP y que **arte pueda trabajar la luz de cada
asset por su cuenta**: abrir un edificio, ponerle una vidriera encendida, guardarlo y verlo en
todos los niveles, sin tocar código ni escenas.

Hoy no se puede: `SceneryManager` crea cada tile como un `SpriteRenderer` pelado en tiempo de
ejecución a partir de una lista de sprites sueltos (`Escenario.asset`), así que no existe ningún
objeto que arte pueda abrir. La única `Light2D` del proyecto es el faro de la moto, y hay una sola
Sorting Layer (`Default`), con lo cual una luz no puede alcanzar a los edificios sin lavar la calle.

Fuera: normal maps de personajes y objetos (solo escenario por ahora), ciclo día/noche, arte final,
y la secuencia de tramos por ambiente (sub-proyecto 6; acá solo se deja el arte clasificado).

## Decisiones tomadas

- **Prefabs**: cada variante de tile y cada tipo del recorrido pasa a ser un prefab con su sprite,
  sus luces y su sombra. El escenario se sigue armando solo, pero instancia y recicla prefabs.
- **Normal maps**: solo escenario (edificios, calle, primer plano). Personajes y objetos se
  iluminan planos; se pueden sumar después sin tocar código.
- **Ambiente**: un asset `PerfilDeLuz` por nivel, con color e intensidad de la luz global de cada
  capa. Arte lo edita y lo ve en vivo en el editor.
- **Gameplay iluminado**: el faro ilumina de verdad; destellos de disparo, explosión y choque;
  resplandor que sigue a la horda.
- **Sombras**: solo lo cercano a la calle (faroles, carteles, vallas, obstáculos, barriles), más la
  moto y los zombies. El fondo lejano no corta luz.
- **Rendimiento**: tope de luces de adorno por pantalla + interruptor `Iluminación: Alta / Baja` en
  Opciones.
- **Taller de luz**: una escena generada con una copia de cada prefab, agrupada por ambiente, con
  el ambiente real de un perfil.
- **Tramos por ambiente**: se prepara el dato (campo de grupo por variante) pero no la lógica.

## 1. Capas de dibujo

Se crean cinco Sorting Layers, de atrás hacia adelante: `Cielo`, `Fondo`, `Calle`, `Juego`,
`Frente`.

- `SceneryLayer` suma `sortingLayer` (nombre) además del `sortingOrder` actual. Asignación:
  Cielo → `Cielo`; Edificios y Árboles → `Fondo`; Cordón, Calle y Vereda → `Calle`; Frontal y
  Primer plano → `Frente`.
- Todo lo del juego (moto, horda, items, sombras de piso, efectos) va a `Juego`, conservando el
  orden por carril de `LaneSorting` dentro de esa capa.
- Cada `Light2D` declara a qué capas alcanza. Es lo que permite iluminar los edificios sin lavar
  la calle.

## 2. Prefabs de escenario y de recorrido

**`SceneryVariant`**: `Sprite sprite` → `GameObject prefab`, más `string grupo` (ciudad,
autopista, puente, transición; libre, por ahora solo informativo y usado por el taller). El ancho
del tile se sigue calculando de la proporción del sprite, leído del `SpriteRenderer` del prefab.

**Contrato del prefab de tile** (lo que arte puede tocar sin romper nada):
- Raíz con `SpriteRenderer` (sprite y material lit). El pivot manda: la base del tile es `y = 0`.
- Hijos opcionales: `Light2D` (las que quiera), `ShadowCaster2D`, `Flicker` (componente nuevo:
  frecuencia, rango de intensidad, semilla, para neones y faroles).
- La capa y el orden los pone el sistema al instanciar; lo que ponga arte se pisa.

**`LevelItemPalette.Look`**: suma `GameObject prefab` opcional. Si está, `LevelScene` instancia el
prefab como hijo del item (en editor y en juego) en vez de pintar el sprite; si no, sigue como hoy.

**Migración**: una herramienta de editor (`Zombineta > Luz > Migrar tiles a prefabs`) crea un
prefab por cada sprite actual en `Art/Tileset/Prefabs/`, con el material lit, y reescribe
`Escenario.asset`. Los placeholders siguen funcionando igual.

**Pool**: `SceneryManager` recicla por variante (una lista de instancias por prefab), apagando en
vez de destruir. Hoy recicla `SpriteRenderer` sueltos.

## 3. Perfil de luz por nivel

`PerfilDeLuz` (ScriptableObject, `Settings/Luz/`): por cada Sorting Layer, color e intensidad de su
luz global; más el color e intensidad de una luz global general.

- `LightingDirector` (MonoBehaviour, `[ExecuteAlways]`, en el objeto `Nivel`): crea y mantiene las
  `Light2D` globales según el perfil, y las actualiza al vuelo cuando el asset cambia (igual que
  `SceneryManager` con `Escenario.asset`).
- `LevelScene` suma `PerfilDeLuz perfil`. `Level_01` y `Level_02` arrancan con el mismo perfil
  (`Noche.asset`); se separan cuando arte quiera.

## 4. Luz de gameplay

- **Faro**: la `Light2D` que ya existe pasa a alcanzar `Calle` y `Juego`, con sombras. Intensidad y
  alcance siguen saliendo de la batería y de la config: no cambia ninguna regla.
- **Destellos de eventos**: `FlashPool` (pool de `Light2D` apagadas) + `FlashConfig`
  (ScriptableObject con color, intensidad, radio y duración por evento: `Shot`, `Explosion`,
  `Crashed`, `RanOver`). Los dispara el mismo flujo de `RunEvent` que ya usan los efectos y la
  vibración; acompañan a las partículas, no las reemplazan.
- **Horda**: una `Light2D` tenue que sigue al frente de la horda; su intensidad crece a medida que
  se acerca, con los mismos metros que usa la barra de amenaza del HUD.

## 5. Sombras

`ShadowCaster2D` en los prefabs de faroles, carteles, vallas, obstáculos y barriles, y en la moto y
los zombies (una silueta simple, no el contorno del dibujo). El fondo lejano no proyecta. En
calidad Baja se apagan todas.

## 6. Presupuesto y calidad

- `LightBudget` (C# plano, testeado): recibe las luces de adorno candidatas (posición, radio,
  prioridad) y el tope, y decide cuáles quedan prendidas — siempre las más cercanas a la cámara.
  Un componente lo aplica cada cierto tiempo, no cada frame.
- Las luces del gameplay (faro, destellos, horda) nunca entran al presupuesto.
- `GameSettings.LightQuality` (Alta / Baja, default Alta, clave `zombineta.lightQuality`) y un
  desplegable en Opciones. En Baja: sin sombras, sin luces de adorno, sin destellos; quedan el
  ambiente y el faro.

## 7. Taller de luz

Herramienta `Zombineta > Luz > Construir taller` que genera `Scenes/TallerLuz.unity`:

- Una fila por capa del escenario, con una copia de cada prefab de esa capa a su altura real,
  separadas y agrupadas por el campo `grupo`, con un cartel con el nombre de cada una.
- La luz de ambiente del perfil elegido (por default `Noche.asset`), y la moto con su faro para
  comparar escalas y ver cómo reacciona cada asset.
- Un objeto con el `LightingDirector` para poder cambiar de perfil desde el inspector.
- Se puede regenerar cuando arte agrega prefabs; no se edita a mano.

## 8. Pruebas

**EditMode:**
- `LightBudget`: respeta el tope; prioriza las más cercanas; con tope 0 apaga todo; determinista.
- Mapeo de capas: cada capa del escenario y cada slot de `LaneSorting` cae en la Sorting Layer
  esperada.
- Aplicación del `PerfilDeLuz`: color e intensidad por capa, y perfil vacío = sin luces globales.
- Los tests actuales (209) siguen en verde.

**Play por MCP:**
- Capturas del mismo tramo: Alta vs Baja, faro prendido vs apagado, un barril explotando.
- Tiempo de cuadro promedio antes y después del sub-proyecto, en el mismo tramo, en Alta y en Baja.
- Consola sin errores; recorrido del flujo hasta el nivel.

**A mano:** arte abre un prefab, le cambia una luz y lo ve en el taller y en el nivel.

## Riesgos

- **Costo de las luces**: es lo que responde la medición de tiempo de cuadro. Si Alta no entra en
  las notebooks del equipo, se bajan los topes por default.
- **Sombras y arte cambiante**: cada silueta nueva necesita su `ShadowCaster2D`; el taller es donde
  se nota si falta.
- **Migración a prefabs**: los niveles y el escenario tienen que seguir viéndose igual antes de
  sumar luz; se verifica con una captura comparada contra la actual.
