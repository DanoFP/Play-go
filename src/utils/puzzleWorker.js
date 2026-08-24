// La generación se fue del hilo principal: exigir resolubilidad lógica y buscar
// la mejor escalera puede tardar varios segundos en los casos grandes, y
// bloquear la UI todo ese rato era inaceptable. Aquí es solo un loader.
//
// Las pistas son datos puros (sin closures), así que el puzzle atraviesa
// structuredClone sin más.

import { generatePuzzle } from './puzzleGenerator.js';

self.onmessage = (e) => {
  const { id, numSuspects, scenarioId, difficulty, seed } = e.data;
  try {
    const puzzle = generatePuzzle(numSuspects, scenarioId, difficulty, seed);
    self.postMessage({ id, puzzle });
  } catch (err) {
    self.postMessage({ id, error: String(err && err.message || err) });
  }
};
