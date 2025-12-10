# SimpleMediator Quality & Security Roadmap

## Vision

- Garantizar que SimpleMediator ofrezca una experiencia de mediación confiable, observable y segura para integradores .NET.
- Asegurar que cada cambio de código se someta a verificaciones automáticas cuantificables de calidad, rendimiento y seguridad.
- Mantener un flujo de lanzamiento predecible, documentado y compatible con la cadena de suministro moderna.
- Alcanzar el más alto nivel de calidad en código, documentación, robustez y mantenibilidad para uso público.

## Principios de calidad

- No se silencian advertencias salvo justificación documentada; se corrigen primero. `TreatWarningsAsErrors=true` ya está activado para todo el árbol.
- Política de Zero Exceptions en el plano de dominio/orquestación: los fallos viajan por el rail funcional (`Either<MediatorError, TValue>`) en lugar de propagarse como excepciones.
- Documentación como ciudadano de primera clase: todo el API público debe estar documentado con XML comments y ejemplos donde sea relevante.
- Código autodescriptivo: nombres claros, responsabilidades únicas, y patrones funcionales consistentes.
- Testing exhaustivo: unit, integration, property-based, contract, mutation, y load testing para máxima confianza.

## Métricas Objetivo

- **Calidad:** 0 advertencias en analizadores (Roslyn + StyleCop + SonarCloud) y ≥ 95 % de cobertura de ramas en paquetes clave.
- **Documentación:** 100% de API público documentado con XML comments, guías completas para todos los escenarios de uso.
- **Rendimiento:** mantener los benchmarks `Send_Command` y `Publish_Notification` ≤ 15 % por encima del baseline registrado.
- **Seguridad:** 0 dependencias con CVSS ≥ 7 abiertas > 7 días; SBOM actualizado en cada release; SLSA Level 2 compliance.
- **Gobernanza:** flujo de commits alineado con Conventional Commits y release notes generados automáticamente.
- **Mutation Testing:** ≥ 95% mutation score para asegurar efectividad de tests.
- **Mantenibilidad:** Technical Debt Ratio ≤ 5% según SonarCloud.

## Badges Recomendados para README

| Categoría | Badge | Estado |
| --- | --- | --- |
| CI Calidad | `[![.NET](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/dotnet-ci.yml)` | ✅ Activo |
| Seguridad | `[![CodeQL](https://github.com/dlrivada/SimpleMediator/actions/workflows/codeql.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/codeql.yml)` | ✅ Activo |
| Cadena suministro | `[![SBOM](https://github.com/dlrivada/SimpleMediator/actions/workflows/sbom.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/sbom.yml)` | ✅ Activo |
| Dependencias | `[![Dependabot](https://img.shields.io/badge/Dependabot-Enabled-025E8C?logo=dependabot&logoColor=white)](https://docs.github.com/code-security/dependabot)` | ✅ Activo |
| Cobertura | `[![.NET Coverage](./badges/dotnet-coverage.svg)](./badges/dotnet-coverage.svg)` | ✅ 92.9% |
| Mutation | `![Mutation](https://img.shields.io/badge/mutation-93.74%25-4C934C.svg)` | ✅ 93.74% |
| Commits | `[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-FE5196)](https://www.conventionalcommits.org/)` | ✅ Activo |
| Benchmarks | `[![Benchmarks](https://github.com/dlrivada/SimpleMediator/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/dlrivada/SimpleMediator/actions/workflows/benchmarks.yml)` | ✅ Activo |
| SonarCloud | `[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=...)](https://sonarcloud.io/dashboard?id=...)` | ⏳ Pendiente |
| API Docs | `[![API Docs](https://img.shields.io/badge/docs-docfx-blue)](./docs/api/)` | ⏳ Pendiente |

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
- [x] PublicApiAnalyzers habilitado en .NET 10 con baseline completo (107 símbolos públicos documentados en `PublicAPI.Unshipped.txt`), configuración en `.editorconfig` para RS0016/RS0017/RS0022/RS0024/RS0025/RS0026/RS0027 como errores, y `#nullable enable` para tracking de anotaciones de nulabilidad; build y tests en verde (212/212).

### Fase 1: Fundamentos de Calidad Extrema (En progreso)

**Objetivo:** Elevar el código y documentación al nivel más alto para publicación pública.

#### 1.1 Documentación API Comprehensiva

- [ ] **Auditar XML comments:** Verificar que el 100% del API público tenga documentación XML completa con:
  - Resumen claro y conciso
  - Descripción de parámetros y returns
  - Ejemplos de uso donde sea relevante
  - Referencias cruzadas a tipos relacionados
  - Remarks para casos especiales o consideraciones de rendimiento
- [ ] **Documentar patrones internos:** Añadir comentarios explicativos en código complejo (RequestDispatcher, NotificationDispatcher, PipelineBuilder)
- [ ] **Generar documentación API con DocFX:**
  - Configurar DocFX en el proyecto
  - Generar sitio de documentación HTML
  - Publicar en GitHub Pages o sitio dedicado
  - Añadir badge al README
- [ ] **Crear guías de arquitectura:**
  - Architecture Decision Records (ADR) para decisiones clave
  - Diagrama de componentes y dependencias
  - Guía de patrones utilizados (ROP, DI, functional programming)

#### 1.2 Código y Estilo

- [ ] **Adoptar namespaces con ámbito de archivo:** Aplicar en todo `src/SimpleMediator` para coherencia y legibilidad
- [ ] **Extraer guard clauses reutilizables:** Crear `EnsureRequest`, `EnsureNextStep`, etc. que encapsulen la creación de errores estándar
- [ ] **Refactorizar `SimpleMediator.Publish`:** Delegar validaciones/guards en helpers internos (parcialmente completado con `Send`)
- [ ] **Optimizar cachés de delegados:** Minimizar reflection y boxing en `RequestHandlerCache` y `NotificationHandlerInvokerCache`
- [ ] **Considerar `CollectionsMarshal.AsSpan`:** Aplicar en iteraciones de alto rendimiento sobre colecciones resueltas desde DI
- [ ] **Revisar y mejorar naming:** Asegurar que todos los nombres sean autodescriptivos y consistentes

#### 1.3 Testing Comprehensivo

- [ ] **Elevar cobertura a ≥95%:** Añadir tests para:
  - Casos edge en cancelación
  - Escenarios de error poco comunes
  - Combinaciones de behaviors
  - Paths de recuperación de errores
- [ ] **Ampliar property-based testing:** Más propiedades en `PropertyTests` para:
  - Invariantes del pipeline
  - Comportamiento de cachés
  - Consistency de metadata
- [ ] **Contract tests exhaustivos:** Validar todos los contratos públicos con ejemplos concretos
- [ ] **Elevar mutation score a ≥95%:** Analizar y eliminar mutantes sobrevivientes
- [ ] **Load testing con umbrales estrictos:** Documentar y validar límites de throughput y latencia

#### 1.4 Análisis Estático Avanzado

- [x] **Escaneo de calidad (SonarCloud o CodeFactor):**
  - Configurar SonarCloud en el repositorio
  - Integrar en CI con quality gate obligatorio
  - Technical Debt Ratio ≤ 5%
  - Maintainability Rating A
  - Reliability Rating A
  - Security Rating A
- [ ] **Análisis de complejidad ciclomática:** Identificar y simplificar métodos complejos (target: ≤10 por método)
- [ ] **Análisis de duplicación:** Eliminar código duplicado (target: <3%)
- [ ] **Security hotspots:** Revisar y resolver todos los security hotspots identificados

### Fase 2: Excelencia en Observabilidad y Diagnóstico

#### 2.1 Observabilidad Mejorada

- [ ] **Introducir `MediatorResult<T>`:** Envoltura legible sobre `Either` con adaptadores de compatibilidad
  - Métodos de conveniencia (IsSuccess, GetValue, GetError, etc.)
  - Pattern matching mejorado
  - Serialización JSON
  - Tests de regresión completos
- [ ] **Ampliar `MediatorDiagnostics`:**
  - Eventos adicionales de ciclo de vida
  - Correlation IDs para tracing distribuido
  - Baggage propagation para contexto
  - Structured logging avanzado
- [ ] **Enriquecer `MediatorMetrics`:**
  - Histogramas de latencia por tipo de request
  - Percentiles (p50, p90, p99)
  - Contadores de errores por código
  - Métricas de throughput
- [ ] **Sustituir `object? Details` por contenedor inmutable:**
  - `ImmutableDictionary<string, object?>` para metadata
  - API de consulta type-safe
  - Serialización completa
  - Documentar contrato de metadatos

#### 2.2 Testing de Observabilidad

- [ ] **Tests de telemetría exhaustivos:**
  - Validar todos los spans de Activity
  - Verificar propagación de contexto
  - Asegurar que métricas se emiten correctamente
  - Tests de structured logging
- [ ] **Tests de integración con OTEL:**
  - Validar exportación a Jaeger/Zipkin
  - Verificar métricas en Prometheus
  - Tests end-to-end de observabilidad

### Fase 3: Seguridad y Cadena de Suministro

#### 3.1 Seguridad Avanzada

- [ ] **Automatizar SBOM continua:**
  - Generar SBOM en cada release
  - Validar integridad de dependencias
  - Publicar SBOM como artifact
- [ ] **Implementar supply chain security (SLSA Level 2):**
  - Provenance statements con GitHub OIDC
  - Firma de artifacts con cosign/Sigstore
  - Verificación de integridad en CI
- [ ] **Dependabot automation:**
  - Auto-merge de dev-dependencies tras CI verde
  - Review process para prod dependencies
  - Security advisory monitoring
- [ ] **Revisar permisos mínimos en workflows:**
  - Aplicar principio de menor privilegio
  - Auditar tokens y secrets
  - Documentar permisos necesarios

#### 3.2 Threat Modeling

- [ ] **Adoptar framework STRIDE para nuevos features:**
  - Documentar amenazas identificadas
  - Mitigaciones implementadas
  - Security tests correspondientes
- [ ] **Security testing:**
  - Fuzzing de inputs
  - Tests de injection
  - Validación de límites

### Fase 4: Producción y Lanzamiento Público

#### 4.1 Preparación para Publicación

- [ ] **Licencia y legal:**
  - Confirmar y documentar licencia (MIT recomendado)
  - Headers de copyright en archivos
  - NOTICE file con atribuciones
  - Contributor License Agreement (CLA) si aplicable
- [ ] **Branding y packaging:**
  - Icon para el paquete NuGet
  - README.md pulido y profesional
  - Tags y keywords optimizados
  - Links a documentación y soporte
- [ ] **Release automation:**
  - GitHub Release Drafter configurado
  - Changelog automático por tipo de cambio
  - Semantic versioning estricto
  - Release notes detallados

#### 4.2 Firma y Distribución

- [ ] **Firma de paquetes NuGet:**
  - Configurar Authenticode o Sigstore
  - Pipeline de firma automática
  - Verificación en instalación
- [ ] **Publicación automatizada:**
  - Pipeline condicionado a todos los gates pasando
  - Rollback automático en caso de fallo
  - Notificaciones de releases
- [ ] **Multi-target support:**
  - Evaluar soporte para .NET 8/9 además de 10
  - Tests de compatibilidad
  - Documentar requirements de framework

#### 4.3 Soporte y Comunidad

- [ ] **Issue templates:**
  - Bug report con reproducción mínima
  - Feature request con justificación
  - Question/Discussion
- [ ] **Pull Request template mejorado:**
  - Checklist exhaustiva de calidad
  - Guidelines de contribución
  - Proceso de review documentado
- [ ] **Protección de ramas:**
  - Revisores obligatorios
  - Status checks obligatorios
  - Firmas de commits (opcional)
- [ ] **Comunicación:**
  - Blog/changelog para releases
  - Twitter/social media presence (opcional)
  - Stack Overflow monitoring

### Fase 5: Optimización y Escalabilidad

#### 5.1 Performance Extremo

- [ ] **Benchmarks comprehensivos:**
  - Suite completa de micro-benchmarks
  - Comparación con alternativas (MediatR, etc.)
  - Regression testing automático
  - Publicar resultados
- [ ] **Optimizaciones avanzadas:**
  - Source generators para eliminar reflection completamente
  - Memory pooling para hot paths
  - Span<T> y stackalloc donde sea seguro
  - Inlining hints para JIT
- [ ] **Load testing en producción:**
  - Chaos engineering scenarios
  - Stress testing con NBomber
  - Endurance testing (24h+)
  - Resource leak detection

#### 5.2 Extensibilidad

- [ ] **Plugin system:**
  - Extensibility points documentados
  - Ejemplos de extensiones comunes
  - Testing de extensiones
- [ ] **Behaviors adicionales incluidos:**
  - Retry con Polly
  - Circuit breaker
  - Rate limiting
  - Caching
  - Idempotency
- [ ] **Samples y templates:**
  - Proyecto template para nuevos proyectos
  - Samples de casos de uso comunes
  - Integration samples (ASP.NET Core, etc.)

### Mejora Continua del Core

- [ ] Refactorizar `SimpleMediator.Publish` para delegar validaciones/guards en helpers internos (parcialmente: `Send` ya usa `PipelineBuilder`).
- [ ] Extraer guard clauses reutilizables (`EnsureRequest`, `EnsureNextStep`, etc.) que encapsulen la creación de errores estándar.
- [x] Adoptar namespaces con ámbito de archivo en todo `src/SimpleMediator` para incrementar la legibilidad y coherencia de estilo.
- [x] Evolucionar `RequestHandlerCallback<T>` a `ValueTask<Either<MediatorError,T>>`.
- [ ] Replantear las cachés para minimizar reflection y boxing en el camino crítico.
- [x] Introducir un `PipelineBuilder<TRequest,TResponse>` reutilizable.
- [ ] Definir una envoltura `MediatorResult<T>` para expresar el resultado de forma más legible.
- [x] Centralizar los códigos de error en `MediatorErrorCodes`.
- [x] Encapsular la instrumentación en `MediatorDiagnostics`.
- [ ] Sustituir `object? Details` por un contenedor inmutable.
- [ ] Considerar `CollectionsMarshal.AsSpan` para iteraciones de alto rendimiento.

## Próximos Pasos Operativos

### Completados

- [x] Crear workflows `dotnet-ci.yml`, `codeql.yml`, `sbom.yml`, `benchmarks.yml` con checks obligatorios.
- [x] Añadir `Directory.Build.props`/`Directory.Build.targets` con reglas de analizadores y `TreatWarningsAsErrors`.
- [x] Integrar Coverlet + ReportGenerator y publicar badge de cobertura.
- [x] Incorporar Conventional Commits lint y actualizar plantilla de PR.
- [x] Añadir gate de cobertura en CI (umbral 90% líneas).
- [x] Redactar `CONTRIBUTING.md` con requisitos de calidad y guía de colaboración.
- [x] Parametrizar umbral de cobertura (variable `COVERAGE_THRESHOLD` con default 90%).
- [x] Definir `MediatorErrorCodes` y estrategia de metadatos inmutables.
- [x] Diseñar y prototipar `PipelineBuilder<TRequest,TResponse>` + migración a `ValueTask`.
- [x] Habilitar PublicApiAnalyzers con baseline completo.

### Inmediatos (Sprint Actual)

- [ ] Configurar SonarCloud y añadir quality gate al README
- [ ] Auditar y completar XML documentation en todo el API público
- [ ] Elevar cobertura de tests a ≥95%
- [ ] Elevar mutation score a ≥95%
- [ ] Adoptar namespaces con ámbito de archivo
- [ ] Extraer guard clauses reutilizables
- [ ] Configurar DocFX para documentación API

### Siguiente Sprint

- [ ] Crear Architecture Decision Records (ADRs)
- [ ] Implementar `MediatorResult<T>`
- [ ] Optimizar cachés de delegados
- [ ] Ampliar property-based testing
- [ ] Configurar SLSA Level 2

### Mediano Plazo

- [ ] Source generators para eliminar reflection
- [ ] Behaviors adicionales (retry, circuit breaker, etc.)
- [ ] Templates y samples
- [ ] Multi-framework support
- [ ] Publicación en NuGet.org

## Seguimiento y Revisión

- **Daily:** Verificar CI/CD pipelines verdes
- **Weekly:** Revisar métricas de calidad (cobertura, mutation, SonarCloud)
- **Monthly:** Auditar dependencies, actualizar roadmap según evolución
- **Quarterly:** Auditar workflows, revisar permisos, planificar siguientes fases
- **Per Release:** Validar todos los quality gates, generar SBOM, publicar changelog

## Métricas de Calidad Actuales

| Métrica | Valor Actual | Objetivo | Estado |
|---------|-------------|----------|--------|
| Cobertura de Líneas | 92.9% | ≥95% | 🟡 Cerca |
| Mutation Score | 93.74% | ≥95% | 🟡 Cerca |
| Build Warnings | 0 | 0 | ✅ OK |
| Tests Passing | 212/212 | 100% | ✅ OK |
| XML Documentation | ~23% | 100% | 🔴 Necesita trabajo |
| Technical Debt | Unknown | ≤5% | ⏳ Pendiente (SonarCloud) |
| Complexity | Unknown | ≤10/método | ⏳ Pendiente |
| Duplication | Unknown | <3% | ⏳ Pendiente |
| Security Rating | Unknown | A | ⏳ Pendiente |

## Referencias

- [Conventional Commits](https://www.conventionalcommits.org/)
- [SLSA Framework](https://slsa.dev/)
- [SonarCloud Quality Gates](https://docs.sonarcloud.io/improving/quality-gates/)
- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [NuGet Package Signing](https://docs.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [GitHub Advanced Security](https://docs.github.com/en/get-started/learning-about-github/about-github-advanced-security)
