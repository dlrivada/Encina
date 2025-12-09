# SimpleMediator Quality & Security Roadmap

## Vision

- Garantizar que SimpleMediator ofrezca una experiencia de mediación confiable, observable y segura para integradores .NET.
- Asegurar que cada cambio de código se someta a verificaciones automáticas cuantificables de calidad, rendimiento y seguridad.
- Mantener un flujo de lanzamiento predecible, documentado y compatible con la cadena de suministro moderna.

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

### Mejora continua del core de SimpleMediator

- Refactorizar `SimpleMediator.Send` y `Publish` para delegar la orquestación en helpers internos dedicados, reduciendo complejidad ciclomática y mejorando la testabilidad del flujo principal.
- Extraer guard clauses reutilizables (`EnsureRequest`, `EnsureNextStep`, etc.) que encapsulen la creación de errores estándar (`mediator.behavior.null_*`).
- Adoptar namespaces con ámbito de archivo en todo `src/SimpleMediator` para incrementar la legibilidad y coherencia de estilo.
- Evolucionar `RequestHandlerCallback<T>` y las implementaciones de comportamiento a `ValueTask<Either<MediatorError,T>>`, evitando asignaciones innecesarias cuando los pasos se completan de forma sincrónica.
- Replantear las cachés (`RequestHandlerCache`, `NotificationHandlerInvokerCache`) para materializar funciones listas para ejecutar que eviten reflection y boxing en el camino crítico.
- Introducir un `PipelineBuilder<TRequest,TResponse>` que construya una sola vez la cadena de behaviors/pre/post processors y devuelva un delegado compilado reutilizable.
- Definir una envoltura `MediatorResult<T>` para expresar el resultado de forma más legible que los `Either.Match` dispersos, manteniendo compatibilidad con la política de cero excepciones.
- Centralizar los códigos de error en `MediatorErrorCodes` (constantes o enum) para prevenir incoherencias y facilitar documentación.
- Encapsular la instrumentación (ActivitySource, logging) en un `MediatorDiagnostics` ampliado con métodos `SendStarted/Completed`, de manera que la capa de orquestación sólo delegue datos sin mezclar responsabilidades.
- Sustituir `object? Details` en `MediatorException` por un contenedor inmutable (p. ej. `ImmutableDictionary<string, object?>`) que permita consultas seguras y facilite la serialización de metadatos.
- Completar la documentación XML del API público y habilitar analizadores de API pública para reforzar compatibilidad binaria en futuras versiones.
- Considerar `CollectionsMarshal.AsSpan` y otros helpers de BCL moderna para iteraciones de alto rendimiento sobre colecciones resueltas desde DI.

### Inmediato (0-2 semanas)

- Configurar workflow `dotnet-ci.yml` con pasos: `dotnet format --verify-no-changes`, analizadores Roslyn (StyleCop.Analyzers + Microsoft.CodeAnalysis.FxCopAnalyzers) y `dotnet test` con cobertura.
- Formalizar scripts existentes (`scripts/run-benchmarks.cs`, `scripts/check-benchmarks.cs`) en un workflow `benchmarks.yml` manual y nocturno con upload de artefactos.
- Activar Dependabot para NuGet y GitHub Actions con revisión semanal.
- Documentar en `README.md` los comandos clave y añadir los badges de CI, CodeQL, Dependabot y licencia.
- Aplicar namespaces con ámbito de archivo y helpers de guard clauses en `src/SimpleMediator` para establecer la base del refactor estructural.
- Auditar `SimpleMediator.Send/Publish` e identificar responsabilidades a mover hacia un `RequestDispatcher`/`NotificationDispatcher` interno.

### Corto Plazo (2-6 semanas)

- Añadir `Directory.Build.props` con reglas de estilo unificadas y severidad de analizadores en Warning-as-Error.
- Implementar lint de Conventional Commits (por ejemplo, `amannn/action-semantic-pull-request`).
- Integrar cobertura con Coverlet + ReportGenerator; publicar badge SVG en `badges/dotnet-coverage.svg`.
- Añadir pruebas de regresión para escenarios de notificaciones múltiples y manejo de fallos en pipelines.
- Publicar SBOM automatizada (Syft, `dotnet sbom`) en workflow `sbom.yml` y adjuntar en releases.
- Consolidar `RequestHandlerCallback<T>` y behaviors en `ValueTask<Either<MediatorError,T>>` y validar el impacto en benchmarks.
- Implementar `PipelineBuilder<TRequest,TResponse>` para construir la cadena de behaviors y reducir complejidad en `SendCore`.
- Introducir `MediatorErrorCodes` y actualizar referencias a códigos literales.

### Medio Plazo (6-12 semanas)

- Introducir análisis de terceros: CodeFactor o SonarCloud para deuda técnica y mantenibilidad.
- Añadir pruebas de carga ligeras con BenchmarkDotNet en modo `--runContinuously` y alertas si se superan umbrales.
- Implementar política de ramas protegidas: revisores obligatorios, status checks obligatorios y firmas opcionales de commits/tag (GPG).
- Instrumentar `MediatorMetrics` con validaciones de telemetría en pruebas (asegurar que métricas se registran y exponen).
- Crear `CONTRIBUTING.md` y checklist de PR con pasos de validación (tests, benchmarks, SBOM, cobertura, revisión de dependencias).
- Optimizar las cachés de delegados (request/notification) para evitar reflection en tiempo de ejecución y aprovechar `CollectionsMarshal.AsSpan` donde aplique.
- Encapsular la instrumentación en `MediatorDiagnostics` y agregar pruebas que validen eventos/activities generados.
- Sustituir `object? Details` por contenedores inmutables seriables y documentar el contrato de metadatos expuestos.

### Largo Plazo (> 12 semanas)

- Adoptar framework de threat modeling ligero para nuevos features (STRIDE o equivalente) documentado en RFCs.
- Publicar entregables firmados y automatizar release notes (GitHub Release Drafter) con changelog seccionado por tipo de cambio.
- Revisar opciones de firma de paquetes NuGet (Authenticode o Sigstore) y publicación automatizada condicionada a pipelines verdes.
- Explorar certificaciones de seguridad de la cadena de suministro (SLSA nivel 2) generando provenance statements con GitHub OIDC + cosign.
- Evaluar `MediatorResult<T>` como reemplazo de `Either` expuesto y, si se adopta, documentar la transición y ruptura mínima en el API.
- Completar la adopción de analizadores de API pública y establecer baseline de contratos versionados.

## Próximos Pasos Operativos

- [ ] Crear workflows `dotnet-ci.yml`, `codeql.yml`, `sbom.yml`, `benchmarks.yml` con checks obligatorios.
- [ ] Añadir `Directory.Build.props`/`Directory.Build.targets` con reglas de analizadores y `TreatWarningsAsErrors`.
- [ ] Integrar Coverlet + ReportGenerator y publicar badge de cobertura.
- [ ] Incorporar Conventional Commits lint y actualizar plantilla de PR.
- [ ] Redactar `CONTRIBUTING.md` con requisitos de calidad y guía de colabora.
- [ ] Aplicar refactor inicial del core (`Send`, `Publish`, guard clauses, namespaces de archivo) según la sección "Mejora continua".
- [ ] Diseñar y prototipar `PipelineBuilder<TRequest,TResponse>` + migración a `ValueTask`.
- [ ] Definir `MediatorErrorCodes`, estrategia de metadatos inmutables y plan de pruebas para la nueva instrumentación.

## Seguimiento y Revisión

- Revisar métricas mensualmente y actualizar objetivos según evolución del producto.
- Utilizar GitHub Projects o Issues etiquetados (`quality`, `security`) para rastrear iniciativas del roadmap.
- Auditar workflows trimestralmente para asegurar dependencias actualizadas y permisos mínimos (principio de menor privilegio).
