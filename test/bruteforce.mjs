import { makeIndex, evalClue } from '../src/utils/puzzleGenerator.js';

// Fuerza bruta con poda incremental. Independiente del solver del generador:
// aquí se enumera y se comprueba, sin propagación de dominios ni heurísticas.
export function bruteForce(p, limit = 2) {
  const board = makeIndex(p);
  const cells = [];
  for (const k of p.live) {
    if (p.blockedSet.has(k)) continue;
    const [r, c] = k.split(',').map(Number);
    cells.push({ row: r, col: c });
  }
  const sols = [];
  const pos = new Array(p.P).fill(null);
  const usedR = new Set(), usedC = new Set();

  // Una pista es comprobable cuando su dueño y sus referencias ya están
  // puestos; comprobarla en ese momento es lo que hace viable la enumeración.
  const byLevel = Array.from({ length: p.P }, (_, i) =>
    p.clues.filter(cl => Math.max(cl.owner, ...cl.refs) === i));

  (function rec(i) {
    if (sols.length >= limit) return;
    if (i === p.P) { sols.push(pos.map(x => ({ ...x }))); return; }
    for (const cell of cells) {
      if (usedR.has(cell.row) || usedC.has(cell.col)) continue;
      pos[i] = cell; usedR.add(cell.row); usedC.add(cell.col);
      if (byLevel[i].every(cl => evalClue(cl, pos, board))) rec(i + 1);
      usedR.delete(cell.row); usedC.delete(cell.col); pos[i] = null;
      if (sols.length >= limit) return;
    }
  })(0);
  return sols;
}
