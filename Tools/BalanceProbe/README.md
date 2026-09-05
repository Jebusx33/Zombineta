# Balance Probe

Corre la simulación **real** del juego (`Assets/_Zombineta/Scripts/Core/RunSimulation.cs`) y
su suite de tests fuera de Unity, contra unos shims mínimos de `Mathf` y NUnit.

Sirve para dos cosas:

1. **Verificar las reglas** sin abrir el editor ni entrar en Play Mode (21 tests, ~3 s).
2. **Balancear (T23)** simulando partidas enteras con distintas configuraciones y
   distintos estilos de juego, en vez de jugar 5 minutos por cada ajuste.

## Cómo correrlo

```bash
cd Tools/BalanceProbe && dotnet run
```

## Cómo mantenerlo

`Shims.cs`, `Runner.cs` y `BalanceProbe.cs` son propios de la herramienta. Los archivos del
juego (`GameConfig.cs`, `RunState.cs`, `RunSimulation.cs`, `PlayerIntent.cs`) y el de tests
(`RunSimulationTests.cs`) se **copian** desde `Assets/` antes de compilar:

```bash
cp ../../Assets/_Zombineta/Scripts/Core/*.cs ../../Assets/_Zombineta/Scripts/Player/PlayerIntent.cs .
cp ../../Assets/_Zombineta/Tests/EditMode/RunSimulationTests.cs .
```

No copiar `RunController.cs` ni `CameraFollow.cs`: son MonoBehaviours y no compilan fuera de Unity.

> Los shims son la verificación rápida, no la oficial. La verificación que vale es el
> Unity Test Runner en EditMode, que corre exactamente los mismos tests contra el Unity de verdad.

## Estilos de juego simulados

- **Adaptativa** — usa turbo cuando la horda aprieta, el faro casi nunca. Representa a
  alguien que no entendió la economía de recursos.
- **Faro-first** — usa el faro siempre que haya batería, porque frena a la horda sin gastar
  una gota de nafta, y reserva el turbo para emergencias.

Que la faro-first llegue y la adaptativa no es la señal de que las mecánicas tienen
profundidad: hay una forma correcta de jugar y se premia.

## Balance vigente (05/09/2026)

Con `GameConfig.asset` en sus valores actuales y bidones cada 200 m, batería cada 500 m
y munición cada 400 m:

| Estilo | Resultado | Tiempo | Gap mínimo |
|---|---|---|---|
| Faro-first | llega (100%) | 306 s | 25 m |
| Adaptativa | muere sin nafta (55%) | 133 s | 0 m |

Pendiente de ajustar: la faro-first llega con el tanque lleno, así que en el último tramo
sobran bidones. Espaciarlos más hacia el final del recorrido.
