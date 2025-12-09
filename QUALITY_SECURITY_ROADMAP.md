# SimpleMediator Quality & Security Roadmap

## Vision

- Garantizar que SimpleMediator ofrezca una experiencia de mediación confiable, observable y segura para integradores .NET.
- Asegurar que cada cambio de código se someta a verificaciones automáticas cuantificables de calidad, rendimiento y seguridad.
- Mantener un flujo de lanzamiento predecible, documentado y compatible con la cadena de suministro moderna.

## Principios de calidad

- No se silencian advertencias salvo justificación documentada; se corrigen primero. `TreatWarningsAsErrors=true` ya está activado para todo el árbol.
- Política de Zero Exceptions en el plano de dominio/orquestación: los fallos viajan por el rail funcional (`Either<MediatorError, TValue>`) en lugar de propagarse como excepciones.

## Métricas Objetivo

- **Calidad:** 0 advertencias en analizadores (Roslyn + StyleCop) y ≥ 95 % de cobertura de ramas en paquetes clave.
- **Rendimiento:** mantener los benchmarks `Send_Command` y `Publish_Notification` ≤ 15 % por encima del baseline registrado.
- **Seguridad:** 0 dependencias con CVSS ≥ 7 abiertas > 7 días; SBOM actualizado en cada release.
- **Gobernanza:** flujo de commits alineado con Conventional Commits y release notes generados automáticamente.

## Badges Recomendados para README

| Categoría | Badge | Acción requerida |
| --- | --- | --- |
| CI Calidad | `[![.NET](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml)` | Crear workflow con `dotnet format`, analizadores Roslyn y pruebas. |
| Seguridad | `[![CodeQL](https://github.com/dlrivada/SimpleMediator/actions/workflows/codeql.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/codeql.yml)` | Activar escaneo CodeQL. |
| Cadena suministro | `[![SBOM](https://github.com/dlrivada/SimpleMediator/actions/workflows/sbom.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/sbom.yml)` | Generar SBOM (Syft o dotnet CLI) en releases. |
| Dependencias | `[![Dependabot](https://img.shields.io/badge/Dependabot-Enabled-025E8C?logo=dependabot&logoColor=white)](https://docs.github.com/code-security/dependabot)` | Habilitar Dependabot para NuGet y GitHub Actions. |
| Cobertura | `[![.NET Coverage](./badges/dotnet-coverage.svg)](./badges/dotnet-coverage.svg)` | Capturar cobertura con Coverlet + ReportGenerator; publicar badge. |
| Commits | `[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-FE5196)](https://www.conventionalcommits.org/)` | Enforce mediante lint de commits. |
| Licencia | `[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)` | Confirmar licencia en repo. |
| Benchmarks | `[![Benchmarks](https://img.shields.io/badge/Benchmarks-Tracked-blue)](./artifacts/performance/README.md)` | Publicar reporte resumido tras cada ejecución. |

## Roadmap por Horizonte

### Progreso reciente

- [x] Integrado `PipelineBuilder<TRequest,TResponse>` y refactorizado `Send` para ejecutar behaviors/pre/post processors sin reflection en el camino crítico; suites de tests (unit, contract, property) en verde.
- [x] `SimpleMediatorTests` ahora se ejecuta en colección xUnit no paralela para evitar interferencias de `ActivitySource` entre casos; suites completas en verde (212/212 tests).
- [x] Eliminada la duplicidad de `RequestHandlerCallback` y alineado el rail funcional (`ValueTask<Either<MediatorError,TResponse>>`) en behaviors y núcleo.
- [x] Activado `TreatWarningsAsErrors` en `Directory.Build.props` y corregido CA1859 en `PipelineBehaviorContracts` (release `dotnet test SimpleMediator.slnx` pasa con 198/198 tests).
- [x] Workflow `dotnet-ci.yml` operativo: checkout, format, build con analizadores en `-warnaserror`, tests con cobertura + ReportGenerator y opción de benchmarks.
- [x] Cobertura generada con Coverlet + ReportGenerator (`Line coverage: 92.9%`); badge publicado en `badges/dotnet-coverage.svg` y enlazado en README.
- [x] Añadido lint de Conventional Commits vía `amannn/action-semantic-pull-request` (workflow `conventional-commits.yml`) y plantilla de PR para reforzar checklist de calidad y rail funcional.
- [x] Gate de cobertura en CI: ReportGenerator `JsonSummary` + paso que falla si la cobertura de líneas cae por debajo del 90%.
- [x] Umbral de cobertura ahora parametrizable vía `COVERAGE_THRESHOLD` (env/vars) en `dotnet-ci.yml` para ajustes controlados sin tocar el workflow.
- [x] Borrador de `MediatorErrorCodes` para centralizar códigos y preparar sustitución de literales.
- [x] Interface `IPipelineBuilder<TRequest,TResponse>` añadida como esqueleto para el refactor de pipeline sin reflejos ni asignaciones extras.
- [x] Behaviors y core actualizados para usar `MediatorErrorCodes` en lugar de literales.
- [x] Nueva guía de contribución: `CONTRIBUTING.md` recoge rail funcional, zero exceptions, formato, tests, cobertura y convenciones de PR.
- [x] Telemetría enriquecida: `Send` ahora etiqueta `mediator.request_name`, `mediator.response_type`, `mediator.handler` y `mediator.handler_count`; `Publish` etiqueta `mediator.notification_name`, `mediator.notification_kind`, `mediator.handler_count` y `mediator.failure_reason`, con pruebas de regresión que cubren éxito, cancelaciones y fallos en `SimpleMediatorTests`.
- [x] Listeners de actividad en tests filtrados y colección xUnit dedicada (`PipelineBehaviors`) para evitar fugas de actividades entre pruebas; suites completas en verde (207/207 tests).

### Mejora continua del core de SimpleMediator

- [ ] Refactorizar `SimpleMediator.Publish` para delegar validaciones/guards en helpers internos (parcialmente: `Send` ya usa `PipelineBuilder` y se añadieron guards de notificación y behavior).
- [ ] Extraer guard clauses reutilizables (`EnsureRequest`, `EnsureNextStep`, etc.) que encapsulen la creación de errores estándar (`mediator.behavior.null_*`).
- [ ] Adoptar namespaces con ámbito de archivo en todo `src/SimpleMediator` para incrementar la legibilidad y coherencia de estilo.
 [x] Evolucionar `RequestHandlerCallback<T>` y las implementaciones de comportamiento a `ValueTask<Either<MediatorError,T>>`, evitando asignaciones innecesarias cuando los pasos se completan de forma sincrónica.
 [ ] Replantear las cachés (`RequestHandlerCache`, `NotificationHandlerInvokerCache`) para materializar funciones listas para ejecutar que eviten reflection y boxing en el camino crítico.
 [x] Introducir un `PipelineBuilder<TRequest,TResponse>` que construya una sola vez la cadena de behaviors/pre/post processors y devuelva un delegado compilado reutilizable.
 [ ] Definir una envoltura `MediatorResult<T>` para expresar el resultado de forma más legible que los `Either.Match` dispersos, manteniendo compatibilidad con la política de cero excepciones.
 [x] Centralizar los códigos de error en `MediatorErrorCodes` (constantes o enum) para prevenir incoherencias y facilitar documentación.
 [x] Encapsular la instrumentación (ActivitySource, logging) en un `MediatorDiagnostics` ampliado con métodos `SendStarted/Completed`, de manera que la capa de orquestación sólo delegue datos sin mezclar responsabilidades.
- [ ] Sustituir `object? Details` en `MediatorException` por un contenedor inmutable (p. ej. `ImmutableDictionary<string, object?>`) que permita consultas seguras y facilite la serialización de metadatos.
- [ ] Completar la documentación XML del API público y habilitar analizadores de API pública para reforzar compatibilidad binaria en futuras versiones.
- [ ] Considerar `CollectionsMarshal.AsSpan` y otros helpers de BCL moderna para iteraciones de alto rendimiento sobre colecciones resueltas desde DI.

### Fase 1 (próximo sprint)

- [x] Extraer `RequestDispatcher` paralelo al de notificaciones, reutilizando guard clauses y afinando cachés (RequestHandler/NotificationInvoker) para minimizar reflection/boxing; evaluar `CollectionsMarshal.AsSpan` donde aplique.
- [x] Exponer eventos `SendStarted/Completed` en diagnósticos/métricas y añadir pruebas que aserten tags/activities y métricas emitidas.
- [ ] Habilitar generación de XML docs + analizadores de API pública; preparar check de CI que falle ante cambios breaking no documentados y completar docstrings faltantes.
- [ ] Agregar escaneo de calidad (SonarCloud o CodeFactor en modo read-only) y plantilla de protección de ramas (revisores + checks obligatorios); evaluar gate de dependencias de bajo riesgo con auto-merge tras CI verde.

### Fase 2

- [ ] Introducir `MediatorResult<T>` como envoltura legible sobre `Either`, con adaptadores de compatibilidad y tests de regresión (éxito/fallo + metadata).
- [ ] Ampliar pruebas de regresión de notificaciones (múltiples handlers, fallos parciales) y cobertura de cachés y métricas.
- [ ] Automatizar SBOM continua y revisar permisos mínimos en workflows; considerar auto-merge de Dependabot en dev-deps tras CI verde.
- [ ] Afinar benchmarks y presupuesto de rendimiento tras los cambios de dispatcher/cachés; publicar reporte en `artifacts/performance/`.

### Fase 3

- [ ] Introducir análisis de terceros: CodeFactor o SonarCloud para deuda técnica y mantenibilidad.
- [ ] Añadir pruebas de carga ligeras con BenchmarkDotNet en modo `--runContinuously` y alertas si se superan umbrales.
- [ ] Implementar política de ramas protegidas: revisores obligatorios, status checks obligatorios y firmas opcionales de commits/tag (GPG).
- [ ] Instrumentar `MediatorMetrics` con validaciones de telemetría en pruebas (asegurar que métricas se registran y exponen).
- [ ] Crear `CONTRIBUTING.md` y checklist de PR con pasos de validación (tests, benchmarks, SBOM, cobertura, revisión de dependencias).
- [ ] Optimizar las cachés de delegados (request/notification) para evitar reflection en tiempo de ejecución y aprovechar `CollectionsMarshal.AsSpan` donde aplique.
- [ ] Encapsular la instrumentación en `MediatorDiagnostics` y agregar pruebas que validen eventos/activities generados.
- [ ] Sustituir `object? Details` por contenedores inmutables seriables y documentar el contrato de metadatos expuestos.

### Fase 4

- [ ] Adoptar framework de threat modeling ligero para nuevos features (STRIDE o equivalente) documentado en RFCs.
- [ ] Publicar entregables firmados y automatizar release notes (GitHub Release Drafter) con changelog seccionado por tipo de cambio.
- [ ] Revisar opciones de firma de paquetes NuGet (Authenticode o Sigstore) y publicación automatizada condicionada a pipelines verdes.
- [ ] Explorar certificaciones de seguridad de la cadena de suministro (SLSA nivel 2) generando provenance statements con GitHub OIDC + cosign.
- [ ] Evaluar `MediatorResult<T>` como reemplazo de `Either` expuesto y, si se adopta, documentar la transición y ruptura mínima en el API.
- [ ] Completar la adopción de analizadores de API pública y establecer baseline de contratos versionados.

## Próximos Pasos Operativos

- [x] Crear workflows `dotnet-ci.yml`, `codeql.yml`, `sbom.yml`, `benchmarks.yml` con checks obligatorios.
- [x] Añadir `Directory.Build.props`/`Directory.Build.targets` con reglas de analizadores y `TreatWarningsAsErrors`.
- [x] Integrar Coverlet + ReportGenerator y publicar badge de cobertura.
- [x] Incorporar Conventional Commits lint y actualizar plantilla de PR.
  - [x] Workflow `conventional-commits.yml` con `amannn/action-semantic-pull-request`.
  - [x] Plantilla de PR con checklist de formato, tests, rail funcional y cobertura.
- [x] Añadir gate de cobertura en CI (umbral 90% líneas, basado en ReportGenerator JsonSummary).
- [x] Redactar `CONTRIBUTING.md` con requisitos de calidad y guía de colabora.
- [x] Parametrizar umbral de cobertura (variable `COVERAGE_THRESHOLD` con default 90%).
- [x] Esqueleto de `MediatorErrorCodes` creado (pendiente de adopción progresiva).
- [x] Esqueleto de `IPipelineBuilder<TRequest,TResponse>` creado para futura composición de pipeline.
- [x] Reemplazo inicial de literales por `MediatorErrorCodes` en core y behaviors.
- [x] Aplicar refactor inicial del core (`Publish`, guard clauses, namespaces de archivo) según la sección "Mejora continua". (Estado: `Send` ya usa `PipelineBuilder` y rail funcional consolidado; guard clauses de behaviors usan `MediatorErrorCodes`; `Publish` delega en `NotificationDispatcher` con metadatos y namespaces de archivo aplicados.)
- [x] Diseñar y prototipar `PipelineBuilder<TRequest,TResponse>` + migración a `ValueTask`.
- [x] Definir `MediatorErrorCodes`, estrategia de metadatos inmutables y plan de pruebas para la nueva instrumentación (metadatos adjuntan handler/request/stage en fallos; tests cubren códigos y metadatos extraíbles).

## Seguimiento y Revisión

- Revisar métricas mensualmente y actualizar objetivos según evolución del producto.
- Utilizar GitHub Projects o Issues etiquetados (`quality`, `security`) para rastrear iniciativas del roadmap.
- Auditar workflows trimestralmente para asegurar dependencias actualizadas y permisos mínimos (principio de menor privilegio).
- Posponer la activación de PublicApiAnalyzers/baseline; retomarlo al final para evitar bloquear avance.
