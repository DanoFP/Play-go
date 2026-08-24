# Murdoku

Juego de lógica deductiva en React, inspirado en el Murdoku de Manuel Garand:
un sudoku de escena del crimen.

## Reglas

- La escena es una grilla **H×W, no necesariamente cuadrada**, con habitaciones
  irregulares y celdas recortadas para que la planta no sea un rectángulo.
- El número de personas es **min(H, W)**: los sospechosos y la víctima.
- **Cada persona ocupa una fila y una columna únicas.**
- El mobiliario no ocupable (mesas, plantas, chimeneas…) **bloquea** su casilla:
  nadie puede estar ahí. Camas, sillas, sofás y alfombras sí se pueden ocupar.
- La víctima también se coloca y tiene su propia declaración.
- **El asesino es el único sospechoso que comparte habitación con la víctima.**

## Generación de puzzles

El generador produce la solución primero, deriva todas las pistas que son
ciertas para ella y después elige un subconjunto mínimo. El criterio de
aceptación no es que la solución sea única, sino algo más fuerte: que sea
**alcanzable por deducción pura**, sin probar y descartar.

Un solver lógico propaga restricciones como lo haría una persona —recorta cada
dominio con las pistas propias, elimina la fila y la columna de quien ya está
fijado, y aplica consistencia de arco a las pistas relativas— y devuelve en qué
ronda queda fijada cada persona. Con eso el generador garantiza:

- **Un único punto de partida**: exactamente una persona sale con su ficha
  sola, así "la primera es la más fácil" es literal y no un empate.
- Una **cadena**: las fichas se ordenan y numeran por esa secuencia, y cada una
  se abre con lo que resolvieron las anteriores.

Una escalera perfecta —cada persona en su propia ronda— es demasiado rara para
exigirla como filtro: la probabilidad cae a plomo al crecer el número de
personas. En su lugar se generan varios casos válidos y se devuelve el de mejor
escalera, que en la práctica deja el 90% de los escalones distintos.

La selección no puede fallar a mitad de camino: añadir pistas solo recorta
dominios, así que si el pool completo no basta se descarta el tablero de
entrada, y si basta, reforzar de a una pista converge siempre. Al final se podan
las pistas redundantes.

### Tipos de pista

| Fuerza | Pistas |
| --- | --- |
| Directa | sobre un mueble, en una habitación, en la fila/columna N |
| Media | junto a un mueble, al lado de alguien, misma habitación, en una esquina |
| Deductiva | a la izquierda/derecha/norte/sur de alguien, a N casillas, negaciones, pegado a una pared |

La dificultad decide qué fuerzas entran al sorteo. La persona que abre el caso
recibe pistas de cualquier fuerza; el resto, solo las que permita la dificultad.
En **Difícil** eso significa un arranque claro y todo lo demás relacional —si
nadie pudiera tener una pista fuerte no habría punto de partida y el caso solo
se resolvería adivinando.

El orden se corresponde con la estrategia recomendada para el Murdoku original:
resolver primero habitación fija, fila/columna fija y objeto único, y dejar las
relativas para el final.

## Escenarios

La Mansión · El Transatlántico · El Expreso · El Hotel · El Museo · El Palacio ·
La Aldea — cada uno con sus habitaciones, paleta y texturas de suelo.

## Desarrollo

```bash
npm install
npm run dev      # servidor de desarrollo
npm run build    # build de producción
npm test         # suite del generador (sin dependencias)
```

`npm test` comprueba las invariantes estructurales, la cadena de deducción y
reverifica la unicidad de la solución **por fuerza bruta**, con una enumeración
independiente del solver que usa el generador.

Hay además un smoke test de la interfaz (`npm run test:ui`) que necesita
Playwright y un `vite preview` en el puerto 4200.

## Cómo está hecho

Stack: React 18 + Vite, sin más dependencias en runtime.

- El tablero se dibuja en **SVG**: mobiliario vectorial, texturas de suelo por
  habitación, paredes que siguen el contorno real de la planta, y `viewBox` para
  que escale en pantallas chicas.
- La generación corre en un **Web Worker**: buscar un caso resoluble por
  deducción pura puede llevar segundos, y bloquear la UI no era aceptable.
  Mientras tanto se muestra un loader.
- Las pistas son **datos puros** (`{owner, refs, type, text, target}`), sin
  closures. Por eso el puzzle atraviesa `structuredClone` hacia el worker, y el
  hilo principal puede reevaluarlas para avisar de contradicciones en vivo.
- Toda la aleatoriedad pasa por un PRNG sembrado, así que **cada caso tiene
  número** y se puede compartir por enlace.
