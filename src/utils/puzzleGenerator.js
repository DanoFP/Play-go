// ═══════════════════════════════════════════════════════════════════════════
//  MURDOKU — generador de puzzles con validación de unicidad
// ═══════════════════════════════════════════════════════════════════════════
//
//  Reglas del juego:
//    · Grilla N×N. Hay numSuspects sospechosos + 1 víctima = N personas.
//    · Cada persona ocupa una fila y una columna únicas (N-torres).
//    · Las casillas con mobiliario no ocupable están bloqueadas para todos.
//    · La víctima queda en la última fila/columna libre.
//    · El asesino es quien comparte habitación con la víctima.
//
//  El generador produce la solución primero, deriva TODAS las pistas que son
//  ciertas para esa solución, y después elige un subconjunto mínimo que hace
//  que la solución sea única (verificado por backtracking).

// ─── Nombres ────────────────────────────────────────────────────────────────

const SUSPECT_NAMES = [
  'Cora', 'Bella', 'Axel', 'Douglas', 'Sofía', 'Marco', 'Lena', 'Hugo',
  'Rita', 'Diego', 'Nora', 'Iván', 'Clara', 'Emil', 'Vera',
];
const VICTIM_NAMES = ['Vincent', 'Victor', 'Vera', 'Valentina', 'Vito'];

// ─── Escenarios ─────────────────────────────────────────────────────────────
//  Cada escenario define 7 habitaciones (se usan las primeras K según tamaño)
//  con su propio color y tipo de suelo, que el tablero dibuja como textura.

export const SCENARIOS = [
  {
    id: 'mansion', name: 'La Mansión', icon: '🏛️',
    rooms: [
      { name: 'Gran Salón',  color: '#e6d9be', floor: 'wood'   },
      { name: 'Biblioteca',  color: '#cfd9c2', floor: 'wood'   },
      { name: 'Comedor',     color: '#e0c9c4', floor: 'tile'   },
      { name: 'Cocina',      color: '#d7dde2', floor: 'tile'   },
      { name: 'Dormitorio',  color: '#d8cddf', floor: 'carpet' },
      { name: 'Estudio',     color: '#ddd3bd', floor: 'wood'   },
      { name: 'Bodega',      color: '#c9c4bb', floor: 'stone'  },
    ],
  },
  {
    id: 'transatlantico', name: 'El Transatlántico', icon: '🚢',
    rooms: [
      { name: 'Cubierta',    color: '#c6d8e4', floor: 'wood'   },
      { name: 'Camarotes',   color: '#d2ddc9', floor: 'carpet' },
      { name: 'Sala Máq.',   color: '#c8c5c0', floor: 'metal'  },
      { name: 'Cocina',      color: '#dde2e4', floor: 'tile'   },
      { name: 'Comedor',     color: '#e3d6c2', floor: 'wood'   },
      { name: 'Bodega',      color: '#c4c0b6', floor: 'stone'  },
      { name: 'Puente',      color: '#cdd5e0', floor: 'metal'  },
    ],
  },
  {
    id: 'expreso', name: 'El Expreso', icon: '🚂',
    rooms: [
      { name: 'Vag. Comedor',color: '#e5d5bd', floor: 'wood'   },
      { name: 'Coche Cama',  color: '#d6cbe0', floor: 'carpet' },
      { name: 'Salón',       color: '#d3ddd0', floor: 'carpet' },
      { name: 'Fumadores',   color: '#dcc9c4', floor: 'wood'   },
      { name: 'Equipajes',   color: '#c9c5bd', floor: 'metal'  },
      { name: 'Cocina',      color: '#dbe1e3', floor: 'tile'   },
      { name: 'Furgón',      color: '#c5c2c8', floor: 'metal'  },
    ],
  },
  {
    id: 'hotel', name: 'El Hotel', icon: '🏨',
    rooms: [
      { name: 'Recepción',   color: '#e5d8c0', floor: 'tile'   },
      { name: 'Bar',         color: '#ddc7c2', floor: 'wood'   },
      { name: 'Sala Juntas', color: '#cdd8c4', floor: 'carpet' },
      { name: 'Suite',       color: '#d9cde0', floor: 'carpet' },
      { name: 'Cocina',      color: '#dae0e3', floor: 'tile'   },
      { name: 'Bodega',      color: '#c7c3ba', floor: 'stone'  },
      { name: 'Terraza',     color: '#c9dcc9', floor: 'stone'  },
    ],
  },
  {
    id: 'museo', name: 'El Museo', icon: '🏺',
    rooms: [
      { name: 'Renacimiento',color: '#e4d7bd', floor: 'stone'  },
      { name: 'Arte Moderno',color: '#ccd7e3', floor: 'tile'   },
      { name: 'Antigüedades',color: '#ded0c4', floor: 'stone'  },
      { name: 'Escultura',   color: '#d3ddc9', floor: 'tile'   },
      { name: 'Archivo',     color: '#d2cec3', floor: 'wood'   },
      { name: 'Entrada',     color: '#d9d9d9', floor: 'tile'   },
      { name: 'Almacén',     color: '#c5c1b8', floor: 'stone'  },
    ],
  },
  {
    id: 'palacio', name: 'El Palacio', icon: '👑',
    rooms: [
      { name: 'Salón Trono', color: '#e8d3ae', floor: 'carpet' },
      { name: 'Biblioteca',  color: '#ccd7cc', floor: 'wood'   },
      { name: 'Comedor Real',color: '#ddc8c4', floor: 'wood'   },
      { name: 'Capilla',     color: '#d3d5e5', floor: 'stone'  },
      { name: 'Torre Norte', color: '#c8cdd6', floor: 'stone'  },
      { name: 'Torre Sur',   color: '#d0c9d6', floor: 'stone'  },
      { name: 'Jardín',      color: '#c7dcc0', floor: 'grass'  },
    ],
  },
  {
    id: 'aldea', name: 'La Aldea', icon: '🏘️',
    rooms: [
      { name: 'Taberna',     color: '#e3d2b2', floor: 'wood'   },
      { name: 'Herrería',    color: '#cdc5b8', floor: 'stone'  },
      { name: 'Molino',      color: '#d5dcbd', floor: 'wood'   },
      { name: 'Iglesia',     color: '#d8d8dc', floor: 'stone'  },
      { name: 'Granero',     color: '#d2d3bb', floor: 'wood'   },
      { name: 'Posada',      color: '#dccdb8', floor: 'carpet' },
      { name: 'Establo',     color: '#c9d2be', floor: 'grass'  },
    ],
  },
];

// ─── Mobiliario ─────────────────────────────────────────────────────────────
//  occupiable: true  → una persona PUEDE estar sobre esa casilla
//  occupiable: false → la casilla está BLOQUEADA para todos (pista por sí sola)

export const OBJECT_TYPES = {
  bed:       { def: 'la cama',       indef: 'una cama',        occupiable: true,  on: 'Estaba sobre la cama' },
  chair:     { def: 'la silla',      indef: 'una silla',       occupiable: true,  on: 'Estaba sentado en una silla' },
  rug:       { def: 'la alfombra',   indef: 'una alfombra',    occupiable: true,  on: 'Estaba sobre la alfombra' },
  sofa:      { def: 'el sofá',       indef: 'un sofá',         occupiable: true,  on: 'Estaba en el sofá' },
  table:     { def: 'la mesa',       indef: 'una mesa',        occupiable: false },
  window:    { def: 'la ventana',    indef: 'una ventana',     occupiable: false },
  plant:     { def: 'la planta',     indef: 'una planta',      occupiable: false },
  bookshelf: { def: 'la estantería', indef: 'una estantería',  occupiable: false },
  fireplace: { def: 'la chimenea',   indef: 'una chimenea',    occupiable: false },
  wardrobe:  { def: 'el armario',    indef: 'un armario',      occupiable: false },
  barrel:    { def: 'el barril',     indef: 'un barril',       occupiable: false },
  clock:     { def: 'el reloj',      indef: 'un reloj de pie', occupiable: false },
};

const OCCUPIABLE = Object.keys(OBJECT_TYPES).filter(k => OBJECT_TYPES[k].occupiable);
const PROPS      = Object.keys(OBJECT_TYPES).filter(k => !OBJECT_TYPES[k].occupiable);

// ─── Dificultad ─────────────────────────────────────────────────────────────
//  Cada tipo de pista tiene un "tier": 1 = fuerte/directa, 3 = débil/deductiva.
//  La dificultad decide qué tiers entran al pool de candidatas.

const TIER = {
  onObject: 1, inRoom: 1, row: 1, col: 1,
  nextToObject: 2, adjacentTo: 2, sameRoomAs: 2, corner: 2,
  leftOf: 3, rightOf: 3, above: 3, below: 3,
  notInRoom: 3, edge: 3, notEdge: 3, distance: 3, notSameRoomAs: 3,
};

export const DIFFICULTIES = {
  facil:   { label: 'Fácil',   tiers: [1, 2]    },
  normal:  { label: 'Normal',  tiers: [1, 2, 3] },
  dificil: { label: 'Difícil', tiers: [2, 3]    },
};

// ─── Utilidades ─────────────────────────────────────────────────────────────

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

const pick = arr => arr[Math.floor(Math.random() * arr.length)];
const key  = (r, c) => `${r},${c}`;

function neighbors(row, col, N) {
  return [[row - 1, col], [row + 1, col], [row, col - 1], [row, col + 1]]
    .filter(([r, c]) => r >= 0 && r < N && c >= 0 && c < N)
    .map(([r, c]) => ({ row: r, col: c }));
}

const manhattan = (a, b) => Math.abs(a.row - b.row) + Math.abs(a.col - b.col);

// ─── Habitaciones irregulares ───────────────────────────────────────────────
//  Crecimiento por regiones: se siembran K semillas separadas y cada región
//  absorbe casillas de su frontera, priorizando las más "encajadas" para que
//  las habitaciones queden compactas en vez de con tentáculos.

function growRooms(N, K) {
  const owner = Array(N * N).fill(-1);
  const idx = (r, c) => r * N + c;

  // Semillas: la primera al azar, las siguientes lo más lejos posible.
  const seeds = [{ row: Math.floor(Math.random() * N), col: Math.floor(Math.random() * N) }];
  while (seeds.length < K) {
    let best = null, bestDist = -1;
    for (let r = 0; r < N; r++) {
      for (let c = 0; c < N; c++) {
        const cell = { row: r, col: c };
        if (seeds.some(s => s.row === r && s.col === c)) continue;
        const d = Math.min(...seeds.map(s => manhattan(s, cell)));
        if (d > bestDist) { bestDist = d; best = cell; }
      }
    }
    seeds.push(best);
  }
  seeds.forEach((s, i) => { owner[idx(s.row, s.col)] = i; });

  const sizes = Array(K).fill(1);
  let remaining = N * N - K;

  while (remaining > 0) {
    // La región más chica crece primero → tamaños equilibrados.
    const order = [...Array(K).keys()].sort((a, b) => sizes[a] - sizes[b]);
    let grew = false;

    for (const region of order) {
      const frontier = [];
      for (let r = 0; r < N; r++) {
        for (let c = 0; c < N; c++) {
          if (owner[idx(r, c)] !== -1) continue;
          const touching = neighbors(r, c, N).filter(n => owner[idx(n.row, n.col)] === region).length;
          if (touching > 0) frontier.push({ row: r, col: c, touching });
        }
      }
      if (frontier.length === 0) continue;

      // Preferir la casilla con más vecinos ya en la región (compacidad).
      const maxTouch = Math.max(...frontier.map(f => f.touching));
      const best = pick(frontier.filter(f => f.touching === maxTouch));
      owner[idx(best.row, best.col)] = region;
      sizes[region]++;
      remaining--;
      grew = true;
      break;
    }
    if (!grew) break; // seguridad
  }

  return owner;
}

function createRooms(N, scenario) {
  const K = Math.min(N, scenario.rooms.length);
  const owner = growRooms(N, K);
  const defs = shuffle([...scenario.rooms]).slice(0, K);

  const rooms = defs.map((d, i) => ({
    id: `room_${i}`,
    name: d.name,
    color: d.color,
    floor: d.floor,
    cells: [],
  }));

  for (let r = 0; r < N; r++) {
    for (let c = 0; c < N; c++) {
      const o = owner[r * N + c];
      if (o >= 0) rooms[o].cells.push({ row: r, col: c });
    }
  }

  // El centroide sirve para poner la etiqueta de la habitación.
  for (const room of rooms) {
    const avgR = room.cells.reduce((s, c) => s + c.row, 0) / room.cells.length;
    const avgC = room.cells.reduce((s, c) => s + c.col, 0) / room.cells.length;
    let best = room.cells[0], bestD = Infinity;
    for (const cell of room.cells) {
      const d = Math.abs(cell.row - avgR) + Math.abs(cell.col - avgC);
      if (d < bestD) { bestD = d; best = cell; }
    }
    room.labelCell = best;
  }

  return rooms.filter(r => r.cells.length > 0);
}

// ─── Índices de consulta del tablero ────────────────────────────────────────

function buildBoard(rooms, objects, N) {
  const roomOf = new Map();
  for (const room of rooms)
    for (const cell of room.cells) roomOf.set(key(cell.row, cell.col), room);

  const objOf = new Map();
  for (const o of objects) objOf.set(key(o.row, o.col), o);

  const blocked = new Set();
  for (const o of objects)
    if (!OBJECT_TYPES[o.type].occupiable) blocked.add(key(o.row, o.col));

  return {
    N, rooms, objects,
    roomAt:  (r, c) => roomOf.get(key(r, c)) || null,
    objAt:   (r, c) => objOf.get(key(r, c)) || null,
    isBlocked: (r, c) => blocked.has(key(r, c)),
  };
}

// ─── Mobiliario ─────────────────────────────────────────────────────────────
//  Los props (no ocupables) se ponen SOLO en casillas que no usa nadie: son
//  restricciones duras y a la vez decoran. Los ocupables van en cualquier lado.

function placeObjects(rooms, solution, N) {
  const objects = [];
  const taken = new Set();
  const solSet = new Set(solution.map(p => key(p.row, p.col)));

  const free = [];
  for (let r = 0; r < N; r++)
    for (let c = 0; c < N; c++)
      if (!solSet.has(key(r, c))) free.push({ row: r, col: c });

  // Props bloqueantes: ~28% de las casillas libres, sin bloquear una habitación
  // entera ni dejar una fila/columna sin salida.
  const propTarget = Math.max(2, Math.round(free.length * 0.28));
  const propTypes = shuffle(PROPS);
  let pi = 0, propCount = 0;

  const roomIdOf = new Map();
  for (const room of rooms)
    for (const c of room.cells) roomIdOf.set(key(c.row, c.col), room.id);

  const rowBlocked  = new Array(N).fill(0);
  const colBlocked  = new Array(N).fill(0);
  const roomBlocked = new Map(rooms.map(r => [r.id, 0]));
  const roomSize    = new Map(rooms.map(r => [r.id, r.cells.length]));

  for (const cell of shuffle(free)) {
    if (propCount >= propTarget) break;

    // Ninguna habitación puede quedar completamente bloqueada, y ninguna fila
    // o columna puede perder tantas casillas que deje de admitir una persona.
    const rid = roomIdOf.get(key(cell.row, cell.col));
    if (roomBlocked.get(rid) >= roomSize.get(rid) - 1) continue;
    if (rowBlocked[cell.row] >= N - 2 || colBlocked[cell.col] >= N - 2) continue;

    objects.push({ type: propTypes[pi % propTypes.length], row: cell.row, col: cell.col });
    taken.add(key(cell.row, cell.col));
    rowBlocked[cell.row]++;
    colBlocked[cell.col]++;
    roomBlocked.set(rid, roomBlocked.get(rid) + 1);
    propCount++;
    pi++;
  }

  // Objetos ocupables: en algunas casillas de la solución (habilitan "estaba
  // sobre X") y en algunas libres restantes, para que no delaten la respuesta.
  const occSpots = shuffle([
    ...solution.filter(() => Math.random() < 0.7),
    ...free.filter(c => !taken.has(key(c.row, c.col))).filter(() => Math.random() < 0.35),
  ]);
  const occTypes = shuffle(OCCUPIABLE);
  occSpots.forEach((cell, i) => {
    if (taken.has(key(cell.row, cell.col))) return;
    objects.push({ type: occTypes[i % occTypes.length], row: cell.row, col: cell.col });
    taken.add(key(cell.row, cell.col));
  });

  return objects;
}

// ─── Generación de pistas candidatas ────────────────────────────────────────
//  Devuelve, para el sospechoso `i`, todas las pistas VERDADERAS respecto de
//  la solución. Cada pista lleva un `test(pos)` que el solver reevalúa sobre
//  asignaciones hipotéticas.

function candidateClues(i, solution, board, names, numSuspects, allowedTiers) {
  const { N } = board;
  const me = solution[i];
  const out = [];

  const add = (type, text, refs, test) => {
    if (!allowedTiers.includes(TIER[type])) return;
    out.push({ owner: i, type, text, refs, test });
  };

  // ── Unarias: dependen solo de la casilla del sospechoso ──────────────────

  const myObj = board.objAt(me.row, me.col);
  if (myObj && OBJECT_TYPES[myObj.type].occupiable) {
    const t = myObj.type;
    add('onObject', OBJECT_TYPES[t].on, [], pos => {
      const o = board.objAt(pos[i].row, pos[i].col);
      return !!o && o.type === t;
    });
  }

  const nearTypes = new Set(
    neighbors(me.row, me.col, N)
      .map(n => board.objAt(n.row, n.col))
      .filter(Boolean)
      .map(o => o.type)
  );
  for (const t of nearTypes) {
    add('nextToObject', `Estaba junto a ${OBJECT_TYPES[t].indef}`, [], pos =>
      neighbors(pos[i].row, pos[i].col, N).some(n => {
        const o = board.objAt(n.row, n.col);
        return !!o && o.type === t;
      })
    );
  }

  const myRoom = board.roomAt(me.row, me.col);
  add('inRoom', `Estaba en ${myRoom.name}`, [], pos =>
    board.roomAt(pos[i].row, pos[i].col)?.id === myRoom.id
  );

  for (const room of board.rooms) {
    if (room.id === myRoom.id) continue;
    add('notInRoom', `No estaba en ${room.name}`, [], pos =>
      board.roomAt(pos[i].row, pos[i].col)?.id !== room.id
    );
  }

  add('row', `Estaba en la fila ${me.row + 1}`, [], pos => pos[i].row === me.row);
  add('col', `Estaba en la columna ${me.col + 1}`, [], pos => pos[i].col === me.col);

  const isCorner = c => (c.row === 0 || c.row === N - 1) && (c.col === 0 || c.col === N - 1);
  const isEdge   = c => c.row === 0 || c.row === N - 1 || c.col === 0 || c.col === N - 1;

  if (isCorner(me)) add('corner', 'Estaba en una esquina del plano', [], pos => isCorner(pos[i]));
  if (isEdge(me))   add('edge', 'Estaba pegado a una pared exterior', [], pos => isEdge(pos[i]));
  else              add('notEdge', 'No estaba pegado a ninguna pared exterior', [], pos => !isEdge(pos[i]));

  // ── Binarias: relacionan al sospechoso con otra persona ──────────────────

  for (let j = 0; j <= numSuspects; j++) {
    if (j === i) continue;
    const other = solution[j];
    const name = names[j];

    if (me.col < other.col)
      add('leftOf', `Estaba a la izquierda de ${name}`, [j], pos => pos[i].col < pos[j].col);
    if (me.col > other.col)
      add('rightOf', `Estaba a la derecha de ${name}`, [j], pos => pos[i].col > pos[j].col);
    if (me.row < other.row)
      add('above', `Estaba más al norte que ${name}`, [j], pos => pos[i].row < pos[j].row);
    if (me.row > other.row)
      add('below', `Estaba más al sur que ${name}`, [j], pos => pos[i].row > pos[j].row);

    if (manhattan(me, other) === 1)
      add('adjacentTo', `Estaba justo al lado de ${name}`, [j], pos => manhattan(pos[i], pos[j]) === 1);

    // Solo distancias cortas: "a 11 casillas" es cierto pero no ayuda a deducir
    // y suena raro; entre 2 y 4 la pista sigue siendo manejable mentalmente.
    const d = manhattan(me, other);
    if (d >= 2 && d <= 4)
      add('distance', `Estaba a ${d} casillas de ${name}`, [j], pos => manhattan(pos[i], pos[j]) === d);

    const otherRoom = board.roomAt(other.row, other.col);
    if (otherRoom.id === myRoom.id)
      add('sameRoomAs', `Compartía habitación con ${name}`, [j], pos =>
        board.roomAt(pos[i].row, pos[i].col)?.id === board.roomAt(pos[j].row, pos[j].col)?.id
      );
    else
      add('notSameRoomAs', `No compartía habitación con ${name}`, [j], pos =>
        board.roomAt(pos[i].row, pos[i].col)?.id !== board.roomAt(pos[j].row, pos[j].col)?.id
      );
  }

  return out;
}

// ─── Solver ─────────────────────────────────────────────────────────────────
//  Backtracking sobre los sospechosos con poda de N-torres. La víctima queda
//  determinada por la fila y columna libres, así que las pistas que la
//  referencian se evalúan en la hoja del árbol.

function countSolutions(clues, board, numSuspects, limit = 2) {
  const { N } = board;
  const V = numSuspects;              // índice de la víctima
  const pos = new Array(V + 1).fill(null);

  const unary = Array.from({ length: numSuspects }, () => []);
  const binary = [];
  for (const c of clues) (c.refs.length === 0 ? unary[c.owner] : binary).push(c);

  // Dominio de cada sospechoso: las casillas que sus pistas unarias permiten.
  // Recorta la búsqueda de N² a unas pocas casillas por persona.
  const domain = [];
  for (let i = 0; i < numSuspects; i++) {
    const cells = [];
    for (let r = 0; r < N; r++) {
      for (let c = 0; c < N; c++) {
        if (board.isBlocked(r, c)) continue;
        pos[i] = { row: r, col: c };
        if (unary[i].every(cl => cl.test(pos))) cells.push({ row: r, col: c });
      }
    }
    pos[i] = null;
    if (cells.length === 0) return 0;
    domain.push(cells);
  }

  // Primero el sospechoso más restringido: poda el árbol mucho antes.
  const order = [...Array(numSuspects).keys()].sort((a, b) => domain[a].length - domain[b].length);
  const depthOf = new Map(order.map((s, d) => [s, d]));
  depthOf.set(V, numSuspects);

  // Cada pista binaria se comprueba en cuanto sus dos extremos están puestos.
  const byDepth = Array.from({ length: numSuspects + 1 }, () => []);
  for (const c of binary) {
    const d = Math.max(depthOf.get(c.owner), ...c.refs.map(r => depthOf.get(r)));
    byDepth[d].push(c);
  }

  const usedRows = new Set(), usedCols = new Set();
  let count = 0;

  function rec(d) {
    if (count >= limit) return;

    if (d === numSuspects) {
      let vr = -1, vc = -1;
      for (let r = 0; r < N; r++) if (!usedRows.has(r)) vr = r;
      for (let c = 0; c < N; c++) if (!usedCols.has(c)) vc = c;
      if (board.isBlocked(vr, vc)) return;          // la víctima no cabe ahí
      pos[V] = { row: vr, col: vc };
      if (byDepth[numSuspects].every(c => c.test(pos))) count++;
      pos[V] = null;
      return;
    }

    const i = order[d];
    for (const cell of domain[i]) {
      if (usedRows.has(cell.row) || usedCols.has(cell.col)) continue;
      pos[i] = cell;
      usedRows.add(cell.row); usedCols.add(cell.col);
      if (byDepth[d].every(c => c.test(pos))) rec(d + 1);
      usedRows.delete(cell.row); usedCols.delete(cell.col);
      pos[i] = null;
      if (count >= limit) return;
    }
  }

  rec(0);
  return count;
}

// ─── Selección del conjunto de pistas ───────────────────────────────────────
//
//  Añadir pistas nunca aumenta el número de soluciones, así que:
//    · Si el pool COMPLETO no deja una solución única, ningún subconjunto lo
//      hará → el tablero es irrecuperable y se descarta de entrada.
//    · Si el pool completo sí la deja, reforzar de a una pista termina siempre,
//      porque en el peor caso se acaban añadiendo todas.
//  Eso convierte la selección en un proceso que no puede fallar a mitad de
//  camino, sin presupuestos de reintentos ni constantes que calibrar.

function selectClues(pools, board, numSuspects) {
  if (pools.some(p => p.length === 0)) return null;
  if (countSolutions(pools.flat(), board, numSuspects) !== 1) return null;

  const chosen = pools.map(p => [pick(p)]);
  const flat = () => chosen.flat();

  while (countSolutions(flat(), board, numSuspects) > 1) {
    // Reforzar al sospechoso que menos pistas tenga.
    const order = [...Array(numSuspects).keys()].sort((a, b) => chosen[a].length - chosen[b].length);
    for (const i of order) {
      const fresh = pools[i].filter(c => !chosen[i].includes(c));
      if (fresh.length === 0) continue;

      // Preferir un tipo que este sospechoso todavía no tenga, para que su
      // ficha no acabe siendo tres variantes de lo mismo.
      const usedTypes = new Set(chosen[i].map(c => c.type));
      const novel = fresh.filter(c => !usedTypes.has(c.type));
      const sample = shuffle(novel.length ? novel : fresh).slice(0, 5);

      // Quedarse con la primera candidata que ya deje la solución única:
      // converge con bastantes menos pistas que eligiendo al azar.
      let winner = null;
      for (const c of sample) {
        chosen[i].push(c);
        const unique = countSolutions(flat(), board, numSuspects) === 1;
        chosen[i].pop();
        if (unique) { winner = c; break; }
      }
      chosen[i].push(winner || sample[0]);
      break;
    }
  }

  // Poda: quitar pistas redundantes para que el puzzle quede elegante.
  const removable = shuffle(chosen.flatMap((list, i) => list.map(c => ({ i, c }))));
  for (const { i, c } of removable) {
    if (chosen[i].length <= 1) continue;
    const backup = chosen[i];
    chosen[i] = chosen[i].filter(x => x !== c);
    if (countSolutions(flat(), board, numSuspects) !== 1) chosen[i] = backup;
  }

  return chosen;
}

// ─── API pública ────────────────────────────────────────────────────────────

export function canPlaceAt(puzzle, row, col) {
  return !puzzle.blockedSet.has(key(row, col));
}

export function generatePuzzle(numSuspects = 3, scenarioId = null, difficulty = 'normal') {
  const N = numSuspects + 1;
  const diff = DIFFICULTIES[difficulty] || DIFFICULTIES.normal;
  const scenario = SCENARIOS.find(s => s.id === scenarioId) || pick(SCENARIOS);

  for (let attempt = 0; attempt < 400; attempt++) {
    const rooms = createRooms(N, scenario);

    // Colocación N-torres: la permutación de columnas garantiza fila y
    // columna únicas para cada persona.
    const cols = shuffle([...Array(N).keys()]);
    const solution = cols.map((col, row) => ({ row, col }));

    // El asesino tiene que quedar determinado sin ambigüedad: la habitación de
    // la víctima debe contener EXACTAMENTE un sospechoso. Con habitaciones
    // irregulares eso no está garantizado, así que se descarta la colocación
    // antes de gastar trabajo en el mobiliario y las pistas.
    const roomIdOf = new Map();
    for (const room of rooms)
      for (const c of room.cells) roomIdOf.set(key(c.row, c.col), room.id);

    const victimPos = solution[numSuspects];
    const victimRoomId = roomIdOf.get(key(victimPos.row, victimPos.col));
    const inVictimRoom = solution
      .slice(0, numSuspects)
      .filter(p => roomIdOf.get(key(p.row, p.col)) === victimRoomId);
    if (inVictimRoom.length !== 1) continue;

    const objects = placeObjects(rooms, solution, N);
    const board = buildBoard(rooms, objects, N);

    // La solución no puede caer sobre una casilla bloqueada (no debería pasar,
    // porque los props solo van en casillas libres, pero se verifica).
    if (solution.some(p => board.isBlocked(p.row, p.col))) continue;

    const suspectNames = shuffle(SUSPECT_NAMES).slice(0, numSuspects);
    // Las pistas relacionales nombran a la víctima, así que su nombre no puede
    // coincidir con el de ningún sospechoso o la pista sería ambigua.
    const victimName = pick(VICTIM_NAMES.filter(v => !suspectNames.includes(v)));
    const names = [...suspectNames, victimName];

    const pools = [];
    for (let i = 0; i < numSuspects; i++)
      pools.push(candidateClues(i, solution, board, names, numSuspects, diff.tiers));

    const chosen = selectClues(pools, board, numSuspects);
    if (!chosen) continue;   // el tablero no admite unicidad con estas pistas

    const suspects = suspectNames.map((name, i) => ({
      id: `s${i}`,
      name,
      row: solution[i].row,
      col: solution[i].col,
      roomId: board.roomAt(solution[i].row, solution[i].col).id,
      clues: chosen[i].map(c => c.text),
    }));

    // Garantizado por la comprobación de arriba: hay uno y solo uno.
    const murderer = suspects.find(s => s.roomId === victimRoomId);

    return {
      N,
      rooms,
      objects,
      suspects,
      scenario,
      difficulty,
      // Restricciones con su predicado, para poder reverificar la unicidad
      // desde fuera (tests). La UI solo consume `suspects[].clues`.
      constraints: chosen.flat(),
      blockedSet: new Set(objects.filter(o => !OBJECT_TYPES[o.type].occupiable).map(o => key(o.row, o.col))),
      victim: {
        name: victimName,
        row: victimPos.row,
        col: victimPos.col,
        roomId: victimRoomId,
        clue: 'Apareció en la única casilla que quedó libre',
      },
      murderer: murderer.id,
    };
  }

  // Si la dificultad pedida no converge, se afloja antes de rendirse.
  if (difficulty === 'dificil') return generatePuzzle(numSuspects, scenarioId, 'normal');
  if (difficulty === 'normal')  return generatePuzzle(numSuspects, scenarioId, 'facil');
  return generatePuzzle(numSuspects, null, 'facil');
}

export function checkPlacement(puzzle, userPlacements) {
  const placed = Object.values(userPlacements).filter(Boolean);
  const rows = placed.map(p => p.row);
  const cols = placed.map(p => p.col);
  if (new Set(rows).size !== rows.length) return { valid: false };
  if (new Set(cols).size !== cols.length) return { valid: false };
  return { valid: true };
}

export function isCorrectSolution(puzzle, userPlacements) {
  return puzzle.suspects.every(s => {
    const p = userPlacements[s.id];
    return p && p.row === s.row && p.col === s.col;
  });
}

// Devuelve un sospechoso todavía sin colocar (o mal colocado) para la pista.
export function getHint(puzzle, userPlacements) {
  const wrong = puzzle.suspects.filter(s => {
    const p = userPlacements[s.id];
    return !p || p.row !== s.row || p.col !== s.col;
  });
  if (wrong.length === 0) return null;
  return pick(wrong);
}
