import { SUSPECT_COLORS } from './GameBoard.jsx';

// Las fichas llegan ya ordenadas por la secuencia en que la deducción las va
// fijando: la primera se resuelve con su propia declaración y cada siguiente
// se abre con lo que dejaron las anteriores. El número lo hace explícito.

function Card({ person, index, color, isVictim, isActive, isMurderer, placed, showSolution, onClick, clickable }) {
  return (
    <div
      className={[
        isVictim ? 'victim-card' : 'suspect-card',
        isActive ? 'suspect-active' : '',
        isMurderer ? 'suspect-murderer' : '',
        placed && !showSolution ? 'suspect-placed' : '',
      ].join(' ')}
      onClick={clickable ? onClick : undefined}
      style={{ '--accent': color, cursor: clickable ? 'pointer' : 'default' }}
    >
      <div className="card-step" title="Orden sugerido de deducción">{index}</div>

      <div className={`suspect-avatar ${isVictim ? 'victim-avatar' : ''}`} style={{ background: color }}>
        <svg viewBox="-12 -12 24 24" width="100%" height="100%">
          {isVictim ? (
            <g fill="rgba(255,255,255,0.94)">
              <rect x="-1.6" y="-8" width="3.2" height="16" rx="0.8" />
              <rect x="-5.5" y="-3.5" width="11" height="3.2" rx="0.8" />
            </g>
          ) : (
            <g fill="rgba(255,255,255,0.94)">
              <circle cx="0" cy="-3.5" r="3.6" />
              <path d="M-6.4 7 a6.4 6 0 0 1 12.8 0 Z" />
            </g>
          )}
        </svg>
      </div>

      <div className="suspect-info">
        <div className="suspect-name">
          {person.name}
          {isVictim && <span className="victim-tag">víctima</span>}
          {isMurderer && <span className="murderer-badge">☠ ASESINO</span>}
        </div>

        <ul className="clue-list">
          {person.clues.map((c, k) => <li key={k} className="suspect-clue">{c}</li>)}
        </ul>

        {placed && !showSolution && (
          <div className="placed-label">F{placed.row + 1} · C{placed.col + 1}</div>
        )}
        {showSolution && (
          <div className="placed-label">F{person.row + 1} · C{person.col + 1}</div>
        )}
      </div>
    </div>
  );
}

export default function SuspectList({ puzzle, userPlacements, activeSuspect, onSelectSuspect, gameStatus }) {
  const { suspects, victim, murderer } = puzzle;
  const showSolution = gameStatus === 'won' || gameStatus === 'revealed';
  const playing = gameStatus === 'playing';

  // La víctima se intercala en la lista en su propio lugar de la secuencia.
  const people = [
    ...suspects.map((s, i) => ({ p: s, color: SUSPECT_COLORS[i % SUSPECT_COLORS.length], isVictim: false })),
    { p: { ...victim, id: 'victim' }, color: '#5d6d7e', isVictim: true },
  ].sort((a, b) => a.p.step - b.p.step);

  return (
    <div className="suspect-list">
      <h3 className="panel-title">Declaraciones · en orden de deducción</h3>

      {people.map(({ p, color, isVictim }, i) => (
        <Card
          key={p.id}
          person={p}
          index={i + 1}
          color={color}
          isVictim={isVictim}
          isActive={activeSuspect === p.id}
          isMurderer={showSolution && p.id === murderer}
          placed={userPlacements[p.id]}
          showSolution={showSolution}
          clickable={playing}
          onClick={() => onSelectSuspect(p.id)}
        />
      ))}
    </div>
  );
}
