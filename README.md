# 🎮 Zombineta (TBD)

Juego de conducción, supervivencia y evasión 2.5D desarrollado en Unity.

---

## 📖 Descripción

**Zombineta** es un videojuego de supervivencia y conducción frenética en 2.5D, donde el jugador toma el rol de una valiente repartidora en una motocicleta Vespa. Su misión es llevar alimentos a clientes hambrientos a través de una carretera post-apocalíptica mientras evade a una implacable horda de zombies. 

El juego contrasta la alta tensión de una persecución constante con un tono ligero: la protagonista es inmune al virus zombie simplemente gracias a su buena alimentación y su rutina estricta de cardio. No hay armas ni combate directo; la clave es la velocidad, los reflejos y la gestión del entorno.

---

## 🎯 Objetivo del Proyecto

* **Narrativo:** Transmitir la adrenalina de realizar un trabajo cotidiano (delivery) en el escenario más hostil posible, manteniendo un humor irónico.
* **Jugable:** Dominar el cambio rápido de carriles, el uso de rampas y la gestión del tiempo bajo la presión constante de un enemigo invencible.
* **Conceptual:** Transformar el *survival horror* tradicional eliminando el combate y enfocándose puramente en la evasión táctica a alta velocidad.

---

## 🕹️ Mecánicas Principales

* **Conducción en carriles:** Movimiento vertical fluido (estilo *Excitebike*) para sortear vehículos abandonados y barricadas.
* **Sistema de saltos:** Uso de rampas para evitar trampas en el suelo y alcanzar objetos elevados.
* **Gestión de recursos:**
  * **Combustible:** Disminuye constantemente; quedarse en cero permite que la horda te alcance.
  * **Batería (Faro):** Permite ver en la oscuridad, pero su uso prolongado agota la energía.
* **Interacción rápida:** Activación de interruptores al paso para abrir puertas blindadas o activar trampas ambientales.

> ❗ **No existe combate directo.** El enfoque está en la evasión milimétrica, mantener la inercia y administrar los recursos para no ser devorado.

---

## 🏠 Mundo del Juego

* **Escenario:** Carreteras industriales y rurales destruidas, envueltas en oscuridad y niebla.
* **Estructura:** Tramos lineales con obstáculos generados o diseñados para exigir reflejos rápidos.
* **Progresión visual:** Uso de múltiples capas de *parallax* para dar profundidad, con una atmósfera opresiva que contrasta fuertemente con el color brillante de la Vespa de la protagonista.

---

## 🎭 Personajes

* **Repartidora (Jugador):** Ágil, decidida e inmune al virus. Su única preocupación es que el pedido no llegue frío.
* **La Horda (Antagonista):** Una masa oscura e inagotable con ojos brillantes. No son enemigos individuales, sino una fuerza de la naturaleza que avanza dinámicamente según los errores del jugador.

---

## 🎨 Estilo Artístico

* Estética de horror cinematográfico con elementos semi-realistas y figuras de alto contraste.
* Paleta fuertemente desaturada (negros, grises y azules profundos).
* Iluminación focalizada: luces cálidas naranjas y amarillas provenientes del faro de la motocicleta y los interruptores.
* Modelado 3D optimizado para vista lateral.

---

## 🔊 Audio

* Sonido dinámico del motor que reacciona a los saltos, la velocidad y el nivel de combustible.
* Audio espacial opresivo de la horda acercándose o alejándose según la distancia real.
* Efectos ambientales rápidos: recolección de bidones, derrapes, activación de mecanismos mecánicos pesados.

---

## 🗂️ Estructura del repositorio

| Carpeta | Qué es |
| :--- | :--- |
| `Prototipo/` | Proyecto de Unity del **prototipo** (entregable de E2). Congelado: solo arreglos para los playtests. Estado de cierre en el tag `prototipo-v1`. |
| `Juego/` | Proyecto de Unity del **juego definitivo**, en construcción. |
| `Juego/Assets/_Zombineta/Simulacion/` | Las reglas del juego en C# plano, con sus tests. Las usan **los dos proyectos**: el prototipo la importa como paquete local. |
| `docs/` | Diseños y planes de cada etapa. |
| `HANDOFF.md` | Estado técnico detallado para retomar el desarrollo. |

---

## ⚙️ Configuración Técnica

| Componente | Detalle |
| :--- | :--- |
| **Motor** | Unity 6 (Versión 6000.x) |
| **Lenguaje** | C# |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Audio Engine** | FMOD Studio / Reaper (Mezcla y diseño) |
| **Modelado 3D** | Blender |
| **Plataforma** | PC / WebGL |

---

## 📦 Dependencias

**FMOD Studio Unity Integration**

Este proyecto utiliza FMOD para la gestión del audio adaptativo y procedimental.

**Instalación:**
1. Abrir el proyecto en Unity.
2. Ir a *Window → Package Manager*.
3. Asegurarse de tener importado el paquete de integración de FMOD según la documentación oficial.
4. Vincular el proyecto de Unity con el banco de sonidos `.fspro` generado.

---

## 🎨 Assets y Texturas

Los modelos base generados (bloqueos geométricos y texturas iniciales) están organizados en el repositorio interno. 

**Instrucciones:**
1. Descargar la carpeta de modelos.
2. Importarla en `Assets/Models` y `Assets/Textures`.
3. Configurar los materiales usando los shaders URP (`Lit` / `Simple Lit`) ajustando el *emission* para los elementos interactivos.

---

## 🚧 Estado Actual

**Prototipo / Vertical Slice en desarrollo:**
* Sistema de movimiento en carriles implementado.
* Mecánicas de recolección de gasolina y batería.
* Presión dinámica de la horda operativa.
* Puzles de entorno básicos (puertas e interruptores).

---

## ▶️ Ejecución

1. Instalar **Unity 6000.6.0f1** (la misma versión para todo el equipo).
2. Abrir desde Unity Hub la carpeta `Prototipo/` (para jugar el prototipo) o `Juego/` (el definitivo). **No** la raíz del repo.
3. Prototipo: cargar `Assets/_Zombineta/Scenes/Prototipo.unity` y presionar *Play*.
4. Juego: cargar `Assets/_Zombineta/Scenes/Boot.unity`.

**Antes de clonar (una sola vez por máquina):**

```bash
git lfs install
```

Las imágenes, audios y modelos van por Git LFS: sin eso llegan como archivos de texto de 130 bytes y Unity muestra sprites rotos.

**Para mergear escenas y prefabs sin romperlas (una vez por máquina, ajustando la ruta a tu Unity):**

```bash
git config --global merge.unityyamlmerge.driver "'D:/Dev/Unity/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

Si alguien abrió la raíz por error y Unity creó `Assets/`, `Packages/` o `ProjectSettings/` sueltas en la raíz, se pueden borrar: git las ignora y no son parte del juego.

> Conviene clonar el repo en una ruta corta (por ejemplo `D:\Zombineta`): algunos paquetes de Unity tienen archivos con rutas muy largas y Windows no admite más de 260 caracteres.
