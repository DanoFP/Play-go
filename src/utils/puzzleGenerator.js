// ═══════════════════════════════════════════════════════════════════════════
//  MURDOKU — generador de puzzles con cadena de deducción garantizada
// ═══════════════════════════════════════════════════════════════════════════
//
//  Reglas (siguiendo la especificación del Murdoku original):
//    · La escena es una grilla H×W, no necesariamente cuadrada, y puede tener
//      celdas recortadas para que la planta no sea un rectángulo.
//    · El número de personas es min(H, W): P−1 sospechosos y la víctima.
//    · Cada persona ocupa una fila y una columna únicas.
//    · El mobiliario no ocupable bloquea la casilla para todos.
//    · El asesino es el único sospechoso que comparte habitación con la víctima.
//
//  El generador no se conforma con que la solución sea única: exige que sea
//  alcanzable por DEDUCCIÓN PURA, sin probar y descartar. Un solver lógico
//  propaga restricciones como lo haría una persona y devuelve en qué ronda
//  queda fijada cada persona. Con eso:
//    · Se garantiza al menos un "ancla": alguien deducible solo con su ficha.
//    · Las fichas se ordenan por esa secuencia, así la primera es la más fácil
//      y cada una se abre con lo que resolvieron las anteriores.

// ─── Nombres ────────────────────────────────────────────────────────────────

const SUSPECT_NAMES = [
  'Cora', 'Bella', 'Axel', 'Douglas', 'Sofía', 'Marco', 'Lena', 'Hugo',
  'Rita', 'Diego', 'Nora', 'Iván', 'Clara', 'Emil', 'Vera',
];
const VICTIM_NAMES = ['Vincent', 'Victor', 'Valentina', 'Vito', 'Verena'];

// ─── Escenarios ─────────────────────────────────────────────────────────────

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
//  tier 1 = ancla (fija por sí sola), 2 = media, 3 = relativa/deductiva.
//  La guía de estrategia del Murdoku recomienda resolver primero habitación
//  fija, fila/columna fija y objeto único, y dejar las relativas para el final;
//  los tiers reproducen esa jerarquía.

const TIER = {
  onObject: 1, inRoom: 1, row: 1, col: 1,
  nextToObject: 2, adjacentTo: 2, sameRoomAs: 2, corner: 2,
  leftOf: 3, rightOf: 3, above: 3, below: 3,
  notInRoom: 3, edge: 3, notEdge: 3, distance: 3, notSameRoomAs: 3,
};

//  `tiers` son las pistas que recibe la mayoría; `anchorTiers`, las que recibe
//  la persona que abre el caso. Separarlas es lo que hace posible el difícil:
//  si nadie puede tener una pista fuerte no hay punto de partida y el puzzle
//  solo se resuelve adivinando. Un Murdoku difícil tiene un arranque claro y
//  todo lo demás relacional, no un arranque imposible.
export const DIFFICULTIES = {
  facil:   { label: 'Fácil',   tiers: [1, 2],    anchorTiers: [1, 2]    },
  normal:  { label: 'Normal',  tiers: [1, 2, 3], anchorTiers: [1, 2, 3] },
  dificil: { label: 'Difícil', tiers: [2, 3],    anchorTiers: [1, 2, 3] },
};

// ─── Aleatoriedad sembrada ──────────────────────────────────────────────────
//  Todo el generador consume este rng, así que un mismo número de caso produce
//  siempre el mismo puzzle: se puede compartir y reproducir.

function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

let rng = Math.random;

// ─── Utilidades ─────────────────────────────────────────────────────────────

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

const pick = arr => arr[Math.floor(rng() * arr.length)];
const key  = (r, c) => `${r},${c}`;
const manhattan = (a, b) => Math.abs(a.row - b.row) + Math.abs(a.col - b.col);

function neighbors(row, col, live) {
  return [[row - 1, col], [row + 1, col], [row, col - 1], [row, col + 1]]
    .filter(([r, c]) => live.has(key(r, c)))
    .map(([r, c]) => ({ row: r, col: c }));
}

// ─── Forma de la planta ─────────────────────────────────────────────────────
//  El Murdoku original define la grilla como n×m con min(n,m) personas y
//  permite borrar celdas para que la planta no sea rectangular.

function chooseDimensions(P) {
  // min(H, W) === P para que entren exactamente P personas.
  const options = [
    { H: P,     W: P     },   // cuadrada
    { H: P,     W: P + 1 },   // apaisada
    { H: P,     W: P + 2 },
    { H: P + 1, W: P     },   // vertical
    { H: P + 2, W: P     },
  ];
  // Las cuadradas siguen siendo las más frecuentes, pero no las únicas.
  return pick([options[0], options[0], ...options.slice(1)]);
}

function isConnected(live, H, W) {
  const start = [...live][0];
  if (!start) return false;
  const seen = new Set([start]);
  const stack = [start];
  while (stack.length) {
    const [r, c] = stack.pop().split(',').map(Number);
    for (const n of neighbors(r, c, live)) {
      const k = key(n.row, n.col);
      if (!seen.has(k)) { seen.add(k); stack.push(k); }
    }
  }
  return seen.size === live.size;
}

function carveShape(H, W, P) {
  const live = new Set();
  for (let r = 0; r < H; r++) for (let c = 0; c < W; c++) live.add(key(r, c));
  if (Math.min(H, W) < 5) return live;             // las chicas quedan enteras

  const style = pick(['full', 'full', 'notch', 'courtyard', 'corners']);
  const drop = [];

  if (style === 'notch') {
    // Muerde una esquina: la planta queda en L.
    const r0 = pick([0, H - 2]), c0 = pick([0, W - 2]);
    drop.push([r0, c0], [r0, c0 + 1], [r0 + 1, c0]);
  } else if (style === 'courtyard') {
    // Patio interior.
    const r0 = 1 + Math.floor(rng() * (H - 3));
    const c0 = 1 + Math.floor(rng() * (W - 3));
    drop.push([r0, c0], [r0, c0 + 1]);
  } else if (style === 'corners') {
    drop.push([0, 0], [H - 1, W - 1]);
  }

  for (const [r, c] of drop) live.delete(key(r, c));

  // Cada fila y cada columna tiene que conservar celdas suficientes, y la
  // planta tiene que seguir siendo de una pieza.
  for (let r = 0; r < H; r++) {
    let n = 0;
    for (let c = 0; c < W; c++) if (live.has(key(r, c))) n++;
    if (n < 2) return carveShape(H, W, P);
  }
  for (let c = 0; c < W; c++) {
    let n = 0;
    for (let r = 0; r < H; r++) if (live.has(key(r, c))) n++;
    if (n < 2) return carveShape(H, W, P);
  }
  if (!isConnected(live, H, W)) return carveShape(H, W, P);
  return live;
}

// ─── Habitaciones ───────────────────────────────────────────────────────────

function growRooms(live, K) {
  const cells = [...live].map(k => { const [r, c] = k.split(',').map(Number); return { row: r, col: c }; });
  const owner = new Map();

  const seeds = [pick(cells)];
  while (seeds.length < K) {
    let best = null, bestDist = -1;
    for (const cell of cells) {
      if (seeds.some(s => s.row === cell.row && s.col === cell.col)) continue;
      const d = Math.min(...seeds.map(s => manhattan(s, cell)));
      if (d > bestDist) { bestDist = d; best = cell; }
    }
    if (!best) break;
    seeds.push(best);
  }
  seeds.forEach((s, i) => owner.set(key(s.row, s.col), i));

  const sizes = seeds.map(() => 1);
  let remaining = cells.length - seeds.length;

  while (remaining > 0) {
    const order = [...seeds.keys()].sort((a, b) => sizes[a] - sizes[b]);
    let grew = false;
    for (const region of order) {
      const frontier = [];
      for (const cell of cells) {
        if (owner.has(key(cell.row, cell.col))) continue;
        const touching = neighbors(cell.row, cell.col, live)
          .filter(n => owner.get(key(n.row, n.col)) === region).length;
        if (touching > 0) frontier.push({ ...cell, touching });
      }
      if (!frontier.length) continue;
      const maxTouch = Math.max(...frontier.map(f => f.touching));
      const best = pick(frontier.filter(f => f.touching === maxTouch));
      owner.set(key(best.row, best.col), region);
      sizes[region]++; remaining--; grew = true;
      break;
    }
    if (!grew) break;
  }

  return owner;
}

function createRooms(live, scenario, K) {
  const owner = growRooms(live, K);
  const defs = shuffle([...scenario.rooms]).slice(0, K);

  const rooms = defs.map((d, i) => ({
    id: `room_${i}`, name: d.name, color: d.color, floor: d.floor, cells: [],
  }));

  for (const [k, region] of owner) {
    const [r, c] = k.split(',').map(Number);
    if (rooms[region]) rooms[region].cells.push({ row: r, col: c });
  }

  for (const room of rooms) {
    if (!room.cells.length) continue;
    const avgR = room.cells.reduce((s, c) => s + c.row, 0) / room.cells.length;
    const avgC = room.cells.reduce((s, c) => s + c.col, 0) / room.cells.length;
    room.labelCell = room.cells.reduce((best, cell) =>
      Math.abs(cell.row - avgR) + Math.abs(cell.col - avgC) <
      Math.abs(best.row - avgR) + Math.abs(best.col - avgC) ? cell : best, room.cells[0]);
  }

  return rooms.filter(r => r.cells.length > 0);
}

// ─── Índice del tablero ─────────────────────────────────────────────────────

function buildBoard(rooms, objects, live, H, W) {
  const roomOf = new Map();
  for (const room of rooms)
    for (const cell of room.cells) roomOf.set(key(cell.row, cell.col), room);

  const objOf = new Map();
  for (const o of objects) objOf.set(key(o.row, o.col), o);

  const blocked = new Set(
    objects.filter(o => !OBJECT_TYPES[o.type].occupiable).map(o => key(o.row, o.col))
  );

  const playable = [];
  for (const k of live) {
    if (blocked.has(k)) continue;
    const [r, c] = k.split(',').map(Number);
    playable.push({ row: r, col: c });
  }

  return {
    H, W, live, rooms, objects, playable,
    roomAt: (r, c) => roomOf.get(key(r, c)) || null,
    objAt:  (r, c) => objOf.get(key(r, c)) || null,
    isBlocked: (r, c) => blocked.has(key(r, c)),
  };
}

// ─── Colocación ─────────────────────────────────────────────────────────────

function pickPlacement(H, W, P, live) {
  for (let tries = 0; tries < 60; tries++) {
    const rows = shuffle([...Array(H).keys()]).slice(0, P);
    const cols = shuffle([...Array(W).keys()]).slice(0, P);
    const spots = rows.map((r, i) => ({ row: r, col: cols[i] }));
    if (spots.every(s => live.has(key(s.row, s.col)))) return spots;
  }
  return null;
}

function placeObjects(rooms, solution, live, H, W) {
  const objects = [];
  const taken = new Set();
  const solSet = new Set(solution.map(p => key(p.row, p.col)));

  const free = [];
  for (const k of live) if (!solSet.has(k)) {
    const [r, c] = k.split(',').map(Number);
    free.push({ row: r, col: c });
  }

  const roomIdOf = new Map();
  for (const room of rooms)
    for (const c of room.cells) roomIdOf.set(key(c.row, c.col), room.id);

  // Cuántas celdas vivas tiene cada fila, columna y habitación: bloquear no
  // puede dejar ninguna de ellas sin sitio para una persona.
  const liveInRow = new Array(H).fill(0);
  const liveInCol = new Array(W).fill(0);
  const liveInRoom = new Map(rooms.map(r => [r.id, r.cells.length]));
  for (const k of live) {
    const [r, c] = k.split(',').map(Number);
    liveInRow[r]++; liveInCol[c]++;
  }

  const blkRow = new Array(H).fill(0);
  const blkCol = new Array(W).fill(0);
  const blkRoom = new Map(rooms.map(r => [r.id, 0]));

  const propTarget = Math.max(2, Math.round(free.length * 0.28));
  const propTypes = shuffle(PROPS);
  let propCount = 0, pi = 0;

  for (const cell of shuffle(free)) {
    if (propCount >= propTarget) break;
    const rid = roomIdOf.get(key(cell.row, cell.col));
    if (blkRoom.get(rid) >= liveInRoom.get(rid) - 1) continue;
    if (blkRow[cell.row] >= liveInRow[cell.row] - 1) continue;
    if (blkCol[cell.col] >= liveInCol[cell.col] - 1) continue;

    objects.push({ type: propTypes[pi % propTypes.length], row: cell.row, col: cell.col });
    taken.add(key(cell.row, cell.col));
    blkRow[cell.row]++; blkCol[cell.col]++;
    blkRoom.set(rid, blkRoom.get(rid) + 1);
    propCount++; pi++;
  }

  const occSpots = shuffle([
    ...solution.filter(() => rng() < 0.7),
    ...free.filter(c => !taken.has(key(c.row, c.col))).filter(() => rng() < 0.3),
  ]);
  const occTypes = shuffle(OCCUPIABLE);
  occSpots.forEach((cell, i) => {
    if (taken.has(key(cell.row, cell.col))) return;
    objects.push({ type: occTypes[i % occTypes.length], row: cell.row, col: cell.col });
    taken.add(key(cell.row, cell.col));
  });

  return objects;
}

// ─── Pistas ─────────────────────────────────────────────────────────────────
//  Una pista es DATO puro: {owner, refs, type, text, target?, value?}. No lleva
//  closures, así que viaja al worker por structuredClone y el hilo principal
//  puede reevaluarla para detectar contradicciones.

// Índice consultable del tablero. `makeIndex` lo reconstruye a partir de un
// puzzle ya serializado, para poder evaluar pistas fuera del generador.
export function makeIndex(puzzle) {
  const roomOf = new Map(), objOf = new Map();
  for (const room of puzzle.rooms)
    for (const c of room.cells) roomOf.set(key(c.row, c.col), room);
  for (const o of puzzle.objects) objOf.set(key(o.row, o.col), o);
  return {
    live: puzzle.live,
    rooms: puzzle.rooms,
    roomAt: (r, c) => roomOf.get(key(r, c)) || null,
    objAt:  (r, c) => objOf.get(key(r, c)) || null,
  };
}

// `pos` es un array indexado por persona. Devuelve true si la pista todavía no
// es evaluable (falta alguien), para poder usarla sobre estados parciales.
export function evalClue(clue, pos, board) {
  const me = pos[clue.owner];
  if (!me) return true;
  const openSides = p => neighbors(p.row, p.col, board.live).length;

  switch (clue.type) {
    case 'onObject': {
      const o = board.objAt(me.row, me.col);
      return !!o && o.type === clue.target;
    }
    case 'nextToObject':
      return neighbors(me.row, me.col, board.live).some(n => {
        const o = board.objAt(n.row, n.col);
        return !!o && o.type === clue.target;
      });
    case 'inRoom':    return board.roomAt(me.row, me.col)?.id === clue.target;
    case 'notInRoom': return board.roomAt(me.row, me.col)?.id !== clue.target;
    case 'row':       return me.row === clue.value;
    case 'col':       return me.col === clue.value;
    case 'corner':    return openSides(me) <= 2;
    case 'edge':      return openSides(me) < 4;
    case 'notEdge':   return openSides(me) === 4;
    default: break;
  }

  const other = pos[clue.refs[0]];
  if (!other) return true;
  switch (clue.type) {
    case 'leftOf':     return me.col < other.col;
    case 'rightOf':    return me.col > other.col;
    case 'above':      return me.row < other.row;
    case 'below':      return me.row > other.row;
    case 'adjacentTo': return manhattan(me, other) === 1;
    case 'distance':   return manhattan(me, other) === clue.value;
    case 'sameRoomAs':
      return board.roomAt(me.row, me.col)?.id === board.roomAt(other.row, other.col)?.id;
    case 'notSameRoomAs':
      return board.roomAt(me.row, me.col)?.id !== board.roomAt(other.row, other.col)?.id;
    default: return true;
  }
}

// Todas las candidatas son ciertas respecto de la solución.
function candidateClues(i, solution, board, names, P, allowedTiers) {
  const { live } = board;
  const me = solution[i];
  const out = [];

  const add = (type, text, refs, extra = {}) => {
    if (!allowedTiers.includes(TIER[type])) return;
    out.push({ owner: i, type, text, refs, ...extra });
  };

  // ── Anclas y unarias ────────────────────────────────────────────────────

  const myObj = board.objAt(me.row, me.col);
  if (myObj && OBJECT_TYPES[myObj.type].occupiable)
    add('onObject', OBJECT_TYPES[myObj.type].on, [], { target: myObj.type });

  const nearTypes = new Set(
    neighbors(me.row, me.col, live).map(n => board.objAt(n.row, n.col)).filter(Boolean).map(o => o.type)
  );
  for (const t of nearTypes)
    add('nextToObject', `Estaba junto a ${OBJECT_TYPES[t].indef}`, [], { target: t });

  const myRoom = board.roomAt(me.row, me.col);
  add('inRoom', `Estaba en ${myRoom.name}`, [], { target: myRoom.id });

  for (const room of board.rooms) {
    if (room.id === myRoom.id) continue;
    add('notInRoom', `No estaba en ${room.name}`, [], { target: room.id });
  }

  add('row', `Estaba en la fila ${me.row + 1}`, [], { value: me.row });
  add('col', `Estaba en la columna ${me.col + 1}`, [], { value: me.col });

  // Rincón y pared se calculan sobre la planta real, no sobre el rectángulo:
  // una celda es de borde si le falta algún vecino vivo.
  const sides = neighbors(me.row, me.col, live).length;
  if (sides <= 2) add('corner', 'Estaba en un rincón del plano', []);
  if (sides < 4)  add('edge', 'Estaba pegado a una pared exterior', []);
  else            add('notEdge', 'No estaba pegado a ninguna pared exterior', []);

  // ── Relativas ───────────────────────────────────────────────────────────

  for (let j = 0; j < P; j++) {
    if (j === i) continue;
    const other = solution[j];
    const name = names[j];

    if (me.col < other.col) add('leftOf',  `Estaba a la izquierda de ${name}`, [j]);
    if (me.col > other.col) add('rightOf', `Estaba a la derecha de ${name}`,   [j]);
    if (me.row < other.row) add('above',   `Estaba más al norte que ${name}`,  [j]);
    if (me.row > other.row) add('below',   `Estaba más al sur que ${name}`,    [j]);

    const d = manhattan(me, other);
    if (d === 1) add('adjacentTo', `Estaba justo al lado de ${name}`, [j]);
    if (d >= 2 && d <= 4) add('distance', `Estaba a ${d} casillas de ${name}`, [j], { value: d });

    const otherRoom = board.roomAt(other.row, other.col);
    if (otherRoom.id === myRoom.id)
      add('sameRoomAs', `Compartía habitación con ${name}`, [j]);
    else
      add('notSameRoomAs', `No compartía habitación con ${name}`, [j]);
  }

  return out;
}

// ─── Solver lógico ──────────────────────────────────────────────────────────
//  Propaga restricciones como lo haría una persona, sin probar y descartar:
//    1. Recorta cada dominio con las pistas propias (ancla si queda una sola).
//    2. Cada persona ya fijada elimina su fila y su columna del resto.
//    3. Consistencia de arco en las pistas relativas: una casilla sobrevive
//       solo si existe alguna casilla compatible para la persona referida.
//  Devuelve en qué ronda quedó fijada cada persona. Ronda 1 = ancla.

function solveLogically(clues, board, P) {
  let cand = Array.from({ length: P }, () => board.playable.slice());
  const pos = new Array(P).fill(null);
  const resolvedAt = new Array(P).fill(-1);

  const unary = Array.from({ length: P }, () => []);
  const binary = [];
  for (const c of clues) (c.refs.length === 0 ? unary[c.owner] : binary).push(c);

  for (let i = 0; i < P; i++) {
    cand[i] = cand[i].filter(cell => {
      pos[i] = cell;
      const ok = unary[i].every(cl => evalClue(cl, pos, board));
      pos[i] = null;
      return ok;
    });
    if (cand[i].length === 0) return { solved: false };
  }

  let round = 1;
  for (let i = 0; i < P; i++) if (cand[i].length === 1) resolvedAt[i] = 1;

  let changed = true;
  while (changed && round < 40) {
    changed = false;
    round++;

    // Una persona fijada libera su fila y su columna para las demás.
    for (let i = 0; i < P; i++) {
      if (cand[i].length !== 1) continue;
      const c0 = cand[i][0];
      for (let k = 0; k < P; k++) {
        if (k === i) continue;
        const before = cand[k].length;
        cand[k] = cand[k].filter(x => x.row !== c0.row && x.col !== c0.col);
        if (cand[k].length !== before) changed = true;
      }
    }

    for (const cl of binary) {
      const i = cl.owner, j = cl.refs[0];
      const compatible = (ci, cj) => {
        pos[i] = ci; pos[j] = cj;
        const ok = evalClue(cl, pos, board);
        pos[i] = null; pos[j] = null;
        return ok;
      };
      const bi = cand[i].length, bj = cand[j].length;
      cand[i] = cand[i].filter(ci => cand[j].some(cj => compatible(ci, cj)));
      cand[j] = cand[j].filter(cj => cand[i].some(ci => compatible(ci, cj)));
      if (cand[i].length !== bi || cand[j].length !== bj) changed = true;
    }

    if (cand.some(c => c.length === 0)) return { solved: false };
    for (let i = 0; i < P; i++) if (resolvedAt[i] === -1 && cand[i].length === 1) resolvedAt[i] = round;
  }

  return { solved: cand.every(c => c.length === 1), resolvedAt, depth: Math.max(...resolvedAt) };
}

// ─── Selección de pistas ────────────────────────────────────────────────────
//  Añadir pistas solo puede recortar dominios, así que la resolubilidad lógica
//  es monótona: si el pool completo no basta, ningún subconjunto bastará y el
//  tablero se descarta; si basta, reforzar de a una pista termina siempre.

function selectClues(pools, board, P) {
  if (pools.some(p => p.length === 0)) return null;
  if (!solveLogically(pools.flat(), board, P).solved) return null;

  const chosen = pools.map(p => [pick(p)]);
  const flat = () => chosen.flat();

  while (!solveLogically(flat(), board, P).solved) {
    const order = [...Array(P).keys()].sort((a, b) => chosen[a].length - chosen[b].length);
    let added = false;
    for (const i of order) {
      const fresh = pools[i].filter(c => !chosen[i].includes(c));
      if (!fresh.length) continue;

      const usedTypes = new Set(chosen[i].map(c => c.type));
      const novel = fresh.filter(c => !usedTypes.has(c.type));
      const sample = shuffle(novel.length ? novel : fresh).slice(0, 5);

      let winner = null;
      for (const c of sample) {
        chosen[i].push(c);
        const ok = solveLogically(flat(), board, P).solved;
        chosen[i].pop();
        if (ok) { winner = c; break; }
      }
      chosen[i].push(winner || sample[0]);
      added = true;
      break;
    }
    if (!added) return null;
  }

  // Poda: quitar lo redundante manteniendo la resolubilidad lógica.
  const removable = shuffle(chosen.flatMap((list, i) => list.map(c => ({ i, c }))));
  for (const { i, c } of removable) {
    if (chosen[i].length <= 1) continue;
    const backup = chosen[i];
    chosen[i] = chosen[i].filter(x => x !== c);
    if (!solveLogically(flat(), board, P).solved) chosen[i] = backup;
  }

  return chosen;
}

// ─── API pública ────────────────────────────────────────────────────────────

export function canPlaceAt(puzzle, row, col) {
  return puzzle.live.has(key(row, col)) && !puzzle.blockedSet.has(key(row, col));
}

export function generatePuzzle(numSuspects = 3, scenarioId = null, difficulty = 'normal', seed = null) {
  const P = numSuspects + 1;                 // sospechosos + víctima
  const diff = DIFFICULTIES[difficulty] || DIFFICULTIES.normal;

  // El caso queda identificado por su semilla: el mismo número reproduce
  // exactamente el mismo puzzle, así se puede compartir.
  const caseSeed = seed ?? (Math.floor(Math.random() * 0xFFFFFF) + 1);
  rng = mulberry32(caseSeed);

  // El tiro se consume siempre, haya escenario explícito o no: si solo se
  // tirase en un caso, la misma semilla daría secuencias distintas según cómo
  // se pidió el puzzle y el número de caso dejaría de identificarlo.
  const rolled = pick(SCENARIOS);
  const scenario = SCENARIOS.find(s => s.id === scenarioId) || rolled;

  // Una escalera perfecta —cada persona deducible en su propia ronda— es
  // demasiado rara para exigirla: la probabilidad cae a plomo al crecer P y la
  // generación no termina. En su lugar se buscan varios puzzles válidos y se
  // devuelve el de mejor escalera, saliendo antes si aparece una perfecta.
  let best = null;
  let found = 0;

  // Con P grande una escalera perfecta es rarísima y buscarla dispara el
  // tiempo, así que a partir de 6 personas se acepta un escalón de holgura.
  const goodEnough = P <= 5 ? P : P - 1;
  const wanted = P <= 5 ? 12 : 5;

  for (let attempt = 0; attempt < 1500 && found < wanted; attempt++) {
    const { H, W } = chooseDimensions(P);
    const live = carveShape(H, W, P);

    const K = Math.min(P + 1, scenario.rooms.length);
    const rooms = createRooms(live, scenario, K);
    if (rooms.length < 2) continue;

    const solution = pickPlacement(H, W, P, live);
    if (!solution) continue;

    // El asesino tiene que quedar determinado: la habitación de la víctima
    // debe contener exactamente un sospechoso.
    const roomIdOf = new Map();
    for (const room of rooms)
      for (const c of room.cells) roomIdOf.set(key(c.row, c.col), room.id);

    const victimPos = solution[P - 1];
    const victimRoomId = roomIdOf.get(key(victimPos.row, victimPos.col));
    const shareRoom = solution.slice(0, numSuspects)
      .filter(p => roomIdOf.get(key(p.row, p.col)) === victimRoomId);
    if (shareRoom.length !== 1) continue;

    const objects = placeObjects(rooms, solution, live, H, W);
    const board = buildBoard(rooms, objects, live, H, W);
    if (solution.some(p => board.isBlocked(p.row, p.col))) continue;

    const suspectNames = shuffle(SUSPECT_NAMES).slice(0, numSuspects);
    const victimName = pick(VICTIM_NAMES.filter(v => !suspectNames.includes(v)));
    const names = [...suspectNames, victimName];

    // Conviene el pool completo: recortarlo hace que menos tableros admitan
    // resolución lógica y se descarten más, que sale más caro que evaluarlo.
    // Una sola persona puede recibir pistas fuertes: es la que abrirá el caso.
    const anchorIdx = Math.floor(rng() * P);
    const pools = [];
    for (let i = 0; i < P; i++)
      pools.push(candidateClues(i, solution, board, names, P,
                                i === anchorIdx ? diff.anchorTiers : diff.tiers));

    const chosen = selectClues(pools, board, P);
    if (!chosen) continue;

    const { resolvedAt, depth } = solveLogically(chosen.flat(), board, P);

    // Requisito duro: UN solo punto de partida. Exactamente una persona sale
    // con su ficha sola, así "la primera es más fácil que las otras" es literal
    // y no un empate presentado como secuencia. Y tiene que haber cadena.
    if (resolvedAt.filter(r => r === 1).length !== 1) continue;
    if (depth < 2) continue;

    found++;
    // Calidad de la escalera: cuántas rondas distintas hay. P = perfecta.
    const rungs = new Set(resolvedAt).size;
    if (!best || rungs > best.rungs) best = { resolvedAt, depth, rungs, chosen, board, solution,
                                              rooms, objects, live, H, W, roomIdOf, victimPos,
                                              victimRoomId, suspectNames, victimName };
    if (rungs >= goodEnough) break;
    continue;
  }

  if (best) {
    const { resolvedAt, depth, chosen, solution, rooms, objects, live, H, W,
            roomIdOf, victimPos, victimRoomId, suspectNames, victimName } = best;

    const order = [...Array(P).keys()].sort((a, b) => resolvedAt[a] - resolvedAt[b]);
    const stepOf = new Map(order.map((idx, pos) => [idx, pos + 1]));

    const suspects = suspectNames.map((name, i) => ({
      id: `s${i}`, idx: i, name,
      row: solution[i].row, col: solution[i].col,
      roomId: roomIdOf.get(key(solution[i].row, solution[i].col)),
      clues: chosen[i].map(c => c.text),
      step: stepOf.get(i),
    }));
    suspects.sort((a, b) => a.step - b.step);

    const murderer = suspects.find(s => s.roomId === victimRoomId);

    return {
      H, W, P, live, rooms, objects, suspects, scenario, difficulty,
      depth, rungs: best.rungs, seed: caseSeed,
      clues: chosen.flat(),          // datos puros: viajan al worker y se
                                     // reevalúan para detectar contradicciones
      blockedSet: new Set(objects.filter(o => !OBJECT_TYPES[o.type].occupiable).map(o => key(o.row, o.col))),
      victim: {
        id: 'victim', idx: P - 1,
        name: victimName,
        row: victimPos.row, col: victimPos.col,
        roomId: victimRoomId,
        clues: chosen[P - 1].map(c => c.text),
        step: stepOf.get(P - 1),
      },
      murderer: murderer.id,
    };
  }

  // Sin cadena estricta a esta altura: se afloja la dificultad antes de rendirse.
  if (difficulty === 'dificil') return generatePuzzle(numSuspects, scenarioId, 'normal', caseSeed);
  if (difficulty === 'normal')  return generatePuzzle(numSuspects, scenarioId, 'facil', caseSeed);
  return generatePuzzle(numSuspects, null, 'facil', caseSeed + 1);
}

// ─── Utilidades de partida ──────────────────────────────────────────────────

// Devuelve las declaraciones que el estado actual del jugador ya contradice.
// Solo mira pistas cuyos participantes están todos colocados, así que no
// delata nada: informa de un choque que ya se puede comprobar a mano.
export function findContradictions(puzzle, userPlacements) {
  const board = makeIndex(puzzle);
  const people = [...puzzle.suspects, puzzle.victim];
  const pos = new Array(puzzle.P).fill(null);
  for (const p of people) pos[p.idx] = userPlacements[p.id] || null;

  const byIdx = new Map(people.map(p => [p.idx, p]));
  const out = [];
  for (const clue of puzzle.clues) {
    if (!pos[clue.owner]) continue;
    if (clue.refs.some(r => !pos[r])) continue;
    if (!evalClue(clue, pos, board)) {
      out.push({ personId: byIdx.get(clue.owner).id, name: byIdx.get(clue.owner).name, text: clue.text });
    }
  }
  return out;
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
  return [...puzzle.suspects, puzzle.victim].every(s => {
    const p = userPlacements[s.id];
    return p && p.row === s.row && p.col === s.col;
  });
}

// La persona que antes toca según la cadena, entre las que faltan o están mal.
export function nextPending(puzzle, userPlacements) {
  const pending = [...puzzle.suspects, puzzle.victim].filter(s => {
    const p = userPlacements[s.id];
    return !p || p.row !== s.row || p.col !== s.col;
  });
  if (!pending.length) return null;
  return pending.sort((a, b) => a.step - b.step)[0];
}

// Pistas graduadas: cada nivel dice algo más, para que pedir ayuda no equivalga
// a rendirse. Nivel 3 ya señala la casilla.
export function getHint(puzzle, userPlacements, level) {
  const who = nextPending(puzzle, userPlacements);
  if (!who) return null;

  if (level <= 1) {
    return {
      text: `Toca deducir a ${who.name} (ficha ${who.step}). Con lo que ya está colocado, su declaración alcanza.`,
      cell: null,
    };
  }
  if (level === 2) {
    const room = puzzle.rooms.find(r => r.id === who.roomId);
    return { text: `${who.name} estaba en ${room.name}.`, cell: null };
  }
  return {
    text: `${who.name} estaba en la casilla marcada (fila ${who.row + 1}, columna ${who.col + 1}).`,
    cell: { row: who.row, col: who.col },
  };
}
