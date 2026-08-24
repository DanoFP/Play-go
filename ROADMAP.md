# Próximos pasos

Lista para priorizar. El orden dentro de cada bloque es sugerido, no obligatorio.
El esfuerzo es una estimación gruesa: S = un rato, M = media jornada, L = más.

---

## Hecho

- **Tests en el repo**: `npm test` (generador, sin dependencias) y
  `npm run test:ui` (interfaz, con Playwright).
- **Dificultad por profundidad de deducción**: solver lógico que propaga
  restricciones como una persona; solo se aceptan puzzles resolubles sin
  adivinar, con un único punto de partida y cadena ordenada.
- **Plantas no cuadradas e irregulares**: H×W con min(H,W) personas y celdas
  recortadas, según la especificación del Murdoku original.
- **Mobiliario distinguible** y leyenda de objetos.
- **Generación en Web Worker** con loader: el hilo de UI ya no se bloquea.
- **Escalera optimizada**: se generan varios casos válidos y se devuelve el de
  mejor escalera (90% de escalones distintos de media).
- **Detección de contradicciones** en vivo, con la declaración rota tachada.
- **Deshacer / rehacer**.
- **Pistas graduadas** en tres niveles.
- **Casos con semilla**: número de caso reproducible y enlace para compartir.
- **Persistencia** de estadísticas y mejores tiempos.
- **Tablero adaptable** a pantallas chicas.
- **`constraints` fuera de la API**: las pistas son datos puros, sin closures.

---

## Deuda abierta

### 1. Accesibilidad — M
El tablero es SVG con clicks y nada más: sin navegación por teclado, sin roles
ARIA, sin foco visible. Para un juego de grilla el teclado es natural (flechas +
enter) y no es difícil.

### 2. Retoques visuales menores — S
- Las etiquetas de habitación a veces quedan pisadas por un mueble.
- Al colocar o retirar una ficha no hay transición; una animación corta ayuda a
  seguir qué cambió.

---


### 3. Notas por sospechoso — M
Hoy el descarte ✕ es genérico: marca "aquí no hay nadie". En un Murdoku de
verdad uno anota *qué* sospechoso no puede estar en cada casilla. Permitir
marcas por persona (con su color) es la herramienta que más se echa en falta
para resolver los casos difíciles a mano.

### 4. Caso diario — S
La semilla ya está: derivarla de la fecha y todos juegan el mismo caso, con
racha y comparación de tiempos. Es lo que engancha en este género.

### 5. Repaso de la deducción al terminar — M
Al resolver o rendirse, mostrar la cadena paso a paso: qué pista fija a quién y
qué se abrió con ello. El solver ya calcula exactamente eso.
