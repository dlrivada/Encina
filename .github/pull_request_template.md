# Pull Request Checklist

- [ ] Título/PR sigue Convencional Commits (`type(scope?): subject`) o excepción justificada.
- [ ] `dotnet format` y analizador sin warnings (`TreatWarningsAsErrors`) pasan localmente.
- [ ] `dotnet test Encina.slnx --configuration Release` pasa.
- [ ] Cobertura no cae bajo el umbral acordado (consultar README/ci).
- [ ] Se actualizaron docs/badges si aplica.

## Notas

- Motivar cambios relevantes, decisiones de diseño y riesgos.
- Indicar si se tocaron behaviors/pipelines para validar que siguen el rail funcional (sin excepciones en flujo normal).
