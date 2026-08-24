# Próximos pasos

Lista para priorizar. El orden dentro de cada bloque es sugerido, no obligatorio.
El esfuerzo es una estimación gruesa: S = un rato, M = media jornada, L = más.

---

## Deuda que dejé abierta a propósito

### 1. Mover los tests del generador al repo — S
Escribí dos suites que encontraron el bug del asesino incorrecto (afectaba a ~60%
de los puzzles) y verifican unicidad por fuerza bruta. Viven en un directorio
temporal, así que **hoy no hay nada que detecte una regresión**:

- Invariantes estructurales: N-torres, habitaciones conexas, cobertura de la
  grilla, asesino único en la habitación de la víctima, víctima bien derivada.
- Unicidad independiente: enumera todas las permutaciones de columnas sin la poda
  del solver y comprueba que solo una satisface las pistas, y que coincide con la
  solución declarada.

Es lo más barato y lo que más protege lo que ya funciona.

### 2. Sacar `constraints` de la API pública del puzzle — S
`generatePuzzle` devuelve `constraints` con los predicados (closures) solo para
que los tests puedan reverificar. La UI no lo usa. Si los tests entran al repo
conviene exponerlo por una vía aparte en vez de colgarlo del objeto de juego.

---

## Jugabilidad

### 3. Dificultad medida por profundidad de deducción — L
Hoy la dificultad se decide por la **fuerza** de las pistas (qué tiers entran al
sorteo). Eso es un proxy: un puzzle "difícil" puede caer rápido si el sorteo
resulta muy determinante, y uno "normal" puede ser durísimo.

Lo correcto es un solver *lógico* (no de fuerza bruta) que aplique reglas de
deducción paso a paso y cuente cuántas rondas necesita y de qué tipo. Con esa
métrica la dificultad pasa a ser real y reproducible. Es el cambio que más sube
la calidad de los puzzles, y también el más caro.

### 4. Detección de contradicciones mientras jugás — M
Ahora solo se puede verificar al final, con todo colocado. Los Murdoku
comerciales avisan cuando una colocación ya contradice una pista. Requiere
evaluar los predicados contra el estado parcial del jugador — la infraestructura
ya está (`constraints` con sus `test`), falta decidir cuánto delata.

### 5. Deshacer / rehacer — S
Es un juego de deducción: equivocarse y volver atrás es parte del bucle. Hoy hay
que retirar fichas a mano una por una.

### 6. Pistas graduadas — S
La pista actual revela directamente una casilla correcta, que es casi rendirse.
Mejor escalar: descartar una fila, señalar qué sospechoso es el más acotado, y
recién después revelar.

---

## Producto

### 7. Casos con semilla y caso diario — M
Todo usa `Math.random()`, así que un puzzle no se puede reproducir ni compartir.
Con un RNG sembrado cada caso tiene número, se puede compartir ("caso #1234") y
habilita el modo diario, que es el que engancha en este género.

### 8. Persistencia — S
Un refresh pierde la partida. Guardar en `localStorage` el puzzle en curso, y de
paso tiempos y rachas por dificultad.

---

## Presentación

### 9. Tablero adaptable a móvil — M
El 7×7 mide ~454px de ancho fijo y se desborda en pantallas chicas. El CSS ya
oculta el texto de los chips por debajo de 720px, pero el tablero no escala.
Conviene que el SVG use `viewBox` con ancho fluido.

### 10. Accesibilidad — M
El tablero es SVG con clicks y nada más: sin navegación por teclado, sin roles
ARIA, sin foco visible. Para un juego de grilla el teclado es natural (flechas +
enter) y no es difícil.

### 11. Retoques visuales menores — S
- Las etiquetas de habitación a veces quedan pisadas por un mueble.
- Al colocar o retirar una ficha no hay transición; una animación corta ayuda a
  seguir qué cambió.

---

## Rendimiento

### 12. Generación fuera del hilo principal — M
El peor caso (7×7 difícil) está en ~250ms de mediana y ~450ms de máximo. Se nota
como un tirón al pedir caso nuevo. Un Web Worker lo saca del hilo de UI; una
alternativa más barata es pregenerar el siguiente caso mientras se juega el
actual.
