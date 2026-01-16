# Plan de Consolidacion de Tests

> **Estado**: Completado
> **Inicio**: 2026-01-15
> **Ultima actualizacion**: 2026-01-16

## Objetivo

Consolidar ~210 proyectos de test individuales en 7 proyectos consolidados + TestInfrastructure.

## Estructura Final

```text
tests/
├── Encina.UnitTests/           # 65 → 1 (~4,600 tests)
├── Encina.IntegrationTests/    # 31 → 1 (~710 tests)
├── Encina.PropertyTests/       # 30 → 1 (~485 tests)
├── Encina.ContractTests/       # 28 → 1 (~400 tests)
├── Encina.GuardTests/          # 20 → 1 (~300 tests)
├── Encina.LoadTests/           # Consolidado en tests/
├── Encina.NBomber/             # Consolidado en tests/
├── Encina.BenchmarkTests/      # Consolidado en tests/
│   ├── Encina.AspNetCore.Benchmarks/
│   ├── Encina.AwsLambda.Benchmarks/
│   ├── Encina.AzureFunctions.Benchmarks/
│   ├── Encina.Benchmarks/
│   ├── Encina.Caching.Benchmarks/
│   ├── Encina.DistributedLock.Benchmarks/
│   ├── Encina.EntityFrameworkCore.Benchmarks/
│   ├── Encina.Extensions.Resilience.Benchmarks/
│   ├── Encina.Polly.Benchmarks/
│   └── Encina.Refit.Benchmarks/
├── Encina.TestInfrastructure/  # Se mantiene
└── Encina.Testing.Examples/    # Ejemplos de referencia
```

## Progreso por Fase

### Fase 1: Unit Tests (*.Tests → Encina.UnitTests)

- **Estado**: ✅ Completado
- **Progreso**: ~4,580 tests migrados
- **Proyectos origen**: 65
- **Proyectos destino**: 1

### Fase 2: Integration Tests (*.IntegrationTests → Encina.IntegrationTests)

- **Estado**: ✅ Completado
- **Progreso**: ~70+ archivos migrados
- **Proyectos origen**: 31
- **Proyectos destino**: 1

### Fase 3: Property Tests (*.PropertyTests → Encina.PropertyTests)

- **Estado**: ✅ Completado
- **Proyectos origen**: 30
- **Proyectos destino**: 1

### Fase 4: Contract Tests (*.ContractTests → Encina.ContractTests)

- **Estado**: ✅ Completado
- **Proyectos origen**: 28
- **Proyectos destino**: 1

### Fase 5: Guard Tests (*.GuardTests → Encina.GuardTests)

- **Estado**: ✅ Completado
- **Proyectos origen**: 20
- **Proyectos destino**: 1

### Fase 6: Load Tests (load/* → tests/)

- **Estado**: ✅ Completado
- **Proyectos movidos**: 2 (Encina.LoadTests, Encina.NBomber)
- **Ubicación anterior**: `load/`
- **Ubicación final**: `tests/Encina.LoadTests/`, `tests/Encina.NBomber/`

### Fase 7: Benchmark Tests (benchmarks/* → tests/Encina.BenchmarkTests/)

- **Estado**: ✅ Completado
- **Proyectos movidos**: 10
- **Ubicación anterior**: `benchmarks/`
- **Ubicación final**: `tests/Encina.BenchmarkTests/`

### Fase 8: Reparar Workflows CI/CD

- **Estado**: ✅ Completado

| Workflow | Cambios | Estado |
|----------|---------|--------|
| `ci.yml` | Actualizar rutas a proyectos consolidados | ✅ |
| `dotnet-ci.yml` | Actualizar paths de tests | ✅ |
| Otros workflows | No requieren cambios (usan scripts) | ✅ |

### Fase 9: Limpieza de Proyectos Antiguos

- **Estado**: ✅ Completado
- **Proyectos eliminados**: ~195

| Tipo | Cantidad | Estado |
|------|----------|--------|
| `tests/*.Tests/` | 65 | ✅ Eliminados |
| `tests/*.IntegrationTests/` | 31 | ✅ Eliminados |
| `tests/*.PropertyTests/` | 30 | ✅ Eliminados |
| `tests/*.ContractTests/` | 28 | ✅ Eliminados |
| `tests/*.GuardTests/` | 20 | ✅ Eliminados |
| `tests/*.LoadTests/` | 21 | ✅ Eliminados |

### Fase 10: Limpieza de Codigo Muerto

- **Estado**: ✅ Completado

| Tarea | Estado |
|-------|--------|
| Actualizar `Encina.slnx` (eliminar referencias) | ✅ |
| Eliminar archivos temporales (`tmpclaude-*`) | ✅ |
| Actualizar benchmark dependencies | ✅ |

### Fase 11: Validacion Final

- **Estado**: ✅ Completado

| Tarea | Estado |
|-------|--------|
| Build `Encina.slnx` completo | ✅ 0 errores |
| Compilar `Encina.UnitTests` | ✅ |
| Compilar `Encina.IntegrationTests` | ✅ |
| Compilar `Encina.PropertyTests` | ✅ |
| Compilar `Encina.ContractTests` | ✅ |
| Compilar `Encina.GuardTests` | ✅ |
| Compilar benchmarks | ✅ |

---

## Resumen de Cambios

### Archivos Modificados

- `Encina.slnx` - Actualizado para usar solo proyectos consolidados
- `.github/workflows/ci.yml` - Actualizado para usar proyectos consolidados
- `.github/workflows/dotnet-ci.yml` - Actualizado para usar proyectos consolidados
- `tests/Encina.BenchmarkTests/*/*.csproj` - Actualizadas referencias relativas
- `tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks/AuthorizationPipelineBehaviorBenchmarks.cs` - Actualizado namespace
- `scripts/run-benchmarks.cs` - Actualizada ruta a proyecto de benchmarks
- `scripts/run-load-harness.cs` - Actualizadas rutas a proyectos de load tests
- `.github/workflows/sonarcloud.yml` - Eliminadas exclusiones obsoletas de benchmarks/load
- `src/Encina.AspNetCore/Properties/AssemblyInfo.cs` - Agregado InternalsVisibleTo para benchmarks

### Proyectos Consolidados (Estructura Final)

```text
tests/
├── Encina.UnitTests/
│   ├── ADO/
│   ├── AspNetCore/
│   ├── Caching/
│   ├── Cli/
│   ├── Core/
│   ├── Dapper/
│   ├── DomainModeling/
│   ├── EntityFrameworkCore/
│   ├── Messaging/
│   ├── Testing/
│   └── ...
├── Encina.IntegrationTests/
│   ├── ADO/
│   ├── Dapper/
│   ├── Caching/
│   └── ...
├── Encina.PropertyTests/
│   ├── ADO/
│   ├── Dapper/
│   └── ...
├── Encina.ContractTests/
│   ├── ADO/
│   ├── Dapper/
│   └── ...
├── Encina.GuardTests/
│   ├── ADO/
│   ├── Dapper/
│   ├── Infrastructure/
│   └── ...
├── Encina.LoadTests/
├── Encina.NBomber/
├── Encina.BenchmarkTests/
│   ├── Encina.AspNetCore.Benchmarks/
│   ├── Encina.Benchmarks/
│   └── ...
├── Encina.TestInfrastructure/
└── Encina.Testing.Examples/
```

---

## Historial de Sesiones

| Fecha | Sesion | Trabajo realizado |
|-------|--------|-------------------|
| 2026-01-15 | 1 | Plan inicial, migracion parcial de Unit Tests |
| 2026-01-16 | 2 | Completadas Fases 1-4 (UnitTests, IntegrationTests, PropertyTests, ContractTests). Fase 5 parcial (proyecto GuardTests base creado). Fases 6-7 verificadas (load/benchmarks ya consolidados). Proyectos añadidos a solucion. |
| 2026-01-16 | 3 | Completadas Fases 5-11. Corregidos errores de compilacion en GuardTests. Actualizados workflows CI/CD. Eliminados ~195 proyectos de test antiguos. Actualizada solucion. Limpieza de archivos temporales. Build completo exitoso. |
| 2026-01-16 | 4 | Movidos LoadTests y Benchmarks a tests/. Actualizados scripts (run-benchmarks.cs, run-load-harness.cs). Actualizadas documentacion (CLAUDE.md, guias de testing). Eliminadas carpetas load/ y benchmarks/ antiguas. |
