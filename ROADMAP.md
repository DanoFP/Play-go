# Próximos pasos

Lista para priorizar. El orden dentro de cada bloque es sugerido, no obligatorio.
El esfuerzo es una estimación gruesa: S = un rato, M = media jornada, L = más.

---

## Hecho desde la última revisión

- **Tests en el repo** (`npm test`): invariantes, cadena de deducción y
  unicidad reverificada por fuerza bruta. Ya hay red de seguridad.
- **Dificultad por profundidad de deducción**: hay un solver lógico que
  propaga restricciones como una persona; el generador solo acepta puzzles
  resolubles sin adivinar, con al menos un ancla y una cadena ordenada.
- **Plantas no cuadradas e irregulares**: grilla H×W con min(H,W) personas y
  celdas recortadas, siguiendo la especificación del Murdoku original.
- **Mobiliario distinguible** y leyenda de objetos.

---

## Deuda abierta

### 1. Sacar `constraints` de la API pública del puzzle — S
`generatePuzzle` devuelve `constraints` con los predicados (closures) solo para
que los tests puedan reverificar por fuerza bruta. La UI no lo usa. Conviene
exponerlo por una vía aparte en vez de colgarlo del objeto de juego.

---

## Jugabilidad

### 2. Detección de contradicciones mientras jugás — M
Ahora solo se puede verificar al final, con todo colocado. Los Murdoku
comerciales avisan cuando una colocación ya contradice una pista. Requiere
evaluar los predicados contra el estado parcial del jugador — la infraestructura
ya está (`constraints` con sus `test`), falta decidir cuánto delata.

### 3. Deshacer / rehacer — S
Es un juego de deducción: equivocarse y volver atrás es parte del bucle. Hoy hay
que retirar fichas a mano una por una.

### 4. Pistas graduadas — S
La pista actual revela directamente una casilla correcta, que es casi rendirse.
Mejor escalar: descartar una fila, señalar qué sospechoso es el más acotado, y
recién después revelar.

---

## Producto

### 5. Casos con semilla y caso diario — M
Todo usa `Math.random()`, así que un puzzle no se puede reproducir ni compartir.
Con un RNG sembrado cada caso tiene número, se puede compartir ("caso #1234") y
habilita el modo diario, que es el que engancha en este género.

### 6. Persistencia — S
Un refresh pierde la partida. Guardar en `localStorage` el puzzle en curso, y de
paso tiempos y rachas por dificultad.

---

## Presentación

### 7. Tablero adaptable a móvil — M
Las plantas grandes (hasta 9 columnas) tienen ancho fijo y se desbordan en
pantallas chicas. El CSS ya
oculta el texto de los chips por debajo de 720px, pero el tablero no escala.
Conviene que el SVG use `viewBox` con ancho fluido.

### 8. Accesibilidad — M
El tablero es SVG con clicks y nada más: sin navegación por teclado, sin roles
ARIA, sin foco visible. Para un juego de grilla el teclado es natural (flechas +
enter) y no es difícil.

### 9. Retoques visuales menores — S
- Las etiquetas de habitación a veces quedan pisadas por un mueble.
- Al colocar o retirar una ficha no hay transición; una animación corta ayuda a
  seguir qué cambió.

---

## Rendimiento

### 10. Generación fuera del hilo principal — M
El peor caso (6 sospechosos en difícil) está en ~550ms de mediana y ~1.7s de
máximo: exigir resolubilidad lógica descarta muchos más tableros. Se nota como
un tirón al pedir caso nuevo, y ahora pesa más que antes. Un Web Worker lo saca del hilo de UI; una
alternativa más barata es pregenerar el siguiente caso mientras se juega el
actual.
