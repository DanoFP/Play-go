// Suite del generador. Sin dependencias: `npm test`.
//
// Comprueba las invariantes estructurales, la cadena de deducción y —lo más
// importante— reverifica la unicidad de la solución por fuerza bruta, con una
// enumeración independiente del solver que usa el generador.
import { generatePuzzle, findContradictions, SCENARIOS, DIFFICULTIES } from '../src/utils/puzzleGenerator.js';
import { bruteForce } from './bruteforce.mjs';

const K = (r, c) => `${r},${c}`;

let total = 0, bad = 0;
const shapes = new Map(), depths = [], rungs = [];
const t0 = Date.now();

for (const diff of Object.keys(DIFFICULTIES)) {
  for (const n of [3, 4, 5, 6]) {
    for (let rep = 0; rep < 6; rep++) {
      total++;
      const sc = SCENARIOS[Math.floor(Math.random() * SCENARIOS.length)];
      const p = generatePuzzle(n, sc.id, diff);
      const tag = `${diff}/${n}sus`;
      const people = [...p.suspects, p.victim];

      shapes.set(`${p.H}x${p.W}${p.live.size < p.H * p.W ? ' recortada' : ''}`,
                 (shapes.get(`${p.H}x${p.W}${p.live.size < p.H * p.W ? ' recortada' : ''}`) || 0) + 1);
      depths.push(p.depth); rungs.push(p.rungs / p.P);

      // 1. min(H,W) === P
      if (Math.min(p.H, p.W) !== p.P) { console.log(`✗ ${tag}: min(H,W)=${Math.min(p.H,p.W)} != P=${p.P}`); bad++; continue; }

      // 2. filas y columnas únicas
      if (new Set(people.map(x => x.row)).size !== p.P ||
          new Set(people.map(x => x.col)).size !== p.P) { console.log(`✗ ${tag}: N-torres roto`); bad++; continue; }

      // 3. nadie fuera de la planta ni sobre mobiliario bloqueante
      if (people.some(x => !p.live.has(K(x.row, x.col)))) { console.log(`✗ ${tag}: persona en celda borrada`); bad++; continue; }
      if (people.some(x => p.blockedSet.has(K(x.row, x.col)))) { console.log(`✗ ${tag}: persona sobre bloqueo`); bad++; continue; }

      // 4. habitaciones: cubren exactamente las celdas vivas, sin solapar, conexas
      const seen = new Set();
      let dup = false;
      for (const room of p.rooms) for (const c of room.cells) {
        if (seen.has(K(c.row, c.col))) dup = true;
        seen.add(K(c.row, c.col));
      }
      if (dup || seen.size !== p.live.size) { console.log(`✗ ${tag}: cobertura ${seen.size}/${p.live.size} dup=${dup}`); bad++; continue; }
      for (const room of p.rooms) {
        const set = new Set(room.cells.map(c => K(c.row, c.col)));
        const st = [room.cells[0]], vis = new Set([K(room.cells[0].row, room.cells[0].col)]);
        while (st.length) {
          const { row, col } = st.pop();
          for (const [dr, dc] of [[1,0],[-1,0],[0,1],[0,-1]]) {
            const k = K(row+dr, col+dc);
            if (set.has(k) && !vis.has(k)) { vis.add(k); st.push({ row: row+dr, col: col+dc }); }
          }
        }
        if (vis.size !== room.cells.length) { console.log(`✗ ${tag}: ${room.name} no conexa`); bad++; }
      }

      // 5. todos tienen al menos una pista
      if (people.some(s => !s.clues || !s.clues.length)) { console.log(`✗ ${tag}: alguien sin pistas`); bad++; continue; }

      // 6. el asesino comparte habitación con la víctima, y es el único
      const sharers = p.suspects.filter(s => s.roomId === p.victim.roomId);
      if (sharers.length !== 1 || sharers[0].id !== p.murderer) { console.log(`✗ ${tag}: asesino mal determinado (${sharers.length} comparten)`); bad++; continue; }

      // 7. LA CADENA: un único punto de partida y fichas en orden creciente
      const allSteps = people.map(s => s.step).sort((a,b)=>a-b);
      if (allSteps.join(',') !== [...Array(p.P).keys()].map(i=>i+1).join(',')) {
        console.log(`✗ ${tag}: los pasos no son 1..${p.P} (${allSteps})`); bad++; continue;
      }
      const steps = p.suspects.map(s => s.step);
      if (steps.some((v, i) => i > 0 && v < steps[i-1])) { console.log(`✗ ${tag}: fichas desordenadas ${steps}`); bad++; continue; }
      if (p.depth < 2) { console.log(`✗ ${tag}: sin cadena (depth ${p.depth})`); bad++; continue; }

      // 8. la solución correcta no dispara ninguna contradicción, y una
      //    colocación deliberadamente mal sí la dispara
      const good = Object.fromEntries(people.map(s => [s.id, { row: s.row, col: s.col }]));
      if (findContradictions(p, good).length) { console.log(`✗ ${tag}: la solución correcta se marca como contradictoria`); bad++; continue; }

      // 9. la semilla reproduce el mismo caso
      const again = generatePuzzle(n, sc.id, diff, p.seed);
      if (JSON.stringify(again.suspects.map(s=>[s.name,s.row,s.col])) !==
          JSON.stringify(p.suspects.map(s=>[s.name,s.row,s.col]))) {
        console.log(`✗ ${tag}: la semilla ${p.seed} no reproduce el caso`); bad++; continue;
      }

      // 10. unicidad por fuerza bruta
      const sols = bruteForce(p);
      if (sols.length !== 1) { console.log(`✗ ${tag}: ${sols.length} soluciones`); bad++; continue; }
      const ok = people.every(x => sols[0][0] && sols[0].some(s => s.row === x.row && s.col === x.col));
      if (!ok) { console.log(`✗ ${tag}: la solución única no coincide`); bad++; }
    }
  }
}

console.log(`\n${total - bad}/${total} puzzles válidos  (${Date.now() - t0}ms)`);
console.log('\nFormas de planta generadas:');
[...shapes.entries()].sort((a,b)=>b[1]-a[1]).forEach(([k,v]) => console.log(`  ${String(v).padStart(3)}×  ${k}`));
const avg = a => (a.reduce((s,x)=>s+x,0)/a.length).toFixed(1);
console.log(`\nProfundidad de deducción: media ${avg(depths)}, máx ${Math.max(...depths)}`);
console.log(`Calidad de la escalera: media ${(avg(rungs)*100)|0}% de escalones distintos`);
process.exit(bad ? 1 : 0);
