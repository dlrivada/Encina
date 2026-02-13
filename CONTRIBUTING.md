# Contributing to Encina

Gracias por contribuir. Este proyecto prioriza código limpio, rail funcional (sin excepciones operativas) y calidad automatizada.

## Requisitos antes de abrir PR

- Sigue Conventional Commits en el título del PR (`type(scope?): subject`) o justifica la excepción.
- Ejecuta `dotnet format Encina.slnx --verify-no-changes`.
- Ejecuta `dotnet test Encina.slnx --configuration Release`.
- Si cambias comportamiento público o métricas, añade/ajusta tests.
- Mantén la política de Zero Exceptions: errores operativos deben viajar como `Either<EncinaError, TValue>`.
- No silencies warnings sin justificación documentada; `TreatWarningsAsErrors` está activo.
- Si añades behaviors/pipelines, asegúrate de devolver `ValueTask<Either<EncinaError,T>>` y evitar `throw` salvo cancelaciones.
- Actualiza README/roadmap/badges cuando afecte a capacidades, cobertura o calidad.

## Checklist rápida

- [ ] Formato y analizadores pasan localmente.
- [ ] Tests en Release pasan.
- [ ] Cobertura no cae bajo el umbral CI (90% líneas) y, si cambia, actualiza badge.
- [ ] Cambios en behaviors/pipelines siguen el rail funcional; sin excepciones operativas.
- [ ] Documentación/badges actualizados si aplica.

## Flujo de CI

- `ci.yml` aplica formato, analizadores en `-warnaserror`, tests con cobertura y gate 90%.
- `conventional-commits.yml` valida títulos de PR.
- `codeql.yml`, `sbom.yml`, `benchmarks.yml` cubren seguridad, cadena de suministro y performance.

## Estilo y API

- Usa `ValueTask` en el camino crítico para minimizar asignaciones.
- Prefiere `EncinaError`/`Either` en lugar de excepciones para flujos esperados.
- Documenta códigos de error y mantén consistencia (`EncinaErrorCodes` cuando se añadan).
- Considera namespaces de archivo y guard clauses reutilizables.

## Tests y ejemplos

- Añade pruebas de contrato cuando se incorporen nuevos behaviors o refactors del core.
- Si tocas métricas/diagnósticos, añade asserts sobre contadores/spans.
- Para nuevas capacidades, incluye snippets mínimos en README o docs.
