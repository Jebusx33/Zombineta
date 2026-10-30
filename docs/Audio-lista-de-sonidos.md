# Zombineta — Lista de audios

Inventario de todo el audio que el juego usa hoy y propuesta de lo que falta, armado a partir del
estado del proyecto al 25/10. La música va aparte, al final. Para el *cómo* (dónde se pisa un
archivo, volúmenes, loops) ver [`Guia-de-sonido.md`](Guia-de-sonido.md).

## Cómo leer las tablas

**Estado**

| Marca | Significa |
|---|---|
| ✅ | Audio definitivo, ya suena en el juego |
| 🟡 | Suena, pero es un **placeholder** sintetizado por código (hay que reemplazarlo) |
| ⚠️ | Está cargado pero tiene un problema (mudo, mal elegido, duración rara) |
| ❌ | No existe: el juego no hace ningún sonido ahí |

**Tipo**

- **Diegético**: el sonido existe dentro del mundo del juego; los personajes lo "oirían" (motor,
  golpes, zombies, explosiones).
- **No diegético**: lo oye solo el jugador; comunica o refuerza (alertas del HUD, stingers, notas).
- **UI**: refuerzo de interfaz (menús, botones, sliders).

**Prioridad**: **A** = hace falta para que el juego se entienda o se sienta terminado · **M** =
mejora clara de la experiencia · **B** = pulido.

**Programación**: **No** = solo cargar el audio · **Mínima** = el evento ya existe en el juego,
falta conectarle un sonido (una clave nueva y una línea) · **Sí** = hay que programar el momento.

---

## 1. Problemas a resolver primero

| # | Problema | Efecto en el juego | Cómo se arregla |
|---|---|---|---|
| 1 | `moto_iddle`, `moto_acelera` y `moto_cambio` están **en silencio** (todas las muestras en 0) | **El motor y el cambio de carril están mudos** | Reexportarlos y pisar los archivos con el mismo nombre |
| 2 | El cambio de carril usa `moto_acelera` (4,7 s) | Un sonido largo para una acción de 0,1 s; se pisa consigo mismo | Usar un clip corto (0,1–0,3 s); ¿`moto_cambio` era para esto? |
| 3 | El ambiente del Nivel 1 no tiene clip | La ciudad queda sin fondo sonoro | Asignar `amb_level1_city` u otro ambiente nuevo |
| 4 | El ambiente del Nivel 2 es `autopista_tension` (0,39 s) en loop | Se repite casi 3 veces por segundo: suena a zumbido | Un loop de ambiente de 8–20 s |
| 5 | Golpe1 y Golpe2 usan el mismo `player_punch` | El combo suena igual en cada golpe (solo cambia el tono) | Dos golpes distintos (y un tercero para variar) |
| 6 | Salto y GolpeAlAire usan el mismo `player_swoosh` | Se confunden entre sí | Un sonido propio para el salto de la rampa |
| 7 | `level_start.wav` y `Pinky Swings.mp3` están en el proyecto pero **no se usan** | — | Definir dónde van (ver secciones 5 y 9) |
| 8 | El popup de radio de los apagones tiene el **campo de sonido vacío** | Aparece en silencio | Cargar los mensajes de radio (sección 7) |
| 9 | 21 efectos siguen siendo placeholders sintetizados | El juego suena "de prototipo" | Reemplazarlos (marcados 🟡 abajo) |

---

## 2. Partida en moto (Nivel 1, Nivel 2 y Tutorial)

### 2.1 La moto y la protagonista — diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Motor en marcha (loop) | Siempre en la partida; el tono sube con la velocidad | ⚠️ | A | No | Mudo (problema 1) |
| Ralentí (loop) | Moto quieta o casi quieta; se cruza con la marcha | ⚠️ | A | No | Mudo (problema 1) |
| Cambio / acelerón | Al entrar en turbo | ❌ | M | No | Ya programado: solo falta el clip (`MotorCambio`) |
| Cambio de carril | Cada cambio de carril | ⚠️ | A | No | Problema 2. Corto, tipo "swish" de neumático |
| Disparo | Disparar con balas | 🟡 | A | No | |
| Sin balas (clic seco) | Disparar sin balas | 🟡 | A | No | |
| Faro on / off | Prender y apagar el faro | 🟡 | M | No | Clic + zumbido eléctrico |
| Choque contra obstáculo | Chocar | 🟡 | A | No | Ideal: variar según el obstáculo (metal del auto, madera del kiosko, bolsas de basura) → **Sí** |
| Salto en rampa | Despegar | ⚠️ | M | No | Problema 6 |
| Aterrizaje | Caer sin ángulo perfecto | 🟡 | M | No | |
| Aterrizaje perfecto | Caer con el ángulo justo | 🟡 | M | No | Puede llevar un brillo no diegético encima |
| Caída de la moto | Se cae al aterrizar mal (`Fell`) | ❌ | A | Mínima | Golpe + metal arrastrándose |
| Frenando sin nafta | La moto se queda sin nafta y frena antes de bajarse | ❌ | A | Mínima | Motor que tose y se apaga. Hoy solo suena "sin nafta" |
| Sin nafta | Se vacía el tanque | 🟡 | A | No | |
| Sin batería | El faro se apaga solo (`RanOutOfBattery`) | ❌ | M | Mínima | Chisporroteo y apagado |
| Faro titilando (batería baja) | Batería < 25 %, el faro parpadea | ❌ | B | Sí | Chisporroteo sincronizado con el parpadeo |
| Granada lanzada | Lanza una granada hacia atrás | ❌ | A | Mínima | Silbido corto + "clinc" de la anilla |
| Granada explota | Cae sobre la horda | ❌ | A | Mínima | Hoy no suena nada |
| Granada denegada | Quiere tirar y no tiene | ❌ | M | Mínima | Mismo criterio que "sin balas" |

### 2.2 Recolectables — diegético con refuerzo

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Bidón de nafta | Agarrarlo | 🟡 | A | No | Líquido + clic |
| Batería | Agarrarla | 🟡 | A | No | Zumbido eléctrico ascendente |
| Caja de balas | Agarrarla | 🟡 | A | No | Cartuchos |
| Recolectable que se acerca / flota | Al pasar por el carril de al lado y atraerlo | ❌ | B | Sí | "Imán" suave |

### 2.3 Zombies y horda — diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Horda (loop posicional) | Siempre; sube al acercarse, también en la pelea | ✅ | A | No | `zombie_horde` |
| Gemidos sueltos de la horda | Con la horda cerca, cada 2,5–6 s | ✅ | M | No | `zombie-sound` / `zombie-bite` |
| Zombie de frente | Entra en rango un zombie que viene de frente | ✅ | A | No | |
| Atropello | Pisar un zombie | 🟡 | A | No | |
| Bala que impacta | La bala le pega a un zombie | ❌ | A | Mínima | Carne; confirma el acierto |
| Zombie muere por bala | Muere de un tiro | ❌ | A | Mínima | Grito corto + cuerpo que cae |
| Disparo errado | La bala no le pega a nada (`ShotMissed`) | ❌ | B | Mínima | Rebote lejano |
| Horda retrocede | Dos aciertos seguidos la frenan | ❌ | A | Mínima | Gruñido colectivo de dolor: premia el acierto |
| Barril explota | Bala o reacción en cadena | 🟡 | A | No | El Nivel 2 tiene una variante propia |
| Zombie saltando | Zombies que saltan al bajarse de la moto | ❌ | B | Sí | |

### 2.4 Mundo y ambiente — diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Ambiente Nivel 1 (ciudad) | Loop de fondo | ⚠️ | A | No | Problema 3. Viento, sirenas lejanas, perros, vidrios |
| Ambiente Nivel 2 (puente / autopista) | Loop de fondo | ⚠️ | A | No | Problema 4. Viento abierto, metal del puente, agua abajo |
| Apagón: corte de luz | Empieza el apagón | ❌ | A | Sí | Zumbido eléctrico que se corta en seco: el momento más dramático del nivel |
| Apagón: vuelve la luz | Termina el apagón | ❌ | M | Sí | Transformador que arranca, tubos que parpadean |
| Postes que titilan | Faroles del escenario que parpadean | ❌ | B | Sí | Chisporroteo posicional |
| Autos abandonados (Nivel 2) | Al pasar junto a autos del embotellamiento | ❌ | M | Sí | Alarmas de auto, bocinas trabadas, radios encendidas |
| Lluvia (si se suma al nivel) | Loop | ❌ | B | Sí | Solo si el arte suma lluvia |

---

## 3. Pelea a pie (beat'em up)

### 3.1 Protagonista — diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Golpe 1 | Primer golpe de la cadena | ✅ | A | No | Problema 5 |
| Golpe 2 | Segundo golpe | ⚠️ | A | No | Mismo clip que el 1 |
| Remate | Tercer golpe, derriba a los que la rodean | ✅ | A | No | `player_spin` |
| Golpe al aire | No le pega a nadie | ✅ | M | No | |
| Le pegan | Un zombie la lastima | ✅ | A | No | `zombie-bite`; sumar un quejido de la protagonista |
| Bajarse de la moto | Salta de la moto al empezar la pelea | ❌ | M | Mínima | Salto + pisada |
| Pasos | Caminando por la arena | ❌ | B | Sí | Sincronizado con la animación |
| Voz: esfuerzo al golpear | Con el remate | ❌ | B | Mínima | 2–3 variantes |
| Voz: dolor | Al recibir un golpe | ❌ | M | Mínima | 2–3 variantes |
| Derribada | Pierde la pelea | ❌ | A | Mínima | Caída + quejido |
| Bomba puesta | Planta la bomba temporizadora | ❌ | A | Mínima | Clic + mecha |
| Mecha de la bomba (loop) | Mientras la bomba espera | ❌ | A | Sí | Tic-tac que se acelera: avisa cuándo explota |
| Bomba explota | Caen todos los zombies | ❌ | A | Mínima | La explosión más grande del juego |
| Bomba denegada | Sin bombas o nadie a quien explotar | ❌ | M | Mínima | |
| Volver a la moto / arranque | Termina la pelea, arranca de nuevo | ❌ | A | Sí | Patada de arranque + motor que vuelve |

### 3.2 Zombies en la pelea — diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Zombie prepara el ataque | El aviso de 0,6 s antes de pegar | ❌ | **A** | Sí | **El más importante de la pelea**: el oído avisa antes que el ojo. Gruñido de carga |
| Zombie recibe golpe | Le pegan y aguanta | ❌ | A | Sí | Quejido / carne |
| Zombie derribado | Cae por el remate | ❌ | M | Sí | Cuerpo contra el piso |
| Zombie se levanta | Vuelve a pararse | ❌ | B | Sí | |
| Zombie muere a golpes | Último golpe | ❌ | A | Sí | |
| Pesado | Variante más grave para el zombie pesado | ❌ | B | Sí | |

### 3.3 Drops — diegético con refuerzo

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Cae un drop | Un zombie suelta botiquín o balas | ❌ | M | Sí | |
| Botiquín recogido | +1 corazón | ❌ | A | Mínima | Positivo, cálido |
| Balas recogidas | +2 balas | ❌ | A | Mínima | Puede reusar "caja de balas" |
| Drop lleno | Pisa algo que no necesita | ❌ | B | Mínima | "Bloop" negativo suave |
| Drop por desaparecer | Titila los últimos 3 s | ❌ | B | Sí | Tic suave |
| Bidón del último zombie | Aparece el bidón que cierra la pelea | ❌ | A | Mínima | Indica hacia dónde ir |

### 3.4 Refuerzo de la pelea — no diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Empieza la pelea | "¡SIN NAFTA!" y se baja | ❌ | A | Mínima | Stinger corto de alarma |
| Oleada nueva | Cartel "OLEADA 2" | ❌ | A | Mínima | Bocina o golpe de percusión |
| Última oleada | "¡ÚLTIMA OLEADA!" | ❌ | M | Mínima | Variante más intensa |
| Contador de combo | "GOLPE" / "COMBO x2" / "¡REMATE!" | ❌ | B | Sí | Tick que sube de tono con el combo |
| Reintento | "¡OTRA VEZ!" | ❌ | M | Mínima | |
| Pelea ganada | Vence la última oleada | ❌ | A | Mínima | Stinger de victoria corto |
| Nota de la pelea | Aparece la letra C / B / A / S | ❌ | M | Sí | Uno por nota: la S tiene que sonar a premio |
| Premio | "+3 BALAS +40 % BATERÍA" | ❌ | B | Sí | Conteo |
| "¡A CORRER!" | Vuelve a la moto | ❌ | M | Mínima | |

---

## 4. Interfaz en partida (HUD) — no diegético

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Aviso de obstáculo en carril | Aparece el icono de alerta del carril | ❌ | A | Sí | Pitido corto; tono distinto por carril ayuda a ubicarlo |
| Poca nafta | "¡POCA NAFTA!" parpadea (< 25 %) | ❌ | A | Sí | Pitido de reserva, no molesto |
| Sensor de zombies | El radar del HUD con la distancia | ❌ | M | Sí | "Ping" que se acelera con la cercanía (como sensor de estacionamiento) |
| Batería baja | Barra de batería en rojo | ❌ | B | Sí | |
| Popup de radio: apagón | El popup de radio, 5–10 s antes del apagón | ⚠️ | A | No | Campo listo, sin audio (problema 8) |
| Popup de radio: otros avisos | Puente, auxilio, poca nafta (sección 7) | ❌ | M | Sí | |
| Mitad del recorrido / hito | La barra de progreso pasa un hito | ❌ | B | Sí | |
| Llegada al refugio | Cruza la meta | 🟡 | A | No | Hoy suena "Victoria" al pasar de pantalla |

---

## 5. Transiciones y flujo del juego

| Sonido | Cuándo suena | Estado | Tipo | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Intro del estudio | Video antes del menú | ✅ | — | — | No | Trae su propio audio en el video |
| Inicio de nivel | Al arrancar a correr | ❌ | No diegético | A | Mínima | **`level_start.wav` ya está en el proyecto sin usar** |
| Cambio de pantalla | Fundido entre pantallas | ❌ | No diegético | B | Sí | Un "whoosh" suave, opcional |
| Entrar a pausa | Abrir el menú de pausa | ❌ | UI | M | Sí | La música ya baja sola; falta el clic |
| Salir de pausa | Volver al juego | ❌ | UI | M | Sí | |
| Página de cinemática | Pasar de viñeta / página | ❌ | No diegético | M | Sí | Papel o golpe de cómic |
| Saltar cinemática | Saltar | ❌ | UI | B | Sí | |
| Victoria (stinger) | Pasar a "Nivel completo" | 🟡 | No diegético | A | No | |
| Derrota (stinger) | Pasar a "Game Over" | 🟡 | No diegético | A | No | |
| Atrapada por la horda | El momento en que te alcanza, antes del Game Over | ❌ | Diegético | A | Mínima | Horda encima + grito; hoy corta directo al stinger |

---

## 6. Menús y pantallas

### 6.1 Navegación — UI

| Sonido | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Mover selección | Moverse por un menú | 🟡 | A | No | |
| Confirmar | Enter / botón Sur | 🟡 | A | No | |
| Volver / cancelar | Esc / botón Este | 🟡 | A | No | |
| Slider de volumen | Mover un slider en Opciones | ❌ | A | Sí | Un tick que suena **al volumen elegido** (así se prueba el volumen de efectos) |
| Interruptor on/off | Sombras, pantalla completa, etc. | ❌ | M | Sí | |
| Opción no disponible | Algo bloqueado | ❌ | B | Sí | |
| Hover con el mouse | Pasar el mouse por un botón | ❌ | B | Sí | Más suave que "mover" |

### 6.2 Pantallas

| Pantalla | Sonido | Estado | Tipo | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Menú principal | Ambiente detrás de la música (viento, gemido lejano) | ❌ | Diegético | B | Sí | Acompaña la animación del menú |
| Menú principal | "Jugar" | ❌ | UI | M | Sí | Moto que arranca: mejor que el confirmar genérico |
| Selección de personaje | Cambiar de personaje | ❌ | UI | M | Sí | |
| Selección de personaje | Elegir personaje | ❌ | UI | M | Sí | Acelerón + frase corta del personaje |
| Nivel completo | Conteo de estadísticas (metros, bajas, notas) | ❌ | No diegético | M | Sí | Tick que sube + golpe final |
| Game Over | Gemidos lejanos al azar | 🟡 | Diegético | M | No | |
| Game Over | "Reintentar" | ❌ | UI | B | Sí | |
| Final y Créditos | Viento al azar | 🟡 | Diegético | B | No | |
| Tutorial | Paso completado | ❌ | No diegético | A | Sí | "Ding" que confirma que lo hizo bien |
| Tutorial | Paso nuevo / cartel | ❌ | No diegético | M | Sí | |

---

## 7. Voces de radio (Central) — no diegético para el jugador, diegético para la historia

2 o 3 variantes por evento; el juego elige al azar sin repetir el último. Textos propuestos en la
conversación del 21/10.

| Evento | Variantes | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Aviso de apagón | 3 | ❌ | A | No | Va en el popup de radio que ya existe |
| Puente / embotellamiento (Nivel 2) | 3 | ❌ | M | Sí | Falta el momento que lo dispare |
| Sin nafta → pelea | 3 | ❌ | A | Mínima | Con el evento "empieza la pelea" |
| Poca nafta | 2 | ❌ | M | Sí | Junto al aviso "¡POCA NAFTA!" |
| Pelea ganada / a correr | 2 | ❌ | B | Mínima | |
| Clic + estática de radio | 1–2 | ❌ | A | No | Antes y después de cada mensaje |

---

## 8. Resumen

| Grupo | Total | ✅ | 🟡 | ⚠️ | ❌ |
|---|---|---|---|---|---|
| Moto y protagonista (2.1) | 19 | 0 | 7 | 4 | 8 |
| Recolectables (2.2) | 4 | 0 | 3 | 0 | 1 |
| Zombies y horda (2.3) | 10 | 3 | 2 | 0 | 5 |
| Mundo y ambiente (2.4) | 7 | 0 | 0 | 2 | 5 |
| Pelea: protagonista (3.1) | 15 | 4 | 0 | 1 | 10 |
| Pelea: zombies (3.2) | 6 | 0 | 0 | 0 | 6 |
| Pelea: drops (3.3) | 6 | 0 | 0 | 0 | 6 |
| Pelea: refuerzo (3.4) | 9 | 0 | 0 | 0 | 9 |
| HUD (4) | 8 | 0 | 1 | 1 | 6 |
| Transiciones (5) | 10 | 1 | 2 | 0 | 7 |
| Menús y pantallas (6) | 17 | 0 | 5 | 0 | 12 |
| Radio (7) | 6 | 0 | 0 | 0 | 6 |
| **Total** | **117** | **8** | **20** | **8** | **81** |

**Por dónde empezar (prioridad A, en este orden):**
1. Reexportar los tres audios de la moto (el motor está mudo).
2. Ambientes de los dos niveles.
3. Zombie prepara el ataque, zombie recibe golpe y zombie muere (la pelea se juega con el oído).
4. Bomba (puesta, mecha, explosión) y granada (lanzada, explosión).
5. Bala que impacta, zombie que muere por bala y horda que retrocede.
6. Corte de luz del apagón y el mensaje de radio.
7. Inicio de nivel (`level_start.wav`, ya está), oleada nueva, pelea ganada.
8. Avisos del HUD: obstáculo en carril, poca nafta.
9. Reemplazar los placeholders 🟡 de disparo, choque, pickups, UI y stingers.

---

## 9. Música (sección aparte)

### 9.1 Lo que hay hoy

| Pantalla / momento | Archivo | Duración | Estado | Nota |
|---|---|---|---|---|
| Menú principal | `mus_menu_loop` | 207 s | ✅ | |
| Opciones | `mus_menu_loop` | 207 s | ✅ | Comparte con el menú: sigue sin corte, está bien |
| Selección de personaje | `mus_menu_loop` | 207 s | ✅ | **`mus_character_loop` existe y no se usa**: decidir si va un tema propio |
| Cinemática | `mus_cinematic_loop` | 30 s | ✅ | |
| Nivel 1 — tema | `mus_level1_loop` | 150 s | ✅ | |
| Nivel 1 — tensión | `zombie-tribe-loop` | 8,3 s | ✅ | Loop libre, tope 0,3; sube con la horda |
| Nivel 2 — tema | `mus_level2_loop` | 16 s | 🟡 | Placeholder |
| Nivel 2 — tensión | `mus_level2_tension` | 16 s | 🟡 | Placeholder. Tiene que medir lo mismo que el tema (o ir como loop libre) |
| Nivel completo | `mus_levelcomplete_loop` | 16 s | 🟡 | Placeholder |
| Game Over | `mus_gameover_loop` | 16 s | 🟡 | Placeholder |
| Final | `mus_ending_loop` | 16 s | 🟡 | Placeholder |
| Créditos | `mus_credits_loop` | 16 s | 🟡 | Placeholder |
| Tutorial | (el tema del nivel que use) | — | 🟡 | Podría tener un tema propio, más tranquilo |

Sin usar en el proyecto: `Pinky Swings.mp3` (30 s), `mus_level1_tension` (16 s, reemplazada por
la tribal) y la carpeta `Musica/old/` (versiones viejas de menú, cinemática y nivel 1).

### 9.2 Lo que sumaría

| Música | Cuándo | Prio | Prog. | Nota |
|---|---|---|---|---|
| Tema de pelea | Mientras se pelea a pie | **A** | Sí | Hoy la pelea sigue con el tema del nivel; un cambio de música marca "ahora es otro juego". Más percusión, más corto (loop de 30–60 s) |
| Capa de última oleada | Encima del tema de pelea en la oleada final | M | Sí | Igual que la tensión: misma duración que el tema de pelea |
| Stinger de inicio de nivel | Al arrancar | A | Mínima | Puede ser `level_start.wav` |
| Stinger de pelea ganada | Al vencer la última oleada | A | Mínima | 1–2 s |
| Stinger por nota (C/B/A/S) | Al mostrar la nota de la pelea | M | Sí | La S, la más festiva |
| Música del apagón | Durante el apagón | M | Sí | El tema baja a un filtro "ahogado" (o se corta) y queda solo la tensión: refuerza la oscuridad |
| Tema del puente (Nivel 2) | Tramo del embotellamiento | B | Sí | Variación más tensa del tema del Nivel 2 |
| Tema de selección de personaje | Pantalla de selección | B | No | Si se quiere distinguir del menú (`mus_character_loop` ya está como placeholder) |
| Tema del tutorial | Tutorial | B | Sí | Más calmo, para aprender sin presión |
| ¿`Pinky Swings`? | A definir | — | — | Candidatos: créditos, final o nivel completo |

### 9.3 Formatos y duraciones

- Música en WAV u OGG, 44,1 kHz. Los temas, en loop sin clic (ver `Guia-de-sonido.md`, sección 7).
- **Tema y tensión de un mismo nivel deben medir exactamente lo mismo**, salvo que la tensión vaya
  como **loop libre** (`Tension Libre` en el `AudioDeNivel`), como la tribal del Nivel 1.
- Stingers: 1–3 s, sin loop.
- Efectos: WAV 44,1 kHz, 16 o 24 bit. Cortos y sin silencio al principio (cualquier silencio
  inicial se siente como retraso).
