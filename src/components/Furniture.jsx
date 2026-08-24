// Mobiliario dibujado en SVG. Cada componente pinta dentro de una caja de
// `s`×`s` centrada en el origen del <g> que lo contiene.

const wood  = '#8a6a45';
const wood2 = '#6f5436';
const dark  = '#4a3a28';

function Bed({ s }) {
  const w = s * 0.78, h = s * 0.62;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={3} fill="#c9b79a" stroke={wood2} strokeWidth={1.4} />
      <rect x={x + 2} y={y + 2} width={w - 4} height={h * 0.34} rx={2} fill="#f0ece2" />
      <rect x={x + 2} y={y + h * 0.4} width={w - 4} height={h * 0.56} rx={2} fill="#9a4f4a" />
      <path d={`M${x + 2} ${y + h * 0.58} h${w - 4}`} stroke="#7d3f3b" strokeWidth={1} />
    </g>
  );
}

function Chair({ s }) {
  const w = s * 0.5, h = s * 0.56;
  return (
    <g>
      <rect x={-w / 2} y={-h / 2} width={w} height={h * 0.42} rx={2} fill={wood} stroke={wood2} strokeWidth={1.2} />
      <rect x={-w / 2} y={-h / 2 + h * 0.42} width={w} height={h * 0.3} rx={2} fill="#a5825a" stroke={wood2} strokeWidth={1.2} />
      <rect x={-w / 2 + 1.5} y={h / 2 - h * 0.28} width={2.2} height={h * 0.28} fill={wood2} />
      <rect x={w / 2 - 3.7} y={h / 2 - h * 0.28} width={2.2} height={h * 0.28} fill={wood2} />
    </g>
  );
}

function Sofa({ s }) {
  const w = s * 0.8, h = s * 0.5;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h * 0.45} rx={3} fill="#5f7a8a" stroke="#3f5462" strokeWidth={1.3} />
      <rect x={x} y={y + h * 0.4} width={w} height={h * 0.6} rx={3} fill="#7595a6" stroke="#3f5462" strokeWidth={1.3} />
      <rect x={x} y={y + h * 0.35} width={w * 0.14} height={h * 0.65} rx={3} fill="#516d7c" />
      <rect x={x + w * 0.86} y={y + h * 0.35} width={w * 0.14} height={h * 0.65} rx={3} fill="#516d7c" />
    </g>
  );
}

function Rug({ s }) {
  const w = s * 0.82, h = s * 0.6;
  return (
    <g>
      <rect x={-w / 2} y={-h / 2} width={w} height={h} rx={4} fill="#a8603f" stroke="#7d452c" strokeWidth={1.4} />
      <rect x={-w / 2 + 4} y={-h / 2 + 4} width={w - 8} height={h - 8} rx={2} fill="none" stroke="#d9a877" strokeWidth={1.3} />
      <ellipse cx={0} cy={0} rx={w * 0.2} ry={h * 0.2} fill="none" stroke="#d9a877" strokeWidth={1.3} />
    </g>
  );
}

function Table({ s }) {
  const w = s * 0.72, h = s * 0.5;
  return (
    <g>
      <ellipse cx={0} cy={-h * 0.1} rx={w / 2} ry={h * 0.36} fill={wood} stroke={wood2} strokeWidth={1.4} />
      <ellipse cx={0} cy={-h * 0.16} rx={w / 2 - 3} ry={h * 0.28} fill="#a07c52" />
      <rect x={-2} y={h * 0.16} width={4} height={h * 0.34} fill={wood2} />
      <rect x={-w * 0.18} y={h * 0.46} width={w * 0.36} height={3} rx={1.5} fill={dark} />
    </g>
  );
}

function Window({ s }) {
  const w = s * 0.64, h = s * 0.56;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={2} fill="#a8cbe0" stroke={wood2} strokeWidth={2} />
      <path d={`M${x} ${y + h * 0.75} L${x + w} ${y + h * 0.2}`} stroke="#d3e6f2" strokeWidth={3} opacity={0.7} />
      <line x1={0} y1={y} x2={0} y2={y + h} stroke={wood2} strokeWidth={1.8} />
      <line x1={x} y1={0} x2={x + w} y2={0} stroke={wood2} strokeWidth={1.8} />
    </g>
  );
}

function Plant({ s }) {
  const potW = s * 0.34, potH = s * 0.24;
  return (
    <g>
      <path d={`M${-potW / 2} ${s * 0.08} L${potW / 2} ${s * 0.08} L${potW / 2 - 3} ${s * 0.08 + potH} L${-potW / 2 + 3} ${s * 0.08 + potH} Z`}
            fill="#b5674a" stroke="#8c4b33" strokeWidth={1.2} />
      <ellipse cx={0} cy={-s * 0.08} rx={s * 0.2} ry={s * 0.15} fill="#3f7a44" />
      <ellipse cx={-s * 0.13} cy={-s * 0.02} rx={s * 0.13} ry={s * 0.1} fill="#4d9152" />
      <ellipse cx={s * 0.13} cy={-s * 0.02} rx={s * 0.13} ry={s * 0.1} fill="#356b3b" />
      <line x1={0} y1={-s * 0.02} x2={0} y2={s * 0.09} stroke="#2f5c33" strokeWidth={2} />
    </g>
  );
}

function Bookshelf({ s }) {
  const w = s * 0.66, h = s * 0.68;
  const x = -w / 2, y = -h / 2;
  const books = ['#9a4f4a', '#3f6b8a', '#7a8a45', '#8a6a45', '#6a4a7a'];
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={2} fill="#7a5c3c" stroke={dark} strokeWidth={1.4} />
      {[0, 1, 2].map(shelf => (
        <g key={shelf}>
          {books.map((col, b) => (
            <rect
              key={b}
              x={x + 3 + b * ((w - 6) / books.length)}
              y={y + 3 + shelf * ((h - 4) / 3)}
              width={(w - 6) / books.length - 1}
              height={(h - 4) / 3 - 3}
              fill={books[(b + shelf) % books.length]}
            />
          ))}
          <rect x={x + 1.5} y={y + (shelf + 1) * ((h - 4) / 3)} width={w - 3} height={1.6} fill={dark} />
        </g>
      ))}
    </g>
  );
}

function Fireplace({ s }) {
  const w = s * 0.7, h = s * 0.62;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={2} fill="#8d8378" stroke="#5e564d" strokeWidth={1.4} />
      <path d={`M${x + 5} ${y + h} L${x + 5} ${y + h * 0.42} Q${0} ${y + h * 0.16} ${x + w - 5} ${y + h * 0.42} L${x + w - 5} ${y + h}Z`} fill="#2b241d" />
      <path d={`M0 ${y + h - 4} q-7 -8 -2 -14 q4 6 6 2 q4 6 -1 12 Z`} fill="#e08a2e" />
      <path d={`M0 ${y + h - 4} q-4 -5 -1 -9 q3 4 4 1 q2 4 -1 8 Z`} fill="#f5cf5a" />
    </g>
  );
}

function Wardrobe({ s }) {
  const w = s * 0.56, h = s * 0.72;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={2} fill="#8a6a45" stroke={dark} strokeWidth={1.4} />
      <rect x={x + 3} y={y + 3} width={w / 2 - 4} height={h - 6} rx={1} fill="#9d7c55" stroke={wood2} strokeWidth={0.9} />
      <rect x={x + w / 2 + 1} y={y + 3} width={w / 2 - 4} height={h - 6} rx={1} fill="#9d7c55" stroke={wood2} strokeWidth={0.9} />
      <circle cx={x + w / 2 - 3} cy={0} r={1.8} fill="#e0c060" />
      <circle cx={x + w / 2 + 3} cy={0} r={1.8} fill="#e0c060" />
    </g>
  );
}

function Barrel({ s }) {
  const w = s * 0.48, h = s * 0.6;
  return (
    <g>
      <path d={`M${-w / 2} ${-h / 2} q${w * 0.16} ${h / 2} 0 ${h} l${w} 0 q${-w * 0.16} ${-h / 2} 0 ${-h} Z`}
            fill="#9a6f42" stroke={dark} strokeWidth={1.3} transform={`translate(${-w / 2 + w / 2},0)`} />
      <rect x={-w / 2 - 1} y={-h * 0.26} width={w + 2} height={2.4} fill="#5e5148" />
      <rect x={-w / 2 - 1} y={h * 0.12} width={w + 2} height={2.4} fill="#5e5148" />
      <ellipse cx={0} cy={-h / 2} rx={w / 2} ry={w * 0.16} fill="#b0824f" stroke={dark} strokeWidth={1.1} />
    </g>
  );
}

function Clock({ s }) {
  const w = s * 0.32, h = s * 0.74;
  const x = -w / 2, y = -h / 2;
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={3} fill="#7a5c3c" stroke={dark} strokeWidth={1.4} />
      <circle cx={0} cy={y + w * 0.62} r={w * 0.36} fill="#f0e6d0" stroke={dark} strokeWidth={1.1} />
      <line x1={0} y1={y + w * 0.62} x2={0} y2={y + w * 0.62 - w * 0.22} stroke={dark} strokeWidth={1.3} />
      <line x1={0} y1={y + w * 0.62} x2={w * 0.16} y2={y + w * 0.62} stroke={dark} strokeWidth={1.3} />
      <rect x={x + 2.5} y={y + w * 1.15} width={w - 5} height={h - w * 1.35} rx={1.5} fill="#5c4630" />
      <circle cx={0} cy={y + h * 0.74} r={w * 0.2} fill="#e0c060" />
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
