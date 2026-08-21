const ROOM_NAMES = ['Dormitorio', 'Comedor', 'Salón Principal', 'Sala', 'Cocina', 'Biblioteca', 'Estudio', 'Pasillo'];
const ROOM_COLORS = ['#d4e8ff', '#d4f5e0', '#fdefd4', '#f5d4f5', '#d4f5f5', '#ffd4d4', '#f5f5d4', '#e8d4f5'];
const SUSPECT_NAMES = ['Cora', 'Bella', 'Ella', 'Axel', 'Douglas', 'Austin', 'Brycen', 'Charlene', 'Marco', 'Sofia', 'Hugo', 'Lena', 'Nico', 'Rita', 'Diego'];
const VICTIM_NAMES = ['Vincent', 'Vinny', 'Victor', 'Vera', 'Vince'];

// ─── Predefined Murdoku scenarios ─────────────────────────────────────────────

export const SCENARIOS = {
  // N=4 (3 suspects) → 2×2 = 4 rooms
  small: [
    {
      id: 'mansion', name: 'La Mansión', icon: '🏛️',
      rooms: [
        { name: 'Gran Salón',  color: '#ede0c8' },
        { name: 'Biblioteca',  color: '#cce0c0' },
        { name: 'Comedor',     color: '#e0cccc' },
        { name: 'Bodega',      color: '#cccce0' },
      ],
    },
    {
      id: 'barco', name: 'El Barco', icon: '🚢',
      rooms: [
        { name: 'Cubierta',    color: '#c8d8ec' },
        { name: 'Camarotes',   color: '#d8ecc8' },
        { name: 'Sala Máq.',   color: '#d8ccc8' },
        { name: 'Cocina',      color: '#ecdcc8' },
      ],
    },
    {
      id: 'tren', name: 'El Tren', icon: '🚂',
      rooms: [
        { name: 'Vagón A',     color: '#d8cce8' },
        { name: 'Vag. Comedor',color: '#ece4c8' },
        { name: 'Vagón B',     color: '#c8d8d0' },
        { name: 'Fumadores',   color: '#e0ccc8' },
      ],
    },
  ],
  // N=5 or N=6 (4-5 suspects) → 2×3 = 6 rooms
  large: [
    {
      id: 'hotel', name: 'El Hotel', icon: '🏨',
      rooms: [
        { name: 'Recepción',   color: '#eddfcc' },
        { name: 'Bar',         color: '#e0cccc' },
        { name: 'Sala Juntas', color: '#cce0c0' },
        { name: 'Suite',       color: '#dccce0' },
        { name: 'Cocina',      color: '#cce0d8' },
        { name: 'Bodega',      color: '#cccce0' },
      ],
    },
    {
      id: 'museo', name: 'El Museo', icon: '🏺',
      rooms: [
        { name: 'Renacimiento',color: '#eddfcc' },
        { name: 'Arte Moderno',color: '#ccd8ec' },
        { name: 'Antigüedades',color: '#e0cccc' },
        { name: 'Escultura',   color: '#cce0c0' },
        { name: 'Archivo',     color: '#d8d0c8' },
        { name: 'Entrada',     color: '#d4d4d4' },
      ],
    },
    {
      id: 'palacio', name: 'El Palacio', icon: '👑',
      rooms: [
        { name: 'Salón Trono', color: '#edd8bc' },
        { name: 'Biblioteca',  color: '#ccd8cc' },
        { name: 'Com. Real',   color: '#e0cccc' },
        { name: 'Capilla',     color: '#d8d8ec' },
        { name: 'Torre Norte', color: '#ccd0d8' },
        { name: 'Torre Sur',   color: '#d0ccd8' },
      ],
    },
    {
      id: 'aldea', name: 'La Aldea', icon: '🏘️',
      rooms: [
        { name: 'Taberna',     color: '#edd8bc' },
        { name: 'Herrería',    color: '#d0c8b8' },
        { name: 'Molino',      color: '#ccd8bc' },
        { name: 'Iglesia',     color: '#d8d8d8' },
        { name: 'Granero',     color: '#ccd0bc' },
        { name: 'Posada',      color: '#d8ccbc' },
      ],
    },
  ],
};

export const OBJECT_TYPES = {
  // Ocupables: el sospechoso ESTÁ en esa celda
  bed:       { label: 'una cama',       icon: '🛏️', clueOn: 'Estaba sobre la cama',     occupiable: true  },
  chair:     { label: 'una silla',      icon: '🪑', clueOn: 'Estaba sobre una silla',   occupiable: true  },
  rug:       { label: 'una alfombra',   icon: '🟫', clueOn: 'Estaba sobre la alfombra', occupiable: true  },
  // Props: el sospechoso está en una celda ADYACENTE
  table:     { label: 'una mesa',       icon: '🍽️', occupiable: false },
  window:    { label: 'una ventana',    icon: '🪟', occupiable: false },
  plant:     { label: 'una planta',     icon: '🌿', occupiable: false },
  bookshelf: { label: 'una estantería', icon: '📚', occupiable: false },
  fireplace: { label: 'una chimenea',   icon: '🔥', occupiable: false },
  wardrobe:  { label: 'un armario',     icon: '🚪', occupiable: false },
};

// ─── Utils ────────────────────────────────────────────────────────────────────

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

function splitEvenly(total, parts) {
  const base = Math.floor(total / parts);
  const extra = total % parts;
  return Array.from({ length: parts }, (_, i) => base + (i < extra ? 1 : 0));
}

function adj(row, col, N) {
  return [[row - 1, col], [row + 1, col], [row, col - 1], [row, col + 1]]
    .filter(([r, c]) => r >= 0 && r < N && c >= 0 && c < N)
    .map(([r, c]) => ({ row: r, col: c }));
}

function dist1(a, b) {
  return Math.abs(a.row - b.row) + Math.abs(a.col - b.col) === 1;
}

// ─── Rooms ────────────────────────────────────────────────────────────────────

function createRooms(N, presetRooms) {
  const [rRows, rCols] = N <= 4 ? [2, 2] : N <= 6 ? [2, 3] : [3, 3];
  const rowSplit = splitEvenly(N, rRows);
  const colSplit = splitEvenly(N, rCols);

  let names, colors;
  if (presetRooms) {
    names  = presetRooms.map(r => r.name);
    colors = presetRooms.map(r => r.color);
  } else {
    names  = shuffle([...ROOM_NAMES]);
    colors = shuffle([...ROOM_COLORS]);
  }

  const rooms = [];
  let rowStart = 0, idx = 0;
  for (let rr = 0; rr < rRows; rr++) {
    let colStart = 0;
    for (let rc = 0; rc < rCols; rc++) {
      const rH = rowSplit[rr], rW = colSplit[rc];
      const cells = [];
      for (let r = rowStart; r < rowStart + rH; r++)
        for (let c = colStart; c < colStart + rW; c++)
          cells.push({ row: r, col: c });
      rooms.push({
        id: `room_${rr}_${rc}`,
        name:  names[idx % names.length],
        color: colors[idx % colors.length],
        rowStart, colStart, height: rH, width: rW,
        cells, objects: [],
      });
      idx++; colStart += rW;
    }
    rowStart += rowSplit[rr];
  }
  return rooms;
}

function getRoomAt(rooms, row, col) {
  return rooms.find(r => r.cells.some(c => c.row === row && c.col === col));
}

function getObjectAt(rooms, row, col) {
  for (const room of rooms)
    for (const o of room.objects)
      if (o.row === row && o.col === col) return o;
  return null;
}

// ─── Clue assignment ──────────────────────────────────────────────────────────

function assignClues(rooms, suspectPos, N) {
  const occPool  = shuffle(['bed', 'chair', 'rug']);
  const propPool = shuffle(['table', 'window', 'plant', 'bookshelf', 'fireplace', 'wardrobe']);
  const usedOcc  = new Set();
  const usedProp = new Set();
  const placed   = [];
  const clues    = [];

  for (const { row, col } of suspectPos) {

    // ── Option 1: occupiable object on this cell ──────────────────────────
    const freeOcc = occPool.filter(t => !usedOcc.has(t));
    if (freeOcc.length > 0) {
      const type = freeOcc[0];
      usedOcc.add(type);
      placed.push({ type, row, col });
      clues.push({ type: 'onObject', target: type, text: OBJECT_TYPES[type].clueOn });
      continue;
    }

    // ── Option 2: prop in adjacent cell exclusive to this suspect ─────────
    const usedCells = new Set(placed.map(o => `${o.row},${o.col}`));
    const candidates = adj(row, col, N).filter(a => {
      if (usedCells.has(`${a.row},${a.col}`)) return false;
      if (suspectPos.some(p => p.row === a.row && p.col === a.col)) return false;
      const touching = suspectPos.filter(p => dist1(p, a));
      return touching.length === 1;
    });

    const freeProp = propPool.filter(t => !usedProp.has(t));
    if (candidates.length > 0 && freeProp.length > 0) {
      const cell = candidates[Math.floor(Math.random() * candidates.length)];
      const type = freeProp[0];
      usedProp.add(type);
      placed.push({ type, row: cell.row, col: cell.col });
      clues.push({
        type: 'nextToObject', target: type,
        text: `Estaba junto a ${OBJECT_TYPES[type].label}`,
      });
      continue;
    }

    // ── Option 3: room fallback ───────────────────────────────────────────
    const room = getRoomAt(rooms, row, col);
    clues.push({ type: 'inRoom', target: room.id, text: `Estaba en ${room.name}` });
  }

  return { clues, placed };
}

// ─── Uniqueness validator ─────────────────────────────────────────────────────

function validCells(clue, rooms, N) {
  const result = [];
  for (let r = 0; r < N; r++) {
    for (let c = 0; c < N; c++) {
      if (clue.type === 'onObject') {
        const o = getObjectAt(rooms, r, c);
        if (o && o.type === clue.target) result.push({ row: r, col: c });
      } else if (clue.type === 'nextToObject') {
        const hit = adj(r, c, N).some(a => {
          const o = getObjectAt(rooms, a.row, a.col);
          return o && o.type === clue.target;
        });
        if (hit) result.push({ row: r, col: c });
      } else {
        const room = getRoomAt(rooms, r, c);
        if (room && room.id === clue.target) result.push({ row: r, col: c });
      }
    }
  }
  return result;
}

function countSolutions(clues, rooms, N, idx = 0, usedRows = new Set(), usedCols = new Set()) {
  if (idx === clues.length) return 1;
  const cells = validCells(clues[idx], rooms, N)
    .filter(c => !usedRows.has(c.row) && !usedCols.has(c.col));
  let total = 0;
  for (const cell of cells) {
    usedRows.add(cell.row); usedCols.add(cell.col);
    total += countSolutions(clues, rooms, N, idx + 1, usedRows, usedCols);
    usedRows.delete(cell.row); usedCols.delete(cell.col);
    if (total > 1) return total;
  }
  return total;
}

// ─── Public API ───────────────────────────────────────────────────────────────

export function canPlaceAt(puzzle, row, col) {
  const obj = getObjectAt(puzzle.rooms, row, col);
  return !obj || OBJECT_TYPES[obj.type].occupiable;
}

export function generatePuzzle(numSuspects = 3, scenarioId = null) {
  const N = numSuspects + 1;

  const scenarioGroup = N <= 4 ? SCENARIOS.small : SCENARIOS.large;
  const scenario = scenarioId ? scenarioGroup.find(s => s.id === scenarioId) : null;
  const presetRooms = scenario ? scenario.rooms : null;

  for (let attempt = 0; attempt < 200; attempt++) {
    const rooms = createRooms(N, presetRooms);

    const cols = shuffle([...Array(N).keys()]);
    const placement    = cols.map((col, row) => ({ row, col }));
    const suspectPos   = placement.slice(0, numSuspects);

    const { clues, placed } = assignClues(rooms, suspectPos, N);

    for (const obj of placed)
      getRoomAt(rooms, obj.row, obj.col).objects.push(obj);

    if (countSolutions(clues, rooms, N) !== 1) continue;

    const names      = shuffle([...SUSPECT_NAMES]).slice(0, numSuspects);
    const victimName = VICTIM_NAMES[Math.floor(Math.random() * VICTIM_NAMES.length)];

    const suspects = names.map((name, i) => ({
      id: name.toLowerCase(), name,
      row: suspectPos[i].row, col: suspectPos[i].col,
      roomId: getRoomAt(rooms, suspectPos[i].row, suspectPos[i].col).id,
      clue: clues[i].text,
    }));

    const victimPos  = placement[numSuspects];
    const victimRoom = getRoomAt(rooms, victimPos.row, victimPos.col);
    const murderer   = suspects.find(s => s.roomId === victimRoom.id);

    return {
      N, rooms, suspects, scenario,
      victim: {
        name: victimName,
        row: victimPos.row, col: victimPos.col,
        roomId: victimRoom.id,
        clue: 'Estaba en la última casilla libre',
      },
      murderer: murderer ? murderer.id : suspects[0].id,
    };
  }

  console.error('generatePuzzle: no se encontró puzzle único en 200 intentos');
  return generatePuzzle(numSuspects, scenarioId);
}

export function checkPlacement(puzzle, userPlacements) {
  const placed = Object.entries(userPlacements).filter(([, v]) => v !== null);
  const rows = placed.map(([, v]) => v.row);
  const cols = placed.map(([, v]) => v.col);
  if (new Set(rows).size !== rows.length) return { valid: false };
  if (new Set(cols).size !== cols.length) return { valid: false };
  return { valid: true };
}

export function isCorrectSolution(puzzle, userPlacements) {
  for (const s of puzzle.suspects) {
    const p = userPlacements[s.id];
    if (!p || p.row !== s.row || p.col !== s.col) return false;
  }
  return true;
}
