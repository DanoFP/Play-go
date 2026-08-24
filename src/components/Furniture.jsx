// Mobiliario dibujado en SVG. Cada componente pinta dentro de una caja de
// `s`×`s` centrada en el origen del <g> que lo contiene.
//
// Criterio de diseño: lo que distingue a un mueble de otro tiene que ser la
// SILUETA, no el color, porque a 62px y sobre suelos de colores distintos el
// color solo no alcanza. Además los ocupables (cama, silla, sofá, alfombra) se
// dibujan bajos y apaisados —cosas sobre las que uno se pone— y los que
// bloquean la casilla se dibujan altos y verticales.

const OUT = '#3d2e1e';        // contorno común, da nitidez sobre cualquier suelo
const SW  = 1.4;

// ── Ocupables: bajos, apaisados ─────────────────────────────────────────────

function Bed({ s }) {
  const w = s * 0.84, h = s * 0.6;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* cabecero */}
      <rect x={x - 1} y={y - h * 0.14} width={w + 2} height={h * 0.2} rx={2} fill="#7b5836" />
      <rect x={x} y={y} width={w} height={h} rx={3} fill="#efe8d8" />
      {/* almohada */}
      <rect x={x + w * 0.06} y={y + h * 0.08} width={w * 0.88} height={h * 0.26} rx={3} fill="#fff" />
      {/* manta con doblez */}
      <rect x={x} y={y + h * 0.45} width={w} height={h * 0.55} rx={3} fill="#9c4741" />
      <line x1={x} y1={y + h * 0.58} x2={x + w} y2={y + h * 0.58} stroke="#7d3833" strokeWidth={1.6} />
    </g>
  );
}

function Chair({ s }) {
  const w = s * 0.46, h = s * 0.62;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* respaldo alto con listones: es lo que la separa de la mesa */}
      <rect x={x} y={y} width={w} height={h * 0.5} rx={2} fill="#c79a5e" />
      <line x1={x + w * 0.33} y1={y + 2} x2={x + w * 0.33} y2={y + h * 0.5 - 2} stroke={OUT} strokeWidth={1.1} />
      <line x1={x + w * 0.66} y1={y + 2} x2={x + w * 0.66} y2={y + h * 0.5 - 2} stroke={OUT} strokeWidth={1.1} />
      {/* asiento */}
      <rect x={x - w * 0.1} y={y + h * 0.5} width={w * 1.2} height={h * 0.2} rx={2} fill="#a87c47" />
      {/* patas */}
      <rect x={x - w * 0.04} y={y + h * 0.7} width={w * 0.14} height={h * 0.3} fill="#8a6335" />
      <rect x={x + w * 0.9} y={y + h * 0.7} width={w * 0.14} height={h * 0.3} fill="#8a6335" />
    </g>
  );
}

function Sofa({ s }) {
  const w = s * 0.86, h = s * 0.52;
  const x = -w / 2, y = -h / 2;
  const arm = w * 0.15;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      <rect x={x} y={y} width={w} height={h * 0.5} rx={4} fill="#4a7f86" />
      {/* dos cojines: firma visual del sofá frente a la cama */}
      <rect x={x + arm} y={y + h * 0.34} width={(w - arm * 2) / 2 - 1} height={h * 0.5} rx={3} fill="#69a6ad" />
      <rect x={x + arm + (w - arm * 2) / 2 + 1} y={y + h * 0.34} width={(w - arm * 2) / 2 - 1} height={h * 0.5} rx={3} fill="#69a6ad" />
      {/* apoyabrazos */}
      <rect x={x} y={y + h * 0.28} width={arm} height={h * 0.68} rx={4} fill="#3d6b71" />
      <rect x={x + w - arm} y={y + h * 0.28} width={arm} height={h * 0.68} rx={4} fill="#3d6b71" />
    </g>
  );
}

function Rug({ s }) {
  const w = s * 0.84, h = s * 0.52;
  const rx = w / 2, ry = h / 2;
  // Flecos: dicen "textil" y evitan que se lea como bandeja o mesa.
  const fringe = [];
  for (let i = 0; i < 9; i++) {
    const t = -1 + (2 * i) / 8;
    const px = t * rx * 0.94;
    const py = Math.sqrt(Math.max(0, 1 - (px / rx) ** 2)) * ry;
    fringe.push(
      <line key={`f${i}`} x1={px} y1={-py} x2={px} y2={-py - 3} stroke="#c98f5f" strokeWidth={1.2} />,
      <line key={`g${i}`} x1={px} y1={py} x2={px} y2={py + 3} stroke="#c98f5f" strokeWidth={1.2} />
    );
  }
  return (
    <g>
      {fringe}
      <ellipse cx={0} cy={0} rx={rx} ry={ry} fill="#b06642" stroke={OUT} strokeWidth={SW} />
      <ellipse cx={0} cy={0} rx={rx * 0.72} ry={ry * 0.66} fill="none" stroke="#e0b184" strokeWidth={1.5} />
      <ellipse cx={0} cy={0} rx={rx * 0.38} ry={ry * 0.34} fill="#8d4b2e" stroke="#e0b184" strokeWidth={1.3} />
    </g>
  );
}

// ── Bloqueantes: altos, verticales ──────────────────────────────────────────

function Table({ s }) {
  const w = s * 0.74, h = s * 0.5;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* patas abiertas, visibles a los cuatro lados */}
      <line x1={-w * 0.3} y1={0} x2={-w * 0.4} y2={h * 0.62} stroke="#6f5232" strokeWidth={2.6} />
      <line x1={w * 0.3} y1={0} x2={w * 0.4} y2={h * 0.62} stroke="#6f5232" strokeWidth={2.6} />
      <line x1={-w * 0.12} y1={0} x2={-w * 0.16} y2={h * 0.62} stroke="#5d4429" strokeWidth={2.2} />
      <line x1={w * 0.12} y1={0} x2={w * 0.16} y2={h * 0.62} stroke="#5d4429" strokeWidth={2.2} />
      <ellipse cx={0} cy={-h * 0.16} rx={w / 2} ry={h * 0.34} fill="#8a6538" />
      <ellipse cx={0} cy={-h * 0.22} rx={w / 2 - 3} ry={h * 0.27} fill="#a37c48" />
    </g>
  );
}

function Window({ s }) {
  const w = s * 0.62, h = s * 0.58;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      <rect x={x - 2} y={y - 2} width={w + 4} height={h + 4} rx={2} fill="#7b5836" />
      <rect x={x} y={y} width={w} height={h} fill="#9fc9e2" />
      <path d={`M${x} ${y + h * 0.8} L${x + w} ${y + h * 0.15} L${x + w} ${y + h * 0.45} L${x} ${y + h}Z`}
            fill="#cfe6f3" stroke="none" opacity={0.8} />
      <line x1={0} y1={y} x2={0} y2={y + h} strokeWidth={2.2} />
      <line x1={x} y1={0} x2={x + w} y2={0} strokeWidth={2.2} />
      {/* alféizar */}
      <rect x={x - 4} y={y + h + 2} width={w + 8} height={3.5} rx={1.5} fill="#6b4b2c" />
    </g>
  );
}

function Plant({ s }) {
  const pw = s * 0.36, ph = s * 0.26, top = s * 0.1;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* hojas en abanico, no una bola: se distingue del árbol/arbusto genérico */}
      <line x1={0} y1={top} x2={0} y2={-s * 0.06} stroke="#3d6b3f" strokeWidth={2.2} />
      <ellipse cx={-s * 0.15} cy={-s * 0.08} rx={s * 0.13} ry={s * 0.07} fill="#4f9455" transform="rotate(-28 -6 -3)" />
      <ellipse cx={s * 0.15} cy={-s * 0.08} rx={s * 0.13} ry={s * 0.07} fill="#3f7a44" transform="rotate(28 6 -3)" />
      <ellipse cx={0} cy={-s * 0.2} rx={s * 0.1} ry={s * 0.14} fill="#4a8a4f" />
      {/* maceta troncocónica */}
      <path d={`M${-pw / 2} ${top} L${pw / 2} ${top} L${pw / 2 - 4} ${top + ph} L${-pw / 2 + 4} ${top + ph} Z`} fill="#b5674a" />
      <rect x={-pw / 2 - 1} y={top - 1} width={pw + 2} height={4} rx={1.5} fill="#9c5339" />
    </g>
  );
}

function Bookshelf({ s }) {
  const w = s * 0.66, h = s * 0.76;
  const x = -w / 2, y = -h / 2;
  const rows = 3;
  const palette = ['#9c4741', '#3f6b8a', '#7a8a45', '#c08a3e', '#6a4a7a', '#4a8a7a'];
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      <rect x={x} y={y} width={w} height={h} rx={2} fill="#6d5133" />
      {Array.from({ length: rows }, (_, r) => {
        const sy = y + 3 + r * ((h - 4) / rows);
        const sh = (h - 4) / rows - 3.5;
        return (
          <g key={r}>
            {Array.from({ length: 5 }, (_, b) => {
              const bw = (w - 7) / 5;
              // Alturas desiguales: leen como libros, no como barras de color.
              const hh = sh * (0.72 + ((b + r * 2) % 3) * 0.14);
              return (
                <rect key={b} x={x + 3.5 + b * bw} y={sy + (sh - hh)} width={bw - 1.2} height={hh}
                      fill={palette[(b + r * 2) % palette.length]} stroke="none" />
              );
            })}
            <rect x={x + 1.5} y={sy + sh} width={w - 3} height={2.4} fill="#4e3a24" stroke="none" />
          </g>
        );
      })}
    </g>
  );
}

function Fireplace({ s }) {
  const w = s * 0.74, h = s * 0.64;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* repisa saliente: marca la chimenea aunque el fuego sea chico */}
      <rect x={x - 3} y={y} width={w + 6} height={h * 0.16} rx={1.5} fill="#a09488" />
      <rect x={x} y={y + h * 0.14} width={w} height={h * 0.86} fill="#8d8378" />
      <path d={`M${x + 5} ${y + h} L${x + 5} ${y + h * 0.52} Q0 ${y + h * 0.26} ${x + w - 5} ${y + h * 0.52} L${x + w - 5} ${y + h}Z`}
            fill="#241d17" />
      <path d={`M0 ${y + h - 3} q-8 -9 -2 -16 q4.5 7 6.5 2 q4.5 7 -1 14 Z`} fill="#e08a2e" stroke="none" />
      <path d={`M0 ${y + h - 3} q-4 -5 -1 -9 q3 4 4 1 q2 4 -1 8 Z`} fill="#f7d867" stroke="none" />
    </g>
  );
}

function Wardrobe({ s }) {
  const w = s * 0.6, h = s * 0.78;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* cornisa: lo separa del reloj, que es estrecho y sin remate */}
      <rect x={x - 3} y={y} width={w + 6} height={h * 0.1} rx={1.5} fill="#5d4429" />
      <rect x={x} y={y + h * 0.09} width={w} height={h * 0.91} fill="#8a6536" />
      <rect x={x + 3} y={y + h * 0.14} width={w / 2 - 4.5} height={h * 0.8} rx={1.5} fill="#a07d4e" />
      <rect x={x + w / 2 + 1.5} y={y + h * 0.14} width={w / 2 - 4.5} height={h * 0.8} rx={1.5} fill="#a07d4e" />
      <circle cx={x + w / 2 - 3.5} cy={y + h * 0.54} r={2} fill="#e8c96a" />
      <circle cx={x + w / 2 + 3.5} cy={y + h * 0.54} r={2} fill="#e8c96a" />
      {/* patitas */}
      <rect x={x + 2} y={y + h} width={5} height={3} fill="#5d4429" />
      <rect x={x + w - 7} y={y + h} width={5} height={3} fill="#5d4429" />
    </g>
  );
}

function Barrel({ s }) {
  const w = s * 0.5, h = s * 0.62;
  const x = -w / 2, y = -h / 2;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* perfil abombado */}
      <path d={`M${x} ${y + 4} q${-w * 0.16} ${h / 2} 0 ${h - 8}
                 q${w * 0.5} 5 ${w} 0 q${w * 0.16} ${-(h - 8) / 1} 0 ${-(h - 8)}
                 q${-w * 0.5} -5 ${-w} 0 Z`} fill="#b0793f" />
      <ellipse cx={0} cy={y + 4} rx={w / 2} ry={w * 0.17} fill="#c79154" />
      <rect x={x - w * 0.09} y={y + h * 0.28} width={w * 1.18} height={3} rx={1.2} fill="#4f4438" stroke="none" />
      <rect x={x - w * 0.09} y={y + h * 0.66} width={w * 1.18} height={3} rx={1.2} fill="#4f4438" stroke="none" />
    </g>
  );
}

function Clock({ s }) {
  const w = s * 0.3, h = s * 0.8;
  const x = -w / 2, y = -h / 2;
  const face = y + w * 0.68;
  return (
    <g stroke={OUT} strokeWidth={SW} strokeLinejoin="round">
      {/* remate triangular: silueta inconfundible frente al armario */}
      <path d={`M${x - 3} ${y + 5} L0 ${y - 3} L${x + w + 3} ${y + 5} Z`} fill="#5d4429" />
      <rect x={x} y={y + 4} width={w} height={h - 4} rx={2} fill="#6d5133" />
      <circle cx={0} cy={face} r={w * 0.38} fill="#f2e7cc" />
      <line x1={0} y1={face} x2={0} y2={face - w * 0.24} strokeWidth={1.4} />
      <line x1={0} y1={face} x2={w * 0.18} y2={face} strokeWidth={1.4} />
      {/* péndulo visible */}
      <rect x={x + 3} y={face + w * 0.55} width={w - 6} height={h - w * 1.35} rx={1.5} fill="#3f3020" />
      <line x1={0} y1={face + w * 0.6} x2={0} y2={y + h * 0.82} stroke="#c9a75a" strokeWidth={1.2} />
      <circle cx={0} cy={y + h * 0.84} r={w * 0.22} fill="#e8c96a" />
    </g>
  );
}

const RENDERERS = {
  bed: Bed, chair: Chair, sofa: Sofa, rug: Rug, table: Table,
  window: Window, plant: Plant, bookshelf: Bookshelf,
  fireplace: Fireplace, wardrobe: Wardrobe, barrel: Barrel, clock: Clock,
};

export default function Furniture({ type, s }) {
  const C = RENDERERS[type];
  return C ? <C s={s} /> : null;
}
