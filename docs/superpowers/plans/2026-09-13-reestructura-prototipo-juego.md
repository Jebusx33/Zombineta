# Reestructura prototipo / juego — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repo con `Prototipo/`, `Juego/` y la simulación como paquete local compartido, sin
cambiar el comportamiento del prototipo.

**Architecture:** Movimiento con `git mv` (pares `.cs` + `.meta`) con Unity cerrado; paquete UPM
local referenciado por `file:` desde los dos manifests; `Juego/` clonado de la configuración
del prototipo.

**Tech Stack:** Unity 6000.6.0f1 (`D:/Dev/Unity/6000.6.0f1/Editor/Unity.exe`), git, MCP de Unity.

Spec: `docs/superpowers/specs/2026-09-13-reestructura-prototipo-juego-design.md`.

## Global Constraints

- Nada de cambios de código salvo asmdefs, `package.json` y manifests.
- Cada `.cs` se mueve con su `.meta`.
- Commits terminan con `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`.
- Punto de retorno: tag `prototipo-v1`.

---

### Task 1: Cerrar Unity y mover el proyecto a `Prototipo/`

- [ ] Guardar escena y assets (`EditorSceneManager.SaveOpenScenes`, `AssetDatabase.SaveAssets`) y
  cerrar Unity (`EditorApplication.Exit(0)` por MCP).
- [ ] `git mv Assets Packages ProjectSettings Tools Prototipo/`.
- [ ] Mover también (sin git, están ignoradas) `Library`, `Logs`, `UserSettings`, `Temp` a
  `Prototipo/` para no reimportar todo; borrar los `.csproj`/`.slnx` generados en la raíz.
- [ ] `.gitignore`: quitar el `/` inicial de `[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild/`,
  `[Bb]uilds/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `[Mm]emoryCaptures/`, `[Rr]ecordings/` para
  que apliquen a los dos proyectos.

### Task 2: Extraer la simulación al paquete

- [ ] Crear `Paquetes/com.zombineta.simulacion/package.json`:

```json
{
  "name": "com.zombineta.simulacion",
  "version": "0.1.0",
  "displayName": "Zombineta - Simulacion",
  "description": "Reglas del juego en C# plano: moto, horda, salto, recorrido, flujo de pantallas y lenguaje de camara. Sin MonoBehaviour.",
  "unity": "6000.6"
}
```

- [ ] `git mv` de los 18 archivos de la tabla del spec (con `.meta`) a
  `Paquetes/com.zombineta.simulacion/Runtime/<Carpeta>/`.
- [ ] `Runtime/Zombineta.Simulacion.asmdef`:

```json
{
    "name": "Zombineta.Simulacion",
    "rootNamespace": "Zombineta",
    "references": [],
    "autoReferenced": true,
    "noEngineReferences": false
}
```

- [ ] `git mv Prototipo/Assets/_Zombineta/Tests/EditMode/*` a
  `Paquetes/com.zombineta.simulacion/Tests/Editor/` y renombrar su asmdef a
  `Zombineta.Simulacion.Tests.asmdef` con `"name": "Zombineta.Simulacion.Tests"` y
  `"references": ["Zombineta.Simulacion", "UnityEngine.TestRunner", "UnityEditor.TestRunner"]`.
- [ ] `Prototipo/Assets/_Zombineta/Scripts/Zombineta.asmdef`: agregar `"Zombineta.Simulacion"`.
- [ ] `Prototipo/Packages/manifest.json`: agregar
  `"com.zombineta.simulacion": "file:../../Paquetes/com.zombineta.simulacion"` y
  `"testables": ["com.zombineta.simulacion"]`.

### Task 3: Verificar el prototipo

- [ ] Abrir Unity en `Prototipo/` (`Unity.exe -projectPath`), esperar la importación y la
  conexión del MCP (si pide aprobación, avisar al usuario).
- [ ] Consola sin `error CS`; 125 tests en verde.
- [ ] Recorrer los assets de `Settings/` buscando scripts faltantes.
- [ ] Play: arrancar un nivel, 3 s de partida, horda viva, un disparo que mata. Sin errores.
- [ ] Commit: `refactor: prototipo a Prototipo/ y simulacion como paquete compartido`.

### Task 4: Crear `Juego/`

- [ ] Cerrar Unity. Copiar a `Juego/`: `Packages/manifest.json`, `ProjectSettings/` y
  `Assets/Settings/` (con `.meta`) desde `Prototipo/`.
- [ ] Abrir Unity en `Juego/`, crear `Assets/_Zombineta/Scenes/Boot.unity` vacía (cámara
  ortográfica y `Global Light 2D`), registrarla como única escena en Build Settings.
- [ ] 125 tests en verde también acá.
- [ ] Commit: `feat: proyecto Juego con la simulacion compartida y escena Boot`.

### Task 5: Documentación y subida

- [ ] `README.md` y `HANDOFF.md`: estructura nueva, cómo abrir cada proyecto, que el prototipo
  está congelado para E2, que las ramas del equipo se recrean desde `master`, y la sugerencia
  de clonar el repo en una ruta limpia.
- [ ] Commit, push a `Jose`, merge a `master`.
- [ ] Reabrir Unity en `Prototipo/` para dejar al usuario donde estaba.
