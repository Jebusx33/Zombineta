# Playtest de Jesús — hallazgos y resultado de la reunión

Fuente: notas de Jesús (primera hora de testeo), las respuestas de Seba y la **reunión del equipo**
posterior. Lo que se decidió está en la sección **"Resultado de la reunión"** y se ejecuta en el plan
nuevo: [`2026-10-21-controles-granadas-oleadas.md`](2026-10-21-controles-granadas-oleadas.md).
Estado: **solo planificado**.

## Resultado de la reunión (decisiones del equipo)

La dinámica del juego cambió un poco. Lo que quedó:

1. **Un solo esquema de controles, como los beat'em up** (se descarta el esquema WASD + clics que se había
   propuesto antes):
   - **A pie:** flechas para mover · **X** atacar (golpe y combo) · **Z** disparo.
   - **Moto:** flechas (↑/↓ carril como movimiento principal) · **Espacio** luz · **X** disparo ·
     **Z** bomba/granada *(nuevo)*.
   - **Mando:** se queda como estaba (RT/LT acelerar y frenar, A golpe, X disparo) y suma **Y** granada
     *(nuevo)*.
2. **Granadas:** arma secundaria de la moto, un **recolectable más** en el camino. Los **tanques que
   explotan dejan de tener sentido en la moto** y se sacan de ahí.
3. **Barriles:** podrían quedar solo en el **beat'em up**, para explotarlos a balazos *(a revisar con el
   equipo)*.
4. **La ambulancia no gustó para nada y se saca**. En su lugar:
   - **Sin timer.** La pelea se gana **venciendo a toda la oleada** de zombies, y recién ahí se
     puede seguir. El **sistema de notas B/A/S se queda**.
   - Un **recolectable** (la misma granada) usado a pie funciona como **bomba**: elimina a todos los
     enemigos activos de la pantalla. **No frena la oleada**.
5. **La activación manual de la ambulancia** que pedía Jesús **queda descartada**: ya no hay ambulancia.

## Qué dijo Jesús y en qué quedó

| # | Jesús | Qué se decidió | Hallazgo en el código |
|---|-------|----------------|------------------------|
| 1 | Los controles son incómodos (X, Z, J con WASD/flechas) | **Esquema único de beat'em up** (X, Z y flechas); el mapeo cambia según el modo. Remapeo en Opciones: opcional | Hoy: Disparo = X o clic izq.; Golpe = Z, J o clic der. **Z y J eran la misma acción** (no hay patada). No existe pantalla de remapeo |
| 2 | Latencia entre la animación del ataque y la colisión | Pulir (Fase 5 del plan nuevo) | El golpe se resuelve en el mismo cuadro en que se aprieta, **pero uno apretado durante el enfriamiento (0,27 s) se pierde sin aviso** (no hay cola de entrada): se siente "lento" aunque no lo sea |
| 3 | Los genéricos deben morir con 3 impactos como máximo | De acuerdo; con balazos hay que **definir** | Común 3 golpes, Corredor 2, Pesado 5. El balazo mata por **tirada al azar** (85 % común, 100 % corredor, 35 % pesado) |
| 4 | "El sistema de combate condiciona al jugador" | **Aclarar entre todos** (sin fecha) | — |
| 5 | Z y J (golpe/patada) lejos: no se encadenan combos | Con X como único ataque **desaparece** el problema de teclas lejanas. Queda por definir con Juana lo de los combos alternando A–B | Hay **una sola** acción de golpe (cadena de 3 con remate) |
| 6 | Las animaciones cuerpo a cuerpo son lentas: leer la frecuencia de pulsación y escalar el ritmo, como en los arcade | De acuerdo; se pidió y **no quedó bien** (Fase 5) | Se subió la velocidad una vez (0,35 → 0,27 s), pero es fija; no depende de qué tan rápido se apriete |
| 7 | La ambulancia no gusta; debería activarse a mano | **Se saca la ambulancia**; la pelea se gana venciendo a la oleada y hay bomba/granada recolectable. **No** será manual | Hoy: el auxilio llega a los 30 s pase lo que pase; la furgoneta cruza y deja el bidón |
| 8 | "El tanque se vacía y el avatar no hace la transición" | Faltan assets/animaciones; hoy pasable | **El nivel 2 tenía `ayudas = 0`**: sin nafta no había pelea, era Game Over; en el nivel 1 se acababa tras 3 auxilios. Muy probablemente era esto. **Resuelto el 21/10: se sacó el límite y todos los niveles pelean** (Fase 7 del plan de controles) |
| 9 | Spawn: grupos de 3 a 6, 1–2 por carril | Puede ser, pero Seba lo sintió **fácil**; se ajusta con datos | Oleadas de 3 / 5 / 6 a la vez (ya está en 3–6). No hay tope por carril |
| 10 | Mejoró, pero "no mucho más divertido" | Las granadas, las bombas y el objetivo de "vencer la oleada" apuntan a eso | Los sonidos de golpe y remate siguen **sin clips** (pendiente de José): pesa mucho en la sensación |

## Qué cambió respecto del borrador anterior

- **Controles:** se reemplaza el esquema "WASD/flechas + clic izquierdo/derecho + Espacio" por el de
  **X / Z / flechas**. El remapeo deja de ser necesario para arrancar.
- **Fin de la pelea:** de "vencer a todos **o** tope de tiempo, con un pibe que tira un bidón" a **vencer
  a toda la oleada, sin timer y sin pibe/ambulancia**.
- **Aparecen** las granadas, la bomba y los barriles en el beat'em up, que no estaban en el borrador.

## Qué sigue

Todo el trabajo (fases 1 a 6) está en [`2026-10-21-controles-granadas-oleadas.md`](2026-10-21-controles-granadas-oleadas.md).
**Hecho:** controles nuevos (X, Z, flechas y Espacio), granada y bomba como recursos con HUD, pelea por
oleadas sin timer (4 oleadas, el último zombie suelta un bidón) y barriles en la arena. **Pendiente:** la
respuesta del combate (cola de entrada y ritmo según la pulsación) y el ajuste de encuentros con datos. La pelea
del nivel 2 ya está (Fase 7: todos los niveles se resuelven peleando).
