# Zombineta — Lista de audios

Inventario de todo el audio que el juego usa hoy y propuesta de lo que falta, armado a partir del
estado del proyecto al 01/11 y puesto al día el 06/11 con los audios editados. Los efectos con audio editado suenan a volumen 1 (vienen ecualizados), salvo la horda (`Horda`, `ZombieAdelante`) y la moto (`Motor`, `MotorRalenti`, `MotorCambio`), que desde el 10/11 van a 0,8. La música del Nivel 1 (`mus_level1_loop`) se subió 2 dB en el archivo el 10/11: si se vuelve a exportar desde Reaper, sumarlos ahí. La música va aparte, al final. Para el *cómo* (dónde se pisa un
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

**Archivo**: cómo tiene que llamarse el audio; las reglas están en la sección siguiente.

**Programación**: **No** = solo cargar el audio · **Mínima** = el evento ya existe en el juego,
falta conectarle un sonido (una clave nueva y una línea) · **Sí** = hay que programar el momento.

## Cómo nombrar los archivos

Todo en minúsculas, con guion bajo y sin acentos: `grupo_que_es.wav`. Las columnas **Archivo** de
cada tabla traen el nombre sin la extensión.

| Empieza con | Para qué |
|---|---|
| `moto_` | La moto, en español porque ya son así: `moto_apaga_motor` |
| `player_` | La protagonista y sus armas: `player_pistol_shot` |
| `zombie_` y `horde_` | Un zombie o la horda |
| `pickup_` y `drop_` | Lo que se recoge y lo que queda en el piso |
| `barrel_` y `world_` | Explosivos y sonidos del mundo |
| `hud_` y `ui_` | Avisos en partida, menús y pantallas |
| `screen_` | Sonidos al azar de una pantalla |
| `stinger_` | Golpes musicales cortos |
| `radio_` | Voces de la Central |
| `amb_` y `mus_` | Ambientes en `Audio/Ambiente/` y música en `Audio/Musica/` |

- **Varias variantes:** se numeran desde `_01`. Si el sonido es uno solo, no lleva número. Por
  ejemplo, `moto_choque_01` a `_02` significa `moto_choque_01.wav` y `moto_choque_02.wav`.
- **`sfx_` no se usa más:** ese prefijo marca los placeholders generados por código. El audio final
  no lo lleva, así que `sfx_pickup_fuel_01` se reemplaza por `pickup_fuel`.
- **Si el archivo ya tiene su nombre final:** pisalo con el mismo nombre y entra solo. Un nombre
  nuevo hay que conectarlo una vez en su asset de `Settings/Audio/Sonidos/`.
- **Nombres viejos:** `moto_iddle`, `player_granade`, `zombie-sound` y `zombie-bite` se quedan así
  porque ya están conectados, aunque tengan una errata o un guion.

---

## 1. Problemas a resolver primero

| # | Problema | Efecto en el juego | Cómo se arregla |
|---|---|---|---|
| 1 | `moto_iddle`, `moto_acelera` y `moto_cambio` están **en silencio** (todas las muestras en 0) | **El motor y el cambio de carril están mudos** | **Resuelto (06/11):** vinieron reexportados, y `moto_cambio` ya suena al entrar en turbo |
| 2 | El cambio de carril usa `moto_acelera` (4,7 s) | Un sonido largo para una acción de 0,1 s; se pisa consigo mismo | **Resuelto (06/11):** `moto_carril` (2 s, más largo que lo pedido: probarlo con muchos cambios seguidos) |
| 3 | El ambiente del Nivel 1 no tiene clip | La ciudad queda sin fondo sonoro | **Resuelto (06/11):** `amb_level1_city` nuevo, de 40 s |
| 4 | El ambiente del Nivel 2 es `autopista_tension` (0,39 s) en loop | Se repite casi 3 veces por segundo: suena a zumbido | **Resuelto (06/11):** `autopista_tension` ahora es un pulso de 2,1 s que se repite |
| 5 | Golpe1 y Golpe2 usan el mismo `player_punch` | El combo suena igual en cada golpe (solo cambia el tono) | Dos golpes distintos: `player_punch_01` y `player_punch_02` |
| 6 | Salto y GolpeAlAire usan el mismo `player_swoosh` | Se confunden entre sí | **Resuelto (06/11):** `moto_salto` |
| 7 | `level_start.wav` y `Pinky Swings.mp3` están en el proyecto pero **no se usan** | — | **`level_start` resuelto (06/11):** suena al arrancar a correr. `Pinky Swings` es el mismo audio que `mus_cinematic_loop` |
| 8 | El popup de radio de los apagones tiene el **campo de sonido vacío** | Aparece en silencio | Cargar los mensajes de radio (sección 7) |
| 9 | 28 efectos siguen siendo placeholders sintetizados | El juego suena "de prototipo" | Reemplazarlos (marcados 🟡 abajo) |

---

## 2. Partida en moto (Nivel 1, Nivel 2 y Tutorial)

### 2.1 La moto y la protagonista — diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Motor en marcha (loop) | `moto_acelera` | Siempre en la partida; el tono sube con la velocidad | ✅ | A | No | `moto_acelera` |
| Ralentí (loop) | `moto_iddle` | Moto quieta o casi quieta; se cruza con la marcha | ✅ | A | No | `moto_iddle` |
| Cambio / acelerón | `moto_cambio` | Al entrar en turbo | ✅ | M | No | `moto_cambio` (`MotorCambio`) |
| Cambio de carril | `moto_carril` | Cada cambio de carril | ✅ | A | No | `moto_carril`. Dura 2 s: probarlo con muchos cambios seguidos |
| Disparo | `player_pistol_shot` | Disparar con balas | ✅ | A | No | `player_pistol_shot` |
| Sin balas (clic seco) | `player_empty_gun` | Disparar sin balas | ✅ | A | No | `player_empty_gun` |
| Faro on / off | `moto_faro_on` y `moto_faro_off` | Prender y apagar el faro | 🟡 | M | No | Clic + zumbido eléctrico |
| Choque contra obstáculo | `moto_choque_01` a `_02` | Chocar | ✅ | A | No | Hay una sola variante (`_01`). Ideal: variar según el obstáculo (metal del auto, madera del kiosko, bolsas de basura) → **Sí** |
| Salto en rampa | `moto_salto` | Despegar | ✅ | M | No | `moto_salto` |
| Aterrizaje | `moto_aterriza` | Caer sin ángulo perfecto | ✅ | M | No | `moto_aterriza` |
| Aterrizaje perfecto | `moto_aterriza_perfecto` | Caer con el ángulo justo | 🟡 | M | No | Puede llevar un brillo no diegético encima |
| Caída de la moto | `moto_caida` | Se cae al aterrizar mal (`Fell`) | ❌ | A | Mínima | Golpe + metal arrastrándose |
| Frenando sin nafta | `moto_apaga_motor` | La moto se queda sin nafta y frena antes de bajarse | ✅ | A | Mínima | Lo cubre `moto_apaga_motor`, el mismo de "Sin nafta" |
| Sin nafta | `moto_apaga_motor` | Se vacía el tanque | ✅ | A | No | `moto_apaga_motor` |
| Sin batería | `moto_sin_bateria` | El faro se apaga solo (`RanOutOfBattery`) | ❌ | M | Mínima | Chisporroteo y apagado |
| Faro titilando (batería baja) | `moto_faro_titila` | Batería < 25 %, el faro parpadea | ✅ | B | Sí | Loop mientras titila (`LoopsDeNivel`) |
| Granada lanzada | `player_granade_throw` | Lanza una granada hacia atrás | ❌ | A | Mínima | Silbido corto + "clinc" de la anilla |
| Granada explota | `player_granade` | Cae sobre la horda | ✅ | A | Mínima | `player_granade`. Reemplaza a la explosión de barril |
| Granada denegada | `player_empty_gun` | Quiere tirar y no tiene | ✅ | M | Mínima | `player_empty_gun`, solo si no quedan granadas |

### 2.2 Recolectables — diegético con refuerzo

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Bidón de nafta | `pickup_fuel` | Agarrarlo | 🟡 | A | No | Líquido + clic |
| Batería | `pickup_battery` | Agarrarla | 🟡 | A | No | Zumbido eléctrico ascendente |
| Caja de balas | `pickup_ammo` | Agarrarla | 🟡 | A | No | Cartuchos |
| Granada | `pickup_grenade` | Agarrarla | 🟡 | A | No |  |
| Bomba | `pickup_bomb` | Agarrarla | 🟡 | A | No |  |
| Recolectable que se acerca / flota | `pickup_magnet` | Al pasar por el carril de al lado y atraerlo | ❌ | B | Sí | "Imán" suave |

### 2.3 Zombies y horda — diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Horda (loop posicional) | `zombie_horde` | Siempre; sube al acercarse, también en la pelea | ✅ | A | No | `zombie_horde` |
| Gemidos sueltos de la horda | `zombie-sound` y `zombie-bite` | Con la horda cerca, cada 2,5–6 s | ✅ | M | No | `zombie-sound` / `zombie-bite` |
| Zombie de frente | `zombie-sound` y `zombie-bite` | Entra en rango un zombie que viene de frente | ✅ | A | No |  |
| Atropello | `zombie_runover` | Pisar un zombie | ✅ | A | No | `zombie_runover` |
| Bala que impacta | `zombie_hit_bullet` | La bala le pega a un zombie | ✅ | A | Mínima | Suena también si lo mata. Los golpes a pie no lo disparan |
| Zombie muere por bala | `zombie_death_bullet` | Muere de un tiro | ❌ | A | Mínima | Grito corto + cuerpo que cae |
| Disparo errado | `player_shot_miss` | La bala no le pega a nada (`ShotMissed`) | ❌ | B | Mínima | Rebote lejano |
| Horda retrocede | `horde_retreat` | Dos aciertos seguidos la frenan | ❌ | A | Mínima | Gruñido colectivo de dolor: premia el acierto |
| Barril explota | `barrel_explosion_01` a `_02` y `barrel_explosion_nivel2` | Bala o reacción en cadena | 🟡 | A | No | El Nivel 2 tiene una variante propia |
| Zombie saltando | `zombie_jump` | Zombies que saltan al bajarse de la moto | ❌ | B | Sí |  |

### 2.4 Mundo y ambiente — diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Ambiente Nivel 1 (ciudad) | `amb_level1_city` | Loop de fondo | ✅ | A | No | `amb_level1_city` (40 s) |
| Ambiente Nivel 2 (puente / autopista) | — | — | ✅ | A | No | Sin ambiente propio desde el 10/11: el nivel suena con su tema (el pulso de la autopista), la tensión y el viento (`amb_wind`). Se sacaron el `Sonido` `AmbienteNivel2` y el archivo `Sfx/autopista_tension` (el pulso de 2,1 s). `amb_level2_bridge` queda sin usar |
| Apagón: corte de luz | `world_blackout_start` | Empieza el apagón | ✅ | A | Sí | Suena cuando la luz empieza a bajar (oscuridad 0,25). Zumbido eléctrico que se corta en seco: el momento más dramático del nivel |
| Apagón: vuelve la luz | `world_blackout_end` | Termina el apagón | ✅ | M | Sí | Suena cuando la luz recuperó casi la mitad, también si se prende para pelear. Transformador que arranca, tubos que parpadean |
| Postes que titilan | `world_pole_flicker` | Faroles del escenario que parpadean | ❌ | B | Sí | Chisporroteo posicional |
| Autos abandonados (Nivel 2) | `world_car_alarm_01` a `_02` | Al pasar junto a autos del embotellamiento | ✅ | M | Sí | Cada auto tiene 35 % de chance a 10 m, con 6–12 s de descanso entre alarmas. Solo en el banco del Nivel 2. Panea mientras suena: arranca a la derecha, pasa por el centro al cruzar el auto y se va a la izquierda bajando (10/11) |
| Lluvia | `amb_rain` | Loop, con el volumen de la lluvia del tramo | ✅ | M | Sí | Ya llueve en el Nivel 1 (los apagones son la tormenta). El volumen sale de `ClimaDirector.Lluvia`, con una curva (`curvaLluvia` 0,5 en `LoopsDeNivel`) para que la lluvia media ya se oiga fuerte |
| Viento | `amb_wind` | Loop, con el volumen del viento del tramo | ✅ | M | Sí | Sopla en todo el Nivel 2 y en las tormentas del Nivel 1. El volumen sale de `ClimaDirector.Viento` |

### 2.5 Jefe del Nivel 2 — diegético

Desde el 10/11 todos tienen su archivo y su clave (`Jefe*` en `SonidoClave`, en el banco global). Los avisos y el disparo
suenan desde donde está el jefe, detrás de la moto. El impacto, la herida, el rebote y la derrota suenan al centro y
sin atenuar: el impacto cae junto a la moto, y los otros tres son la respuesta al tiro del jugador (desde el jefe, a la
izquierda y al 74 %, los tapaba el disparo propio). `boss_hurt` (+11 dB) y `boss_death` (+6 dB) se subieron en el
archivo el 10/11: venían muy bajos; si se vuelven a exportar desde Reaper, sumarlo ahí.

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Fija el carril | `boss_aim` | Empieza el aviso de un ataque (también el de la embestida) | ✅ | A | Sí | Es la señal para esquivar |
| Disparo orgánico | `boss_shot` | Suelta el proyectil, frontal o elevado | ✅ | A | Sí | |
| Impacto en el piso | `boss_impact` | Cae el proyectil elevado | ✅ | A | Sí | Junto a la moto, sin paneo |
| Orugas | `boss_tank` | Loop mientras dura el duelo | ✅ | A | Sí | `OrugasDelJefe` (6.º hijo de `SonidoDelNivel`). Rodando va a 0,6; en el aviso de embestida sube de tono (1 → 1,25) y de volumen (0,6 → 0,85), y embistiendo va a 1,55 de tono y volumen 1 (unos 4 dB más). Afloja al volver y casi se para aturdido. La distancia lo baja poco (`cuantoAtenua` 0,4) y panea a medias (`paneo` 0,5): del todo a la izquierda y al 73 % se perdía. Archivo subido 7 dB (10/11). Se calla derrotado o al terminar la partida |
| Embestida | (las orugas) | Se lanza por el carril | ✅ | A | Sí | No tiene golpe propio: la cuentan las orugas aceleradas. `boss_charge` no existe |
| Recibe daño | `boss_hurt` | Le dan estando aturdido | ✅ | M | Sí | Al centro, sin atenuar. Archivo subido 11 dB |
| Rebote | `boss_bounce` | Le dan cuando no se lo puede lastimar | ✅ | M | Sí | Al centro, sin atenuar, 0,09 s después del tiro (`demoraRebote` en `SfxDirector`) y más agudo (tono 1,12 a 1,25): junto con el disparo propio no se distinguía |
| Derrota | `boss_death` | Se le acaba la vida | ✅ | A | Sí | Al centro, sin atenuar. Archivo subido 6 dB. Las orugas se apagan con un fundido |

---

## 3. Pelea a pie (beat'em up)

### 3.1 Protagonista — diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Golpe 1 | `player_punch_01` | Primer golpe de la cadena | ✅ | A | No | Problema 5 |
| Golpe 2 | `player_punch_02` | Segundo golpe | ⚠️ | A | No | Mismo clip que el 1 |
| Remate | `player_spin` | Tercer golpe, derriba a los que la rodean | ✅ | A | No | `player_spin` |
| Golpe al aire | `player_swoosh` | No le pega a nadie | ✅ | M | No |  |
| Le pegan | `zombie-bite` | Un zombie la lastima | ✅ | A | No | `zombie-bite`; sumar un quejido de la protagonista |
| Bajarse de la moto | `player_dismount` | Salta de la moto al empezar la pelea | ❌ | M | Mínima | Salto + pisada |
| Pasos | `player_step_01` a `_04` | Caminando por la arena | ❌ | B | Sí | Sincronizado con la animación |
| Voz: esfuerzo al golpear | `player_voice_effort_01` a `_03` | Con el remate | ❌ | B | Mínima | 2–3 variantes |
| Voz: dolor | `player_voice_hurt_01` a `_03` | Al recibir un golpe | ❌ | M | Mínima | 2–3 variantes |
| Derribada | `player_defeat` | Pierde la pelea | ❌ | A | Mínima | Caída + quejido |
| Bomba puesta | `player_c4_mecha` | Planta la bomba temporizadora | ✅ | A | Mínima | Empieza el primer pitido de `player_c4_mecha` |
| Mecha de la bomba | `player_c4_mecha` | Mientras la bomba espera | ✅ | A | Sí | `player_c4_mecha`: dura lo mismo que `bombaMecha` (2 s). Si cambia ese valor, hay que reeditar el clip |
| Bomba explota | `player_c4_explosion` | Caen todos los zombies | ✅ | A | Mínima | `player_c4_explosion`. Reemplaza a la explosión de barril |
| Bomba denegada | `player_empty_gun` | Sin bombas o nadie a quien explotar | ✅ | M | Mínima | `player_empty_gun`, solo si no quedan bombas |
| Volver a la moto / arranque | `moto_arranque` | Termina la pelea, arranca de nuevo | ❌ | A | Sí | Patada de arranque + motor que vuelve |

### 3.2 Zombies en la pelea — diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Zombie prepara el ataque | `zombie_attack_charge_01` a `_03` | El aviso de 0,6 s antes de pegar | ❌ | **A** | Sí | **El más importante de la pelea**: el oído avisa antes que el ojo. Gruñido de carga |
| Zombie recibe golpe | `zombie_hit_01` a `_03` | Le pegan y aguanta | ❌ | A | Sí | Quejido / carne |
| Zombie derribado | `zombie_knockdown` | Cae por el remate | ❌ | M | Sí | Cuerpo contra el piso |
| Zombie se levanta | `zombie_getup` | Vuelve a pararse | ❌ | B | Sí |  |
| Zombie muere a golpes | `zombie_death_punch_01` a `_02` | Último golpe | ❌ | A | Sí |  |
| Pesado | `zombie_heavy` | Variante más grave para el zombie pesado | ❌ | B | Sí |  |

### 3.3 Drops — diegético con refuerzo

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Cae un drop | `drop_fall` | Un zombie suelta botiquín o balas | ❌ | M | Sí |  |
| Botiquín recogido | `pickup_medkit` | +1 corazón | ❌ | A | Mínima | Positivo, cálido |
| Balas recogidas | `pickup_ammo` | +2 balas | ❌ | A | Mínima | Puede reusar "caja de balas" |
| Drop lleno | `drop_full` | Pisa algo que no necesita | ❌ | B | Mínima | "Bloop" negativo suave |
| Drop por desaparecer | `drop_fade` | Titila los últimos 3 s | ❌ | B | Sí | Tic suave |
| Bidón del último zombie | `drop_fuel` | Aparece el bidón que cierra la pelea | ❌ | A | Mínima | Indica hacia dónde ir |

### 3.4 Refuerzo de la pelea — no diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Empieza la pelea | `stinger_fight_start` | "¡SIN NAFTA!" y se baja | ❌ | A | Mínima | Stinger corto de alarma |
| Oleada nueva | `stinger_wave` | Cartel "OLEADA 2" | ❌ | A | Mínima | Bocina o golpe de percusión |
| Última oleada | `stinger_last_wave` | "¡ÚLTIMA OLEADA!" | ❌ | M | Mínima | Variante más intensa |
| Contador de combo | `hud_combo_tick` | "GOLPE" / "COMBO x2" / "¡REMATE!" | ❌ | B | Sí | Tick que sube de tono con el combo |
| Reintento | `stinger_retry` | "¡OTRA VEZ!" | ❌ | M | Mínima |  |
| Pelea ganada | `stinger_fight_win` | Vence la última oleada | ❌ | A | Mínima | Stinger de victoria corto |
| Nota de la pelea | `stinger_grade_c`, `_b`, `_a` y `_s` | Aparece la letra C / B / A / S | ❌ | M | Sí | Uno por nota: la S tiene que sonar a premio |
| Premio | `hud_reward_count` | "+3 BALAS +40 % BATERÍA" | ❌ | B | Sí | Conteo |
| "¡A CORRER!" | `stinger_run` | Vuelve a la moto | ❌ | M | Mínima |  |

---

## 4. Interfaz en partida (HUD) — no diegético

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Aviso de obstáculo en carril | `hud_lane_warning` | Aparece el icono de alerta del carril | ❌ | A | Sí | Pitido corto; tono distinto por carril ayuda a ubicarlo |
| Poca nafta | `hud_low_fuel` | La nafta entra en el último 20 % y cada 5 s mientras se gasta | 🟡 | A | No | Pitido de reserva, no molesto |
| Sensor de zombies | `hud_sensor_ping` | El radar del HUD con la distancia | ❌ | M | Sí | Por ahora no hace falta: el sensor está oculto |
| Batería baja | `hud_low_battery` | La batería entra en el último 20 % y cada 5 s mientras se gasta | 🟡 | B | No | |
| Popup de radio: apagón | `radio_blackout_01` a `_03` | El popup de radio, 5–10 s antes del apagón | ⚠️ | A | No | Campo listo, sin audio (problema 8) |
| Popup de radio: otros avisos | Ver sección 7 | Puente, auxilio, poca nafta (sección 7) | ❌ | M | Sí |  |
| Mitad del recorrido / hito | `hud_milestone` | La barra de progreso pasa un hito | ❌ | B | Sí |  |
| Llegada al refugio | `stinger_goal` | Cruza la meta | 🟡 | A | No | Hoy suena "Victoria" al pasar de pantalla |

---

## 5. Transiciones y flujo del juego

| Sonido | Archivo | Cuándo suena | Estado | Tipo | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|---|
| Intro del estudio | — | Video antes del menú | ✅ | — | — | No | Trae su propio audio en el video |
| Inicio de nivel | `level_start` | Al arrancar a correr | ✅ | No diegético | A | Mínima | Una vez por partida y en cada reintento; volver a la moto después de pelear no cuenta |
| Cambio de pantalla | `ui_transition` | Fundido entre pantallas | ❌ | No diegético | B | Sí | Un "whoosh" suave, opcional |
| Entrar a pausa | `ui_pause` | Abrir el menú de pausa | ❌ | UI | M | Sí | La música ya baja sola; falta el clic |
| Salir de pausa | `ui_resume` | Volver al juego | ❌ | UI | M | Sí |  |
| Página de cinemática | `ui_page` | Pasar de viñeta / página | ❌ | No diegético | M | Sí | Papel o golpe de cómic |
| Saltar cinemática | `ui_skip` | Saltar | ❌ | UI | B | Sí |  |
| Victoria (stinger) | `stinger_win_01` a `_02` | Pasar a "Nivel completo" | 🟡 | No diegético | A | No |  |
| Derrota (stinger) | `stinger_lose_01` a `_02` | Pasar a "Game Over" | 🟡 | No diegético | A | No |  |
| Atrapada por la horda | `horde_catch` | El momento en que te alcanza, antes del Game Over | ❌ | Diegético | A | Mínima | Horda encima + grito; hoy corta directo al stinger |

---

## 6. Menús y pantallas

### 6.1 Navegación — UI

| Sonido | Archivo | Cuándo suena | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Mover selección | `ui_move` | Moverse por un menú | 🟡 | A | No |  |
| Confirmar | `ui_confirm` | Enter / botón Sur | 🟡 | A | No |  |
| Volver / cancelar | `ui_back` | Esc / botón Este | 🟡 | A | No |  |
| Slider de volumen | `ui_slider_tick` | Mover un slider en Opciones | ❌ | A | Sí | Un tick que suena **al volumen elegido** (así se prueba el volumen de efectos) |
| Interruptor on/off | `ui_toggle` | Sombras, pantalla completa, etc. | ❌ | M | Sí |  |
| Opción no disponible | `ui_denied` | Algo bloqueado | ❌ | B | Sí |  |
| Hover con el mouse | `ui_hover` | Pasar el mouse por un botón | ❌ | B | Sí | Más suave que "mover" |

### 6.2 Pantallas

| Pantalla | Sonido | Archivo | Estado | Tipo | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|---|
| Menú principal | Ambiente detrás de la música (viento, gemido lejano) | `amb_menu` | ❌ | Diegético | B | Sí | Acompaña la animación del menú |
| Menú principal | "Jugar" | `ui_play` | ❌ | UI | M | Sí | Moto que arranca: mejor que el confirmar genérico |
| Selección de personaje | Cambiar de personaje | `ui_character_change` | ❌ | UI | M | Sí |  |
| Selección de personaje | Elegir personaje | `ui_character_select` | ❌ | UI | M | Sí | Acelerón + frase corta del personaje |
| Mapa | La tele se prende | `map_start` | ✅ | No diegético | A | Sí | `MapaEncendido`, lo toca `PantallaCRT` en el mismo cuadro en que aparece el punto blanco. Se le recortaron 30 ms de silencio al principio y se subió 4 dB (pico -1,3 dB), volumen 1 (10/11) |
| Nivel completo | Conteo de estadísticas (metros, bajas, notas) | `ui_stats_tick` y `ui_stats_end` | ❌ | No diegético | M | Sí | Tick que sube + golpe final |
| Game Over | Zombies al azar | `zombie-sound` y `zombie_horde` | 🟡 | Diegético | M | No | Desde el 10/11, por ahora: se intercalan cada 6 a 14 s (`GameOverZombie` 0,2 y `GameOverHorda` 0,1, unos 6 dB debajo de la música). `screen_groan_01`/`_02` (`ScreenGroan`) quedan sin usar |
| Game Over | "Reintentar" | `ui_retry` | ❌ | UI | B | Sí |  |
| Final y Créditos | Viento al azar | `screen_wind` | 🟡 | Diegético | B | No |  |
| Tutorial | Paso completado | `ui_step_done` | ❌ | No diegético | A | Sí | "Ding" que confirma que lo hizo bien |
| Tutorial | Paso nuevo / cartel | `ui_step_new` | ❌ | No diegético | M | Sí |  |

---

## 7. Voces de radio (Central) — no diegético para el jugador, diegético para la historia

2 o 3 variantes por evento; el juego elige al azar sin repetir el último. Textos propuestos en la
conversación del 21/10.

| Evento | Archivo | Variantes | Estado | Prio | Prog. | Nota |
|---|---|---|---|---|---|---|
| Aviso de apagón | `radio_blackout_01` a `_03` | 3 | ❌ | A | No | Va en el popup de radio que ya existe |
| Puente / embotellamiento (Nivel 2) | `radio_bridge_01` a `_03` | 3 | ❌ | M | Sí | Falta el momento que lo dispare |
| Sin nafta → pelea | `radio_no_fuel_01` a `_03` | 3 | ❌ | A | Mínima | Con el evento "empieza la pelea" |
| Poca nafta | `radio_low_fuel_01` a `_02` | 2 | ❌ | M | Sí | Junto al aviso "¡POCA NAFTA!" |
| Pelea ganada / a correr | `radio_fight_won_01` a `_02` | 2 | ❌ | B | Mínima |  |
| Clic + estática de radio | `radio_static_01` a `_02` | 1–2 | ❌ | A | No | Antes y después de cada mensaje |

---

## 8. Resumen

| Grupo | Total | ✅ | 🟡 | ⚠️ | ❌ |
|---|---|---|---|---|---|
| Moto y protagonista (2.1) | 19 | 14 | 2 | 0 | 3 |
| Recolectables (2.2) | 6 | 0 | 5 | 0 | 1 |
| Zombies y horda (2.3) | 10 | 5 | 1 | 0 | 4 |
| Mundo y ambiente (2.4) | 8 | 7 | 0 | 0 | 1 |
| Jefe del Nivel 2 (2.5) | 8 | 8 | 0 | 0 | 0 |
| Pelea: protagonista (3.1) | 15 | 8 | 0 | 1 | 6 |
| Pelea: zombies (3.2) | 6 | 0 | 0 | 0 | 6 |
| Pelea: drops (3.3) | 6 | 0 | 0 | 0 | 6 |
| Pelea: refuerzo (3.4) | 9 | 0 | 0 | 0 | 9 |
| HUD (4) | 8 | 0 | 3 | 1 | 4 |
| Transiciones (5) | 10 | 2 | 2 | 0 | 6 |
| Menús y pantallas (6) | 17 | 0 | 5 | 0 | 12 |
| Radio (7) | 6 | 0 | 0 | 0 | 6 |
| **Total** | **128** | **44** | **18** | **2** | **64** |

**Por dónde empezar (prioridad A, en este orden):**
Ya resuelto el 06/11: el motor, los ambientes, el impacto de bala, el apagón y el inicio de nivel.

1. Zombie prepara el ataque, zombie recibe golpe y zombie muere (la pelea se juega con el oído).
2. Granada lanzada.
3. Zombie que muere por bala y horda que retrocede.
4. El mensaje de radio del apagón.
5. Oleada nueva y pelea ganada.
6. Aviso del HUD de obstáculo en carril.
7. Reemplazar los placeholders 🟡 de pickups, UI y stingers.

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
| Nivel 2 — tema | `mus_level2_loop` | 0,39 s | ✅ | Es `autopista_tension` en loop: un zumbido corto que da tensión. Se carga sin streaming para que el loop no corte |
| Nivel 2 — tensión | `zombie-tribe-loop` | 8,3 s | ✅ | La misma del Nivel 1 desde el 10/11, con el mismo tope de 0,3. Va como loop libre: el tema dura 0,39 s. `mus_level2_tension` queda sin usar |
| Nivel completo | `mus_victoria_loop` | 160 s | ✅ | "Ragged Held Chord" (10/11), bajada 10,5 dB para quedar a -25 LUFS como el resto de la música |
| Game Over | `mus_derrota_loop` | 178 s | ✅ | "Snarl in the Cellar" (10/11), bajada 13,2 dB (-25 LUFS) |
| Final | `mus_victoria_loop` | 160 s | ✅ | La misma de nivel completo: del nivel completo al final y a los créditos sigue sin cortarse |
| Créditos | `mus_victoria_loop` | 160 s | ✅ | La misma |
| Tutorial | (el tema del nivel que use) | — | 🟡 | Podría tener un tema propio, más tranquilo |

Sin usar en el proyecto: `Pinky Swings.mp3` (30 s, el mismo tema que `mus_cinematic_loop`), `mus_level1_tension` (16 s, reemplazada por
la tribal) y la carpeta `Musica/old/` (versiones viejas de menú, cinemática y nivel 1).

### 9.2 Lo que sumaría

| Música | Archivo | Cuándo | Prio | Prog. | Nota |
|---|---|---|---|---|---|
| Tema de pelea | `mus_fight_loop` | Mientras se pelea a pie | **A** | Sí | Hoy la pelea sigue con el tema del nivel; un cambio de música marca "ahora es otro juego". Más percusión, más corto (loop de 30–60 s) |
| Capa de última oleada | `mus_fight_tension` | Encima del tema de pelea en la oleada final | M | Sí | Igual que la tensión: misma duración que el tema de pelea |
| Stinger de inicio de nivel | `level_start` | Al arrancar | A | Mínima | Puede ser `level_start.wav` |
| Stinger de pelea ganada | `stinger_fight_win` | Al vencer la última oleada | A | Mínima | 1–2 s |
| Stinger por nota (C/B/A/S) | `stinger_grade_c`, `_b`, `_a` y `_s` | Al mostrar la nota de la pelea | M | Sí | La S, la más festiva |
| Música del apagón | `mus_blackout_loop` | Durante el apagón | M | Sí | El tema baja a un filtro "ahogado" (o se corta) y queda solo la tensión: refuerza la oscuridad |
| Tema del puente (Nivel 2) | `mus_level2_bridge_loop` | Tramo del embotellamiento | B | Sí | Variación más tensa del tema del Nivel 2 |
| Tema de selección de personaje | `mus_character_loop` | Pantalla de selección | B | No | Si se quiere distinguir del menú (`mus_character_loop` ya está como placeholder) |
| Tema del tutorial | `mus_tutorial_loop` | Tutorial | B | Sí | Más calmo, para aprender sin presión |
| ¿`Pinky Swings`? | — | A definir | — | — | Candidatos: créditos, final o nivel completo |

### 9.3 Formatos y duraciones

- Música en WAV u OGG, 44,1 kHz. Los temas, en loop sin clic (ver `Guia-de-sonido.md`, sección 7).
- **Tema y tensión de un mismo nivel deben medir exactamente lo mismo**, salvo que la tensión vaya
  como **loop libre** (`Tension Libre` en el `AudioDeNivel`), como la tribal del Nivel 1.
- Stingers: 1–3 s, sin loop.
- Efectos: WAV 44,1 kHz, 16 o 24 bit. Cortos y sin silencio al principio (cualquier silencio
  inicial se siente como retraso).
