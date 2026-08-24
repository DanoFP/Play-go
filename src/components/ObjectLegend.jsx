import { OBJECT_TYPES } from '../utils/puzzleGenerator.js';
import Furniture from './Furniture.jsx';

// Las pistas nombran los muebles ("junto a una estantería"), así que el jugador
// necesita poder mapear dibujo → nombre. Solo se listan los tipos que están en
// este tablero, agrupados por si se puede estar encima o si bloquean la casilla,
// que de paso enseña la regla.

function Swatch({ type }) {
  return (
    <svg viewBox="-19 -19 38 38" width="30" height="30" className="legend-swatch">
      {/* Mismo fondo claro que los suelos del tablero: los muebles están
          dibujados para leerse sobre claro, no sobre el panel oscuro. */}
      <rect x="-19" y="-19" width="38" height="38" rx="4" fill="#ddd5c4" />
      <Furniture type={type} s={32} />
    </svg>
  );
}

export default function ObjectLegend({ objects }) {
  const present = [...new Set(objects.map(o => o.type))];
  const occupiable = present.filter(t => OBJECT_TYPES[t].occupiable);
  const blocking   = present.filter(t => !OBJECT_TYPES[t].occupiable);

  const group = (title, types, cls) => types.length > 0 && (
    <div className="legend-group">
      <div className={`legend-group-title ${cls}`}>{title}</div>
      <div className="legend-items">
        {types.map(t => (
          <div key={t} className="legend-item">
            <Swatch type={t} />
            <span className="legend-name">{OBJECT_TYPES[t].def}</span>
          </div>
        ))}
      </div>
    </div>
  );

  return (
    <div className="object-legend">
      <div className="panel-title">En la escena</div>
      {group('Se puede estar encima', occupiable, 'lg-ok')}
      {group('Bloquean la casilla', blocking, 'lg-block')}
    </div>
  );
}
