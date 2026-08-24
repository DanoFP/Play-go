import { useMemo } from 'react';
import { OBJECT_TYPES } from '../utils/puzzleGenerator.js';
import Furniture from './Furniture.jsx';

const SUSPECT_COLORS = [
  '#3f8fd0', '#d9534f', '#3fae72', '#e0a33c', '#9b6fc4', '#3fb0ae',
];

// El viewBox ya reescala en pantallas chicas, así que estos valores solo fijan
// la proporción entre mobiliario y casilla; conviene que sean generosos.
function cellSize(H, W) {
  const big = Math.max(H, W);
  if (big <= 5) return 88;
  if (big === 6) return 80;
  if (big === 7) return 70;
  if (big === 8) return 62;
  return 56;
}

// ─── Texturas de suelo ──────────────────────────────────────────────────────

function FloorPattern({ id, type, color, s }) {
  const line = 'rgba(0,0,0,0.10)';
  const soft = 'rgba(255,255,255,0.16)';

  const body = {
    wood: (
      <>
        <rect width={s} height={s} fill={color} />
        {[0.25, 0.5, 0.75].map(f => (
          <line key={f} x1={0} y1={s * f} x2={s} y2={s * f} stroke={line} strokeWidth={1} />
        ))}
        <line x1={s * 0.4} y1={0} x2={s * 0.4} y2={s * 0.25} stroke={line} strokeWidth={1} />
        <line x1={s * 0.7} y1={s * 0.5} x2={s * 0.7} y2={s * 0.75} stroke={line} strokeWidth={1} />
      </>
    ),
    tile: (
      <>
        <rect width={s} height={s} fill={color} />
        <rect width={s / 2} height={s / 2} fill={soft} />
        <rect x={s / 2} y={s / 2} width={s / 2} height={s / 2} fill={soft} />
        <line x1={s / 2} y1={0} x2={s / 2} y2={s} stroke={line} strokeWidth={0.8} />
        <line x1={0} y1={s / 2} x2={s} y2={s / 2} stroke={line} strokeWidth={0.8} />
      </>
    ),
    stone: (
      <>
        <rect width={s} height={s} fill={color} />
        <line x1={0} y1={s / 2} x2={s} y2={s / 2} stroke={line} strokeWidth={1.1} />
        <line x1={s / 2} y1={0} x2={s / 2} y2={s / 2} stroke={line} strokeWidth={1.1} />
        <line x1={0} y1={s / 2} x2={0} y2={s} stroke={line} strokeWidth={1.1} />
        <line x1={s} y1={s / 2} x2={s} y2={s} stroke={line} strokeWidth={1.1} />
      </>
    ),
    carpet: (
      <>
        <rect width={s} height={s} fill={color} />
        {[0.2, 0.5, 0.8].map(fy =>
          [0.2, 0.5, 0.8].map(fx => (
            <circle key={`${fx}-${fy}`} cx={s * fx} cy={s * fy} r={1.1} fill="rgba(0,0,0,0.11)" />
          ))
        )}
      </>
    ),
    metal: (
      <>
        <rect width={s} height={s} fill={color} />
        {[0, 0.33, 0.66].map(f => (
          <line key={f} x1={0} y1={s * f} x2={s * (1 - f)} y2={s} stroke={line} strokeWidth={0.9} />
        ))}
        <circle cx={s * 0.15} cy={s * 0.15} r={1.3} fill="rgba(0,0,0,0.18)" />
        <circle cx={s * 0.85} cy={s * 0.85} r={1.3} fill="rgba(0,0,0,0.18)" />
      </>
    ),
    grass: (
      <>
        <rect width={s} height={s} fill={color} />
        {[[0.2, 0.3], [0.6, 0.2], [0.4, 0.7], [0.8, 0.6]].map(([fx, fy], i) => (
          <path key={i} d={`M${s * fx} ${s * fy + 4} q1.5 -4 3 0`} stroke="rgba(0,0,0,0.16)" strokeWidth={1} fill="none" />
        ))}
      </>
    ),
  }[type] || <rect width={s} height={s} fill={color} />;

  return <pattern id={id} width={s} height={s} patternUnits="userSpaceOnUse">{body}</pattern>;
}

// ─── Ficha ──────────────────────────────────────────────────────────────────

function Token({ name, color, s, variant }) {
  const r = s * 0.29;
  const ring =
    variant === 'murderer' || variant === 'victim' ? '#e74c3c'
    : variant === 'solved' ? '#2ecc71'
    : 'rgba(255,255,255,0.55)';

  return (
    <g className={variant === 'murderer' ? 'token-murderer' : ''}>
      <ellipse cx={0} cy={r * 0.95} rx={r * 0.85} ry={r * 0.25} fill="rgba(0,0,0,0.28)" />
      <circle cx={0} cy={0} r={r} fill={color} stroke={ring} strokeWidth={2} />
      {variant === 'victim' ? (
        <g fill="rgba(255,255,255,0.92)">
          <rect x={-r * 0.12} y={-r * 0.55} width={r * 0.24} height={r * 1.1} rx={r * 0.06} />
          <rect x={-r * 0.42} y={-r * 0.24} width={r * 0.84} height={r * 0.24} rx={r * 0.06} />
        </g>
      ) : (
        <g fill="rgba(255,255,255,0.92)">
          <circle cx={0} cy={-r * 0.28} r={r * 0.28} />
          <path d={`M${-r * 0.52} ${r * 0.62} a${r * 0.52} ${r * 0.48} 0 0 1 ${r * 1.04} 0 Z`} />
        </g>
      )}
      <rect x={-r * 1.05} y={r * 1.05} width={r * 2.1} height={r * 0.62} rx={r * 0.31} fill="rgba(12,12,26,0.88)" />
      <text x={0} y={r * 1.5} textAnchor="middle" fontSize={Math.max(8, r * 0.46)}
            fill="#e8e4d8" fontFamily="Georgia, serif" fontWeight="700">
        {name}
      </text>
    </g>
  );
}

// ─── Tablero ────────────────────────────────────────────────────────────────

export default function GameBoard({ puzzle, userPlacements, eliminated, onCellClick, gameStatus, hint }) {
  const { H, W, live, rooms, objects, suspects, victim } = puzzle;
  const S = cellSize(H, W);
  const GUT = 20;
  const bw = W * S, bh = H * S;
  const showSolution = gameStatus === 'won' || gameStatus === 'revealed';

  const people = useMemo(
    () => [...suspects, { ...victim, id: 'victim', isVictim: true }],
    [suspects, victim]
  );

  const roomAt = useMemo(() => {
    const m = new Map();
    for (const room of rooms) for (const c of room.cells) m.set(`${c.row},${c.col}`, room);
    return m;
  }, [rooms]);

  const objAt = useMemo(() => {
    const m = new Map();
    for (const o of objects) m.set(`${o.row},${o.col}`, o);
    return m;
  }, [objects]);

  // Paredes: hay tabique donde cambia de habitación, y muro exterior donde la
  // planta se acaba — que ahora incluye las celdas recortadas, no solo el borde
  // del rectángulo. De cada par de habitaciones vecinas se abre una puerta.
  const { walls, doors } = useMemo(() => {
    const segs = [];
    const byPair = new Map();
    const isLive = (r, c) => live.has(`${r},${c}`);

    for (let r = 0; r < H; r++) {
      for (let c = 0; c < W; c++) {
        if (!isLive(r, c)) continue;
        const mine = roomAt.get(`${r},${c}`);
        const sides = [
          { dr: -1, dc: 0, x1: c * S, y1: r * S, x2: (c + 1) * S, y2: r * S },
          { dr: 1, dc: 0, x1: c * S, y1: (r + 1) * S, x2: (c + 1) * S, y2: (r + 1) * S },
          { dr: 0, dc: -1, x1: c * S, y1: r * S, x2: c * S, y2: (r + 1) * S },
          { dr: 0, dc: 1, x1: (c + 1) * S, y1: r * S, x2: (c + 1) * S, y2: (r + 1) * S },
        ];
        for (const sd of sides) {
          const nr = r + sd.dr, nc = c + sd.dc;
          if (!isLive(nr, nc)) {                     // fuera de la planta
            segs.push({ ...sd, outer: true });
            continue;
          }
          const other = roomAt.get(`${nr},${nc}`);
          if (!other || other.id === mine.id) continue;
          // Solo un lado del par dibuja el tabique, para no duplicarlo.
          if (sd.dr < 0 || sd.dc < 0) continue;
          const seg = { ...sd, outer: false };
          segs.push(seg);
          const pair = [mine.id, other.id].sort().join('|');
          if (!byPair.has(pair)) byPair.set(pair, []);
          byPair.get(pair).push(seg);
        }
      }
    }

    const doorSet = new Set();
    for (const [, list] of byPair) doorSet.add(list[Math.floor(list.length / 2)]);
    return { walls: segs.filter(s => !doorSet.has(s)), doors: [...doorSet] };
  }, [roomAt, live, H, W, S]);

  const placedAt = (row, col) =>
    people.find(p => {
      const u = userPlacements[p.id];
      return u && u.row === row && u.col === col;
    });

  const liveCells = useMemo(() => {
    const out = [];
    for (const k of live) {
      const [r, c] = k.split(',').map(Number);
      out.push({ row: r, col: c });
    }
    return out;
  }, [live]);

  return (
    <div className="board-wrapper">
      {/* Sin ancho fijo: el viewBox deja que el tablero escale con el hueco
          disponible, que es lo que hace falta en pantallas chicas. */}
      <svg viewBox={`0 0 ${bw + GUT} ${bh + GUT}`} className="board-svg"
           style={{ maxWidth: bw + GUT, aspectRatio: `${bw + GUT} / ${bh + GUT}` }}>
        <defs>
          {rooms.map(room => (
            <FloorPattern key={room.id} id={`floor-${room.id}`} type={room.floor} color={room.color} s={S} />
          ))}
          <filter id="board-vignette" x="-10%" y="-10%" width="120%" height="120%">
            <feDropShadow dx="0" dy="2" stdDeviation="3" floodOpacity="0.35" />
          </filter>
          <pattern id="blocked-hatch" width="9" height="9" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
            <rect width="9" height="9" fill="rgba(14,10,6,0.13)" />
            <line x1="0" y1="0" x2="0" y2="9" stroke="rgba(0,0,0,0.17)" strokeWidth="2.5" />
          </pattern>
        </defs>

        {/* Coordenadas: las pistas hablan de "fila 3" y "columna 5" */}
        {Array.from({ length: W }, (_, i) => (
          <text key={`cx${i}`} x={GUT + i * S + S / 2} y={13} textAnchor="middle"
                fontFamily="'Source Code Pro', monospace" fontSize={10} fill="#6f6a5e">{i + 1}</text>
        ))}
        {Array.from({ length: H }, (_, i) => (
          <text key={`cy${i}`} x={10} y={GUT + i * S + S / 2 + 4} textAnchor="middle"
                fontFamily="'Source Code Pro', monospace" fontSize={10} fill="#6f6a5e">{i + 1}</text>
        ))}

        <g transform={`translate(${GUT}, ${GUT})`}>
          {/* Suelos, solo en las celdas que existen */}
          {rooms.map(room => room.cells.map(c => (
            <rect key={`f-${c.row}-${c.col}`} x={c.col * S} y={c.row * S} width={S} height={S}
                  fill={`url(#floor-${room.id})`} />
          )))}

          {objects.filter(o => !OBJECT_TYPES[o.type].occupiable).map(o => (
            <rect key={`b-${o.row}-${o.col}`} x={o.col * S} y={o.row * S} width={S} height={S}
                  fill="url(#blocked-hatch)" />
          ))}

          {rooms.map(room => (
            <text key={`l-${room.id}`}
                  x={room.labelCell.col * S + S / 2} y={room.labelCell.row * S + 11}
                  textAnchor="middle" fontSize={Math.max(7, S * 0.115)} fill="rgba(0,0,0,0.42)"
                  fontFamily="Georgia, serif" fontWeight="700" letterSpacing="0.8"
                  style={{ pointerEvents: 'none' }}>
              {room.name.toUpperCase()}
            </text>
          ))}

          {objects.map(o => (
            <g key={`o-${o.row}-${o.col}`}
               transform={`translate(${o.col * S + S / 2}, ${o.row * S + S / 2 + S * 0.06})`}
               style={{ pointerEvents: 'none' }} filter="url(#board-vignette)">
              <Furniture type={o.type} s={S} />
            </g>
          ))}

          {/* Rejilla, solo dentro de la planta */}
          {liveCells.map(c => (
            <rect key={`g-${c.row}-${c.col}`} x={c.col * S} y={c.row * S} width={S} height={S}
                  fill="none" stroke="rgba(0,0,0,0.07)" strokeWidth={1} style={{ pointerEvents: 'none' }} />
          ))}

          {doors.map((d, i) => {
            const horizontal = d.y1 === d.y2;
            const mx = (d.x1 + d.x2) / 2, my = (d.y1 + d.y2) / 2;
            const half = S * 0.24;
            return (
              <g key={`d${i}`} style={{ pointerEvents: 'none' }}>
                {horizontal ? (
                  <>
                    <line x1={d.x1} y1={my} x2={mx - half} y2={my} stroke="#3a2f22" strokeWidth={3.5} strokeLinecap="round" />
                    <line x1={mx + half} y1={my} x2={d.x2} y2={my} stroke="#3a2f22" strokeWidth={3.5} strokeLinecap="round" />
                    <line x1={mx - half} y1={my} x2={mx + half} y2={my} stroke="rgba(180,150,110,0.7)" strokeWidth={2} strokeDasharray="3 3" />
                  </>
                ) : (
                  <>
                    <line x1={mx} y1={d.y1} x2={mx} y2={my - half} stroke="#3a2f22" strokeWidth={3.5} strokeLinecap="round" />
                    <line x1={mx} y1={my + half} x2={mx} y2={d.y2} stroke="#3a2f22" strokeWidth={3.5} strokeLinecap="round" />
                    <line x1={mx} y1={my - half} x2={mx} y2={my + half} stroke="rgba(180,150,110,0.7)" strokeWidth={2} strokeDasharray="3 3" />
                  </>
                )}
              </g>
            );
          })}

          {walls.map((w, i) => (
            <line key={`w${i}`} x1={w.x1} y1={w.y1} x2={w.x2} y2={w.y2}
                  stroke={w.outer ? '#2a2118' : '#3a2f22'}
                  strokeWidth={w.outer ? 5 : 3.5} strokeLinecap="square"
                  style={{ pointerEvents: 'none' }} />
          ))}

          {/* Descartes */}
          {!showSolution && liveCells.map(({ row: r, col: c }) => {
            if (!eliminated.has(`${r},${c}`)) return null;
            if (placedAt(r, c)) return null;
            const o = objAt.get(`${r},${c}`);
            if (o && !OBJECT_TYPES[o.type].occupiable) return null;
            const cx = c * S + S / 2, cy = r * S + S / 2, k = S * 0.2;
            return (
              <g key={`x-${r}-${c}`} style={{ pointerEvents: 'none' }} opacity={0.55}>
                <line x1={cx - k} y1={cy - k} x2={cx + k} y2={cy + k} stroke="#a8322a" strokeWidth={3} strokeLinecap="round" />
                <line x1={cx + k} y1={cy - k} x2={cx - k} y2={cy + k} stroke="#a8322a" strokeWidth={3} strokeLinecap="round" />
              </g>
            );
          })}

          {hint && !showSolution && (
            <circle className="hint-ring" cx={hint.col * S + S / 2} cy={hint.row * S + S / 2} r={S * 0.4}
                    fill="none" stroke="#f0c040" strokeWidth={3} style={{ pointerEvents: 'none' }} />
          )}

          {/* Fichas del jugador */}
          {!showSolution && people.map((p, i) => {
            const u = userPlacements[p.id];
            if (!u) return null;
            return (
              <g key={`t-${p.id}`} transform={`translate(${u.col * S + S / 2}, ${u.row * S + S / 2})`}
                 style={{ pointerEvents: 'none' }}>
                <Token name={p.name} s={S}
                       color={p.isVictim ? '#5d6d7e' : SUSPECT_COLORS[i % SUSPECT_COLORS.length]}
                       variant={p.isVictim ? 'victim' : 'placed'} />
              </g>
            );
          })}

          {/* Solución */}
          {showSolution && people.map((p, i) => (
            <g key={`sol-${p.id}`} transform={`translate(${p.col * S + S / 2}, ${p.row * S + S / 2})`}
               style={{ pointerEvents: 'none' }}>
              <Token name={p.name} s={S}
                     color={p.isVictim ? '#5d6d7e' : p.id === puzzle.murderer ? '#c0392b' : SUSPECT_COLORS[i % SUSPECT_COLORS.length]}
                     variant={p.isVictim ? 'victim' : p.id === puzzle.murderer ? 'murderer' : 'solved'} />
            </g>
          ))}

          {/* Zonas clicables, solo sobre celdas existentes */}
          {liveCells.map(({ row: r, col: c }) => {
            const o = objAt.get(`${r},${c}`);
            const blocked = o && !OBJECT_TYPES[o.type].occupiable;
            return (
              <rect key={`hit-${r}-${c}`} className={`cell-hit ${blocked ? 'cell-hit-blocked' : ''}`}
                    x={c * S} y={r * S} width={S} height={S} fill="transparent"
                    onClick={() => gameStatus === 'playing' && !blocked && onCellClick(r, c)}
                    style={{ cursor: gameStatus === 'playing' && !blocked ? 'pointer' : 'default' }}>
                {o && (
                  <title>
                    {OBJECT_TYPES[o.type].def}
                    {blocked ? ' — bloquea la casilla' : ' — se puede estar encima'}
                  </title>
                )}
              </rect>
            );
          })}
        </g>
      </svg>
    </div>
  );
}

export { SUSPECT_COLORS };
