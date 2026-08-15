const ROOM_NAMES = [
  'Dormitorio', 'Comedor', 'Salón Principal', 'Sala',
  'Cocina', 'Biblioteca', 'Estudio', 'Pasillo',
];

const ROOM_COLORS = [
  '#d4e8ff', '#d4f5e0', '#fdefd4', '#f5d4f5',
  '#d4f5f5', '#ffd4d4', '#f5f5d4', '#e8d4f5',
];

const SUSPECT_NAMES = [
  'Cora', 'Bella', 'Ella', 'Axel', 'Douglas',
  'Austin', 'Brycen', 'Charlene', 'Marco', 'Sofia',
  'Hugo', 'Lena', 'Nico', 'Rita', 'Diego',
];

const VICTIM_NAMES = ['Vincent', 'Vinny', 'Victor', 'Vera', 'Vince'];

export const OBJECT_TYPES = {
  // Ocupables: el sospechoso puede estar EN la misma celda
  bed:       { label: 'una cama',       icon: '🛏️', clueOn: 'Estaba sobre la cama',     occupiable: true  },
  chair:     { label: 'una silla',      icon: '🪑', clueOn: 'Estaba sobre una silla',   occupiable: true  },
  rug:       { label: 'una alfombra',   icon: '🟫', clueOn: 'Estaba sobre la alfombra', occupiable: true  },
  // No ocupables: el sospechoso debe estar en una celda ADYACENTE
  table:     { label: 'una mesa',       icon: '🍽️', clueOn: null, occupiable: false },
  window:    { label: 'una ventana',    icon: '🪟', clueOn: null, occupiable: false },
  plant:     { label: 'una planta',     icon: '🌿', clueOn: null, occupiable: false },
  bookshelf: { label: 'una estantería', icon: '📚', clueOn: null, occupiable: false },
  fireplace: { label: 'una chimenea',   icon: '🔥', clueOn: null, occupiable: false },
  wardrobe:  { label: 'un armario',     icon: '🚪', clueOn: null, occupiable: false },
};

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

function generatePermutation(n) {
  return shuffle([...Array(n).keys()]);
}

function createRooms(N) {
  const names = shuffle([...ROOM_NAMES]);
  const colors = shuffle([...ROOM_COLORS]);

  let roomGrid; // [numRoomRows][numRoomCols]
  if (N <= 4) {
    roomGrid = [2, 2];
  } else if (N <= 6) {
    roomGrid = [2, 3];
  } else {
    roomGrid = [3, 3];
  }

  const [rRows, rCols] = roomGrid;
  const rowSplit = splitEvenly(N, rRows);
  const colSplit = splitEvenly(N, rCols);

  const rooms = [];
  let nameIdx = 0;
  let colorIdx = 0;
  let rowStart = 0;

  for (let rr = 0; rr < rRows; rr++) {
    let colStart = 0;
    const rH = rowSplit[rr];
    for (let rc = 0; rc < rCols; rc++) {
      const rW = colSplit[rc];
      const cells = [];
      for (let r = rowStart; r < rowStart + rH; r++) {
        for (let c = colStart; c < colStart + rW; c++) {
          cells.push({ row: r, col: c });
        }
      }
      rooms.push({
        id: `room_${rr}_${rc}`,
        name: names[nameIdx++ % names.length],
        color: colors[colorIdx++ % colors.length],
        rowStart,
        colStart,
        height: rH,
        width: rW,
        cells,
        objects: [],
      });
      colStart += rW;
    }
    rowStart += rH;
  }

  return rooms;
}

function splitEvenly(total, parts) {
  const base = Math.floor(total / parts);
  const extra = total % parts;
  return Array.from({ length: parts }, (_, i) => base + (i < extra ? 1 : 0));
}

function addObjectsToRooms(rooms, occupiedSet = new Set()) {
  const objectKeys = Object.keys(OBJECT_TYPES);
  for (const room of rooms) {
    const picked = shuffle(objectKeys).slice(0, Math.min(2, room.cells.length - 1));
    const usedCells = new Set();
    for (const objType of picked) {
      const isOccupiable = OBJECT_TYPES[objType].occupiable;
      const available = room.cells.filter(c => {
        const key = `${c.row},${c.col}`;
        if (usedCells.has(key)) return false;
        // Objetos no ocupables nunca pueden ir en celdas de sospechosos/víctima
        if (!isOccupiable && occupiedSet.has(key)) return false;
        return true;
      });
      if (available.length === 0) continue;
      const cell = available[Math.floor(Math.random() * available.length)];
      usedCells.add(`${cell.row},${cell.col}`);
      room.objects.push({ type: objType, row: cell.row, col: cell.col });
    }
  }
}

function getRoomAt(rooms, row, col) {
  return rooms.find(r => r.cells.some(c => c.row === row && c.col === col));
}

function getObjectAt(rooms, row, col) {
  for (const room of rooms) {
    const obj = room.objects.find(o => o.row === row && o.col === col);
    if (obj) return obj;
  }
  return null;
}

function getAdjacentObject(rooms, row, col) {
  const neighbors = [
    { row: row - 1, col },
    { row: row + 1, col },
    { row, col: col - 1 },
    { row, col: col + 1 },
  ];
  for (const n of neighbors) {
    const obj = getObjectAt(rooms, n.row, n.col);
    if (obj) return obj;
  }
  return null;
}

function generateClue(rooms, row, col, roomForClue) {
  const obj = getObjectAt(rooms, row, col);
  // Solo generar clue "sobre" si el objeto es ocupable (cama, silla, alfombra)
  if (obj && OBJECT_TYPES[obj.type].occupiable) {
    return { text: OBJECT_TYPES[obj.type].clueOn, type: 'onObject', target: obj.type };
  }
  const adjObj = getAdjacentObject(rooms, row, col);
  if (adjObj) {
    return {
      text: `Estaba junto a ${OBJECT_TYPES[adjObj.type].label}`,
      type: 'nextToObject',
      target: adjObj.type,
    };
  }
  return {
    text: `Estaba en ${roomForClue.name}`,
    type: 'inRoom',
    target: roomForClue.id,
  };
}

export function canPlaceAt(puzzle, row, col) {
  for (const room of puzzle.rooms) {
    const obj = room.objects.find(o => o.row === row && o.col === col);
    if (obj) return OBJECT_TYPES[obj.type].occupiable;
  }
  return true;
}

export function generatePuzzle(numSuspects = 3) {
  const N = numSuspects + 1;
  const rooms = createRooms(N);

  // Placement primero → sabemos qué celdas estarán ocupadas
  const cols = generatePermutation(N);
  const placement = cols.map((col, row) => ({ row, col }));
  const occupiedSet = new Set(placement.map(p => `${p.row},${p.col}`));

  // Objetos respetan las celdas ocupadas
  addObjectsToRooms(rooms, occupiedSet);

  const names = shuffle([...SUSPECT_NAMES]).slice(0, numSuspects);
  const victimName = VICTIM_NAMES[Math.floor(Math.random() * VICTIM_NAMES.length)];

  const suspects = names.map((name, i) => {
    const { row, col } = placement[i];
    const room = getRoomAt(rooms, row, col);
    const clue = generateClue(rooms, row, col, room);
    return {
      id: name.toLowerCase(),
      name,
      row,
      col,
      roomId: room.id,
      clue: clue.text,
    };
  });

  const victimPos = placement[numSuspects];
  const victimRoom = getRoomAt(rooms, victimPos.row, victimPos.col);

  const murderer = suspects.find(s => s.roomId === victimRoom.id);

  return {
    N,
    rooms,
    suspects,
    victim: {
      name: victimName,
      row: victimPos.row,
      col: victimPos.col,
      roomId: victimRoom.id,
      clue: 'Estaba en la última casilla libre',
    },
    murderer: murderer ? murderer.id : suspects[0].id,
  };
}

export function checkPlacement(puzzle, userPlacements) {
  // userPlacements: { suspectId: { row, col } | null }
  const placed = Object.entries(userPlacements).filter(([, v]) => v !== null);

  // Check unique rows and cols among placed
  const rows = placed.map(([, v]) => v.row);
  const cols = placed.map(([, v]) => v.col);
  if (new Set(rows).size !== rows.length) return { valid: false, reason: 'Dos personas en la misma fila.' };
  if (new Set(cols).size !== cols.length) return { valid: false, reason: 'Dos personas en la misma columna.' };

  return { valid: true };
}

export function isCorrectSolution(puzzle, userPlacements) {
  for (const suspect of puzzle.suspects) {
    const p = userPlacements[suspect.id];
    if (!p) return false;
    if (p.row !== suspect.row || p.col !== suspect.col) return false;
  }
  return true;
}
