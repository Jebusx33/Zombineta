# Novedades del 05/11: lo que cambió desde tus commits del 03/11

Sebastián: esto es lo que subió José después de tu "Build 0.0.0.12". Tus cambios (2 oleadas, rodeo de la fila e
ícono de la C4) se mergearon sin conflictos y siguen tal cual. El detalle técnico está en `HANDOFF.md` y en
`docs/referencia-proyecto.md`. Al final hay una lista de lo que conviene revisar si tocás esas partes.

## 1. Arte nuevo

- **La protagonista a pie está pintada.** Quieta, caminando, golpe bajo, remate y golpe recibido usan los sprites
  pintados de arte (`Art/Player/APie/`). Tienen el mismo lienzo y el mismo pivote que los bocetos, así que no cambian
  ni el tamaño ni los tiempos. **La caída (`defeat`) sigue en gris:** no vino pintada.
- **Edificios del Nivel 1 más altos (v5).** Son los mismos dibujos con un piso más arriba, así que ya no se cortan en
  pantalla. La capa `Edificios` de `Escenario.asset` pasó de 6,2 a 9,19 de alto: los edificios quedan del mismo
  tamaño y crecen hacia arriba. Las máscaras de luz de las ventanas se rehicieron y ahora también se iluminan las
  ventanas de los pisos nuevos. Se borraron las imágenes v4.

## 2. Clima por tramos (lluvia, niebla y viento)

- Cada tramo de `LevelScene` tiene tres campos nuevos: **`lluvia`**, **`niebla`** y **`viento`**, de 0 a 1. Entran y
  salen con el mismo fundido que el apagón.
- **Es solo visual:** no cambia ninguna regla de la simulación.
- Lo dibuja `ClimaDirector` (`Scripts/Gameplay/Clima/`), que se crea solo al dar Play: **no hay objetos nuevos en
  las escenas.** Arma:
  - lluvia fina con salpicaduras en el asfalto;
  - tres bancos de niebla: lejos sobre los edificios, sobre la calle y delante de todo;
  - papeles que vuela el viento;
  - árboles que se mecen;
  - el haz del faro visible en el aire cuando hay niebla.
- En calidad **Baja** usa menos partículas y el faro no se ve en el aire.
- **Valores de ejemplo cargados:**
  - Nivel 1: llovizna desde "Desarrollar II", y los dos apagones son la tormenta.
  - Nivel 2: viento en todo el nivel, y niebla en el Cruce, la Persecución y la Recta final (donde está el jefe).

## 3. Estilo de imagen

- `LevelScene` tiene un campo nuevo, **`Estilo`**: Normal, Blanco y negro o Película vieja. Hoy los dos niveles
  están en Normal.
- **Cada nivel arranca en blanco y negro y toma color:** 1,2 s sin color y después 2,5 s de transición. Pasa también
  en cada reintento. Si el nivel tiene otro estilo, termina en ese.
- **Al ser atrapada por la horda,** la imagen pierde el color durante el plano final.
- El HUD no cambia: queda a color.
- Lo maneja `EstiloDeImagen` (`Scripts/Gameplay/Estilo/`), que también se crea solo.
- Funciona con un Volume de post-processing armado por código (saturación, sepia, grano, viñeta) y una capa encima
  con rayas, polvo y el parpadeo del proyector (`Resources/Estilo/PeliculaVieja.shader`).
- La cámara prende el post-processing solo mientras hay algún efecto, así que en Normal no cuesta nada.
- **F6** recorre los estilos mientras jugás (solo en el editor y en builds de desarrollo).

## 4. Transición de iris

- Al entrar y salir de un nivel, la pantalla ya no se funde a negro: se cierra en un círculo sobre la protagonista,
  en la moto o a pie, y se abre igual en la escena nueva. El resto de las pantallas siguen con el fundido.
- Está en `ScreenFader` y `GameRoot`, con el shader `Resources/Estilo/Iris.shader`.
- Se apaga con `irisEnLosNiveles` de `GameRoot` (escena Boot). La duración es `segundosIris` (0,7 s).

## 5. Nafta de gracia cerca de la meta

- **En los últimos 200 m antes del refugio la nafta alcanza siempre.** Si lo que queda no llega, el consumo baja en
  proporción: el indicador sigue bajando parejo y el tanque llega a la reserva justo en la meta.
- Los choques y los malos aterrizajes tampoco lo vacían en ese tramo.
- No actúa si el tanque ya estaba vacío antes, ni con el jefe vivo.
- Se ajusta en `GameConfig` con `metrosDeGracia` (0 lo apaga) y `reservaDeGracia`.
- **Ojo con el Nivel 1:** la última pelea antes del refugio sigue pasando si el tanque se vacía antes de esos 200 m.
  Lo único que cambia es que ya no se puede quedar sin nafta viendo la meta.

## 6. Atajos de prueba (editor y builds de desarrollo)

| Tecla | Qué hace |
|---|---|
| F2 / F3 | Ganar / perder el nivel (como antes) |
| F4 | Vaciar el tanque para llegar a la pelea (como antes) |
| F5 | Ir directo al jefe del Nivel 2 |
| F6 | Cambiar el estilo de imagen |

## 7. Si tocás estas partes, revisá esto

- **`GameConfig`:** se sumaron `metrosDeGracia`, `reservaDeGracia` y el campo `jefe`, que no se guarda en el asset.
  Tus oleadas y el rodeo no se tocaron.
- **`RunSimulation`:**
  - Todo lo que resta nafta usa ahora un piso: el consumo, los choques, las caídas y atropellar.
  - `BurnFuel` no gasta durante el duelo con el jefe.
  - `Progress01` y la meta pasan por `Meta`, que cambia con el jefe.
- **`Tramo`:** se sumaron los tres campos de clima. Si agregás tramos en una escena, arrancan en 0.
- **`LevelScene`:** se sumaron `jefe` y `estilo`.
- **`GameRoot` y `ScreenFader`:** el fundido ahora tiene un modo iris. Si cambiás cómo se cargan las escenas,
  fijate en `TocaUnNivel` y `CentroDeLaProtagonista`.
- **Ensamblado `Zombineta.Juego`:** ahora referencia `Unity.RenderPipelines.Core.Runtime`, que hace falta para el
  Volume.
- **Prueba en Unity:** con el Editor minimizado, el juego en Play no avanza. Las pruebas automáticas de clima, estilo
  e iris se hicieron avanzando cuadro por cuadro.

## 8. Lo que falta o no se probó

- No se probó en una build: el shader de rayas y polvo y el post-processing en un player.
- No se vio la apertura del iris al entrar a un nivel desde el mapa; sí el cierre al terminarlo.
- Faltan los sonidos de lluvia y viento (`amb_rain`, `amb_wind`, ya anotados en `docs/Audio-lista-de-sonidos.md`).
- Falta la caída pintada de la protagonista.
- La dificultad del jefe del Nivel 2 todavía no se jugó con teclado o joystick.
