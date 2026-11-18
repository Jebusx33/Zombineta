<p align="center">
  <img src="Juego/Assets/_Zombineta/Art/UI/sprite_logo_deadlivery_2.png" alt="DeadLivery" width="480">
</p>

<h1 align="center">DeadLivery</h1>

<p align="center">
  <b>El pedido llega. Aunque el mundo se haya terminado.</b><br>
  Persecución 2D en moto + beat'em up a pie, hecha en Unity 6 por <b>UCHAR</b>.
</p>

---

## 📖 De qué se trata

Una repartidora en motoneta tiene que llevar un pedido a través de una ciudad tomada por los zombies.
Atrás viene **la horda**: una masa de muertos que no se cansa y avanza más rápido cada vez que te
equivocás. Adelante, autos abandonados, barricadas, rampas y una calle de tres carriles que se apaga
con los cortes de luz.

La protagonista es inmune al virus (gracias a su buena alimentación y su rutina de cardio, claro), así
que su única preocupación es que el pedido no llegue frío.

> El repositorio se llama **Zombineta**, el nombre de trabajo del proyecto. El juego se llama **DeadLivery**.

---

## 🕹️ Cómo se juega

**En la moto.** Vista lateral con tres carriles. Cambiás de carril para esquivar, acelerás con el turbo
(gasta el triple de nafta), frenás para acomodarte y saltás por las rampas inclinando la moto en el aire
para caer bien. Las armas sirven para sacarte zombies de encima:

- **Faro:** con la luz prendida, la horda avanza a la mitad de velocidad. Gasta batería.
- **Pistola:** una bala por tu carril que mata al primer zombie que encuentra. Munición limitada.
- **Granada:** se tira hacia atrás y limpia todos los carriles; la horda tarda unos segundos en reponerse.

**A pie.** Cuando se acaba la nafta, la protagonista se baja y pelea: un beat'em up por oleadas con
combo de tres golpes, disparo, granadas y una bomba con temporizador. El último zombie suelta un bidón:
lo agarrás, llenás el tanque y volvés a la moto. Cada pelea termina con una nota (B, A o S) según los
golpes que recibiste.

**En el camino** aparecen bidones de nafta, baterías, balas, granadas y bombas. Administrarlos es la
mitad del juego: el faro compra tiempo, la pistola compra espacio y el turbo te puede dejar a pie en el
peor lugar.

### Niveles

| | Nivel | Qué tiene |
| :---: | :--- | :--- |
| 🎓 | **Tutorial** | Corto y sin derrota: enseña los controles y los recursos en menos de un minuto. Aparece la primera vez y se puede repetir desde el menú. |
| 1️⃣ | **La salida** | La ciudad de noche, con llovizna y dos apagones de tormenta donde el faro es lo único que ves. |
| 2️⃣ | **El puente** | Viento, niebla y una horda más agresiva. En la recta final, **el jefe**: un duelo en moto con tres corazones. |

Cada nivel abre con una cinemática en forma de página de cómic, y entre nivel y nivel un mapa en un
televisor viejo muestra por dónde va el recorrido.

---

## 🎮 Controles

Se juega con **teclado** o **joystick** (Xbox, PlayStation o cualquiera que Unity reconozca como gamepad).
Los botones cambian de función según estés en la moto o a pie.

| | Teclado | Joystick |
| :--- | :---: | :---: |
| **🛵 En la moto** | | |
| Cambiar de carril | ↑ / ↓ | Stick izq. / D-pad |
| Turbo (mantener) | → | RT / R2 |
| Frenar / retroceder (mantener) | ← | LT / L2 |
| Disparar | X | X / □ · RB / R1 |
| Granada | Z | LB / L1 |
| Faro (prender/apagar) | Espacio | Y / △ |
| Inclinar en el aire | ← / → | LT / RT |
| **🥊 A pie** | | |
| Moverse | Flechas | Stick izq. / D-pad |
| Golpear (3 seguidos = remate) | X | A / ✕ |
| Disparar | Z | X / □ · RB / R1 |
| Granada | C | LB / L1 |
| Bomba con temporizador | Espacio | Y / △ |
| **⏸️ Siempre** | | |
| Pausa | Esc | Start |

---

## 👥 El equipo

DeadLivery lo hace **UCHAR**, un equipo de cinco estudiantes de la carrera de Producción de Videojuegos
de la Universidad de la Ciudad de Buenos Aires, para el Taller de Proyecto Integral.

| | Integrante | Rol |
| :---: | :--- | :--- |
| 🔊 | **José Luis Ressia** | Desarrollo · Sonido |
| 🧪 | **Jesús Gabriel Arias** | Desarrollo · Testing |
| 📋 | **Sebastian Ariel Capurro** | Desarrollo · Producción |
| 🎨 | **Juana Urzua Montelpare** | Ilustración · Level Design |
| 🎨 | **Germán Heuer** | Ilustración · Level Design |

El **game design** lo hicimos entre todos.

**Docentes:** Martin Romero (Producción) y Nicolas Matias Crescenzi (Taller de Proyecto Integral).

---

## 🧰 Herramientas y recursos

| | |
| :--- | :--- |
| **Motor** | Unity 6000.6.0f1 · Universal Render Pipeline 2D · Input System |
| **Ilustración** | Krita · PaintTool SAI |
| **Programación** | C#, con Claude Code como asistente |
| **Música** | Suno |
| **Efectos de sonido** | [Pixabay](https://pixabay.com) (los autores están en los créditos del juego) |
| **Partículas** | Particle Pack de [Kenney](https://kenney.nl) · CC0 |
| **Plataformas** | Windows y Android (probado en una Powkiddy X55) |

---

## ▶️ Abrir el proyecto

**Una sola vez por máquina, antes de clonar:**

```bash
git lfs install
```

Las imágenes, los audios y los modelos van por Git LFS. Si no lo instalás, llegan como archivos de
texto de 130 bytes y Unity muestra los sprites rotos.

Para que Unity pueda mergear escenas y prefabs sin romperlos, ajustá la ruta a tu Unity:

```bash
git config --global merge.unityyamlmerge.driver "'D:/Dev/Unity/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

**Después:**

1. Instalá **Unity 6000.6.0f1**. Todo el equipo usa exactamente esa versión.
2. Clonalo en una ruta corta, por ejemplo `D:\Zombineta`. Algunos paquetes de Unity tienen rutas muy
   largas y Windows no acepta más de 260 caracteres.
3. Desde Unity Hub abrí la carpeta **`Juego/`**, no la raíz del repo.
4. Abrí `Assets/_Zombineta/Scenes/Boot.unity` y dale Play.

Para probar una pantalla o un nivel suelto, abrí esa escena y dale Play: `Boot` se carga sola.

**Atajos de prueba** (solo en el editor y en builds de desarrollo):

| Tecla | Qué hace |
| :---: | :--- |
| F1 | Muestra los números de la partida |
| F2 / F3 | Gana / pierde el nivel |
| F4 | Vacía el tanque para ir directo a la pelea a pie |
| F5 | Salta al jefe del Nivel 2 |
| F6 | Cambia el estilo de imagen (normal, blanco y negro, película vieja) |

**Build de Android:** menú *Zombineta → Build → Android*. El APK queda en `Builds/Android/`, que no se sube
al repo.

**Tests:** *Window → General → Test Runner → EditMode*. Las reglas del juego tienen tests propios y
conviene correrlos antes de subir.

---

## 🗂️ Qué hay en el repo

| Carpeta | Qué es |
| :--- | :--- |
| `Juego/` | El proyecto de Unity del juego. |
| `Juego/Assets/_Zombineta/Simulacion/` | Las reglas del juego en C# puro, sin depender de las escenas: nafta, batería, carriles, horda, jefe. Están testeadas. |
| `Juego/Assets/_Zombineta/Settings/` | Los números que se ajustan sin programar: balance, niveles, cinemáticas, créditos. |
| `Prototipo/` | El prototipo de la Etapa 2, congelado. Su estado final está en el tag `prototipo-v1`. |
| `docs/` | Guías para arte y sonido, la lista de sonidos, las novedades y los diseños de cada etapa. |
| `HANDOFF.md` | El estado técnico completo, para retomar el desarrollo. |

Cada integrante trabaja en su propia rama (`German`, `Jesus`, `Jose`, `Juana`, `Seba`), y `master`
tiene la versión del equipo.

---

## 📚 Para seguir leyendo

- [`HANDOFF.md`](HANDOFF.md): cómo está armado todo, qué está verificado y qué falta.
- [`docs/referencia-proyecto.md`](docs/referencia-proyecto.md): la arquitectura, clase por clase.
- [`docs/Guia-de-iluminacion-para-arte.md`](docs/Guia-de-iluminacion-para-arte.md) y
  [`docs/Guia-de-sonido.md`](docs/Guia-de-sonido.md): cómo sumar arte y sonido al juego.
