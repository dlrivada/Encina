---

## Encina Framework Visual Identity

### Icon Usage Guide

- **SVG**: Usar para web, documentación, presentaciones y cuando se requiera escalabilidad sin pérdida de calidad.
- **PNG**: Exportar en tamaños 32x32, 64x64, 128x128, 256x256, 512x512 para iconos de aplicaciones, favicons, CLI, etc.
- **ICO**: Convertir para aplicaciones Windows.
- **Monocromo**: Usar solo el contorno o una sola tinta para CLI, terminales o fondos oscuros.
- **Coloreado**: Usar la versión completa para web, banners y material promocional.

### Color Palette

Utiliza una paleta de verdes oscuros ( #2E5E2A, #7A9D54 ) y marrón cálido ( #8B5E3C ), con variantes para fondos claros y oscuros. 

**Recomendaciones:**
- Mantener la proporción y el espacio de seguridad alrededor del icono.
- No distorsionar ni rotar el logotipo.
- Usar la paleta oficial para mantener coherencia visual.

# Guía para el cambio de nombre del framework

Esta guía te ayudará a planificar y ejecutar el renombrado completo de tu framework, asegurando una transición ordenada y sin perder detalles importantes.

## 1. Elección del nuevo nombre

- Elige un nombre único, memorable y fácil de escribir.
- Verifica disponibilidad en GitHub, NuGet y dominios web.

## 2. Planificación de impacto

Haz un inventario de todo lo que se verá afectado:

- Nombre del repositorio y organización en GitHub.
- Nombres de carpetas y archivos principales.
- Espacios de nombres (namespaces) en el código fuente.
- Nombres de paquetes NuGet.
- Documentación (README, wiki, ejemplos, comentarios).
- Issues, PRs, badges, workflows de CI/CD.
- Referencias en proyectos de ejemplo, plantillas, adaptadores, etc.
- Comunicaciones externas (blog, web, redes sociales).

## 3. Estrategia de migración

- Decide si harás un "big bang" (todo de golpe) o una migración progresiva.
- Prepara scripts para automatizar renombrados masivos.
- Considera mantener el repo antiguo con un README de redirección y/o paquetes NuGet deprecados que apunten al nuevo.

## 4. Ejecución técnica

- Renombra el repositorio en GitHub y actualiza URLs.
- Cambia los namespaces y nombres de archivos en el código.
- Actualiza los archivos de solución (.sln, .csproj, .props, .targets).
- Modifica la documentación y ejemplos.
- Actualiza los pipelines de CI/CD y badges.
- Publica los nuevos paquetes NuGet con el nuevo nombre.
- Si es posible, publica un paquete deprecado con el nombre antiguo que apunte al nuevo.

## 5. Comunicación y soporte

- Anuncia el cambio en todos los canales (GitHub, NuGet, web, redes).
- Explica los motivos y beneficios del cambio.
- Proporciona una guía de migración para usuarios existentes.
- Mantén soporte para el nombre antiguo durante un tiempo prudencial.

## 6. Verificación y feedback

- Prueba que todo funcione con el nuevo nombre (build, tests, publicación).
- Recoge feedback de la comunidad y ajusta si es necesario.

---

---

## Sugerencias de nombres para el framework

A continuación, una lista de términos inspiradores de varios ámbitos, cada uno con una breve explicación de por qué podría ser un buen nombre para un framework que actúa como base, coordinador o columna vertebral:

### Física y Astrofísica

- **Quark**: Componente fundamental de la materia, base de todo lo complejo.
- **Boson**: Partícula que media fuerzas y conecta elementos.
- **Nexus**: Punto de unión o conexión de sistemas.
- **Core**: El núcleo, lo esencial y central.
- **Atlas**: Sostiene y conecta, como el titán que sostiene el mundo.
- **Nebula**: Región donde nacen nuevas estrellas, origen de sistemas.
- **Galaxia**: Agrupa y coordina sistemas y estrellas.
- **Pulsar**: Fuente constante y confiable de energía y señales.
- **Singularity**: Punto de concentración máxima, origen de transformación.
- **Matrix**: Medio donde se desarrollan y conectan los elementos.

### Anime

- **Senpai**: Figura guía, mentor que ayuda a crecer.
- **Kizuna**: Significa "lazo" o "vínculo" en japonés, une y conecta.
- **Guild**: Organización que agrupa y coordina aventureros.
- **Nakama**: Compañero, grupo unido por un propósito.
- **Akira**: Nombre que significa "brillantez" o "claridad".
- **Shonen**: Género de historias de crecimiento y superación.

### Scouts

- **Totem**: Símbolo que une y representa a un grupo.
- **Clan**: Grupo unido y coordinado.
- **Patrulla**: Equipo pequeño, ágil y coordinado.
- **Manada**: Comunidad que cuida y crece junta.
- **Sendero**: Camino que guía y estructura el avance.
- **Basecamp**: Punto de partida y organización para la aventura.

---

### Filosofía, Ciencia y Matemáticas

- **Aristóteles**: Padre de la lógica y la ciencia racional.
- **Hypatia**: Matemática y astrónoma pionera de Alejandría.
- **Gauss**: Genio matemático, base de la estadística y el álgebra moderna.
- **Ada**: Ada Lovelace, primera programadora de la historia.
- **Turing**: Alan Turing, padre de la computación moderna.
- **Noether**: Emmy Noether, revolucionó el álgebra y la física teórica.
- **Euclides**: Fundador de la geometría clásica.
- **Descartes**: Filósofo y matemático, "pienso, luego existo".
- **Sagan**: Carl Sagan, divulgador y explorador del cosmos.
- **Curie**: Marie Curie, pionera en radioactividad y doble Nobel.

### Exploración Espacial y Ciencia

- **Sputnik**: Primer satélite artificial, inicio de la era espacial.
- **Voyager**: Sondas que exploran el sistema solar y más allá.
- **Apollo**: Programa que llevó al ser humano a la Luna.
- **Laika**: Primera perra en orbitar la Tierra.
- **Hubble**: Telescopio que revolucionó la astronomía.
- **Perseverance**: Rover que explora Marte.
- **Cassini**: Sonda que estudió Saturno y sus lunas.
- **Galileo**: Científico y sonda que exploró Júpiter.

### Informática, Programación y Tecnología

- **Unix**: Sistema operativo base de la informática moderna.
- **Linux**: Núcleo de código abierto, símbolo de comunidad y robustez.
- **Pascal**: Lenguaje de programación y matemático suizo.
- **Cortex**: Núcleo de procesadores ARM.
- **ENIAC**: Primer ordenador electrónico de propósito general.
- **Babbage**: Charles Babbage, precursor de la máquina analítica.
- **Grace**: Grace Hopper, pionera en compiladores y COBOL.
- **Ritchie**: Dennis Ritchie, creador de C y Unix.
- **Torvalds**: Linus Torvalds, creador de Linux y Git.

### Anime y Scouts (personajes, autores, obras)

- **Miyazaki**: Hayao Miyazaki, creador de Studio Ghibli.
- **Totoro**: Personaje icónico de Ghibli, símbolo de protección y naturaleza.
- **Astro**: Astro Boy, pionero del manga y anime.
- **Akira**: Obra clave del cyberpunk japonés.
- **Baden**: Baden-Powell, fundador del movimiento scout.
- **Akela**: Lobo guía en "El libro de la selva", símbolo scout.
- **Gilwell**: Lugar emblemático de formación scout.

### Naturaleza (plantas, animales, minerales)

- **Encina**: Árbol robusto y símbolo de la península ibérica.
- **Olivo**: Árbol milenario, símbolo de paz y longevidad.
- **Lince**: Felino emblemático de la fauna ibérica.
- **Iris**: Flor resistente y colorida.
- **Granito**: Roca fuerte y duradera.
- **Alondra**: Ave asociada a la libertad y el canto.
- **Jara**: Arbusto típico mediterráneo, resistente y aromático.

### Topónimos antiguos de la península ibérica

- **Abdera**: Antigua ciudad fenicia (actual Adra, Almería).
- **Numancia**: Símbolo de resistencia celtíbera.
- **Sagunto**: Ciudad con raíces íberas y romanas.
- **Tartessos**: Legendaria civilización del suroeste peninsular.
- **Gadir**: Nombre fenicio de Cádiz, una de las ciudades más antiguas de Occidente.
- **Iberia**: Nombre antiguo de la península y su pueblo.
- **Munda**: Antigua ciudad romana, escenario de batallas históricas.

Puedes usar estos términos como inspiración, combinarlos o adaptarlos según el enfoque y personalidad que quieras para tu framework.

---


---

## Why "Encina"?

**Encina** is the Spanish word for the holm oak, a native Mediterranean tree renowned for its strength, resilience, and longevity. The holm oak (Quercus ilex) is a keystone species in the Iberian Peninsula, forming the backbone of ancient forests and supporting a rich ecosystem. For centuries, the encina has symbolized endurance, stability, and the ability to thrive in challenging environments.

We chose "Encina" as the name for this framework because it represents the same qualities we want to offer developers: a solid, reliable foundation that supports growth, adapts to diverse needs, and endures over time. Just as the encina tree anchors and nourishes its ecosystem, Encina Framework is designed to be the backbone of your applications, empowering you to focus on what truly matters while it takes care of the essential infrastructure.

### How to pronounce "Encina"

Encina is pronounced: **en-THEE-nah** (for Spanish).

- "en" as in "end" (without the "d")
- "TH" as in "think" (the "c" in Spanish is pronounced like the English "th")
- "EE" as in the verb "see"
- "nah" as in "na" from "banana"

All together: **en-THEE-nah Framework**

This pronunciation guide will help English speakers say the name as intended and avoid confusion.

---

Puedes adaptar y ampliar esta guía según las necesidades de tu proyecto. ¡Éxito con el cambio de nombre!
