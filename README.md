# Murdoku

Juego de lógica deductiva en React, inspirado en el Murdoku de Manuel Garand:
un sudoku de escena del crimen.

## Reglas

- La escena es una grilla N×N dividida en habitaciones irregulares.
- Hay N personas: N−1 sospechosos y la víctima.
- **Cada persona ocupa una fila y una columna únicas** (colocación de N torres).
- El mobiliario no ocupable (mesas, plantas, chimeneas…) **bloquea** su casilla:
  nadie puede estar ahí. Las camas, sillas, sofás y alfombras sí se pueden ocupar.
- Colocados todos los sospechosos, la víctima queda en la única casilla libre.
- **El asesino es quien comparte habitación con la víctima.**

## Generación de puzzles

El generador produce la solución primero, deriva todas las pistas que son
ciertas para ella y después elige un subconjunto que la deja **demostrablemente
única**. La unicidad se verifica con un solver de backtracking sobre
restricciones unarias y binarias.

La selección de pistas no puede fallar a mitad de camino: añadir pistas nunca
aumenta el número de soluciones, así que si el pool completo no da unicidad se
descarta el tablero de entrada, y si la da, reforzar de a una pista converge
siempre. Al final se podan las pistas redundantes.

### Tipos de pista

| Fuerza | Pistas |
| --- | --- |
| Directa | sobre un mueble, en una habitación, en la fila/columna N |
| Media | junto a un mueble, al lado de alguien, misma habitación, en una esquina |
| Deductiva | a la izquierda/derecha/norte/sur de alguien, a N casillas, negaciones, pegado a una pared |

La dificultad decide qué fuerzas entran al sorteo: **Fácil** usa pistas
directas, **Difícil** solo deductivas y relacionales.

## Escenarios

La Mansión · El Transatlántico · El Expreso · El Hotel · El Museo · El Palacio ·
La Aldea — cada uno con sus habitaciones, paleta y texturas de suelo.

## Desarrollo

```bash
npm install
npm run dev      # servidor de desarrollo
npm run build    # build de producción
```

Stack: React 18 + Vite. El tablero se dibuja en SVG (mobiliario vectorial,
texturas de suelo por habitación, paredes con puertas).
