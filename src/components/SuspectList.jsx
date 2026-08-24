import { SUSPECT_COLORS } from './GameBoard.jsx';

export default function SuspectList({ puzzle, userPlacements, activeSuspect, onSelectSuspect, gameStatus }) {
  const { suspects, victim, murderer } = puzzle;
  const showSolution = gameStatus === 'won' || gameStatus === 'revealed';

  return (
    <div className="suspect-list">
      <h3 className="panel-title">Declaraciones</h3>

      {suspects.map((s, i) => {
        const placed = userPlacements[s.id];
        const isActive = activeSuspect === s.id;
        const isMurderer = showSolution && s.id === murderer;
        const color = SUSPECT_COLORS[i % SUSPECT_COLORS.length];

        return (
          <div
            key={s.id}
            className={[
              'suspect-card',
              isActive ? 'suspect-active' : '',
              isMurderer ? 'suspect-murderer' : '',
              placed && !showSolution ? 'suspect-placed' : '',
            ].join(' ')}
            onClick={() => gameStatus === 'playing' && onSelectSuspect(s.id)}
            style={{ '--accent': color }}
          >
            <div className="suspect-avatar" style={{ background: color }}>
              <svg viewBox="-12 -12 24 24" width="100%" height="100%">
                <circle cx="0" cy="-3.5" r="3.6" fill="rgba(255,255,255,0.94)" />
                <path d="M-6.4 7 a6.4 6 0 0 1 12.8 0 Z" fill="rgba(255,255,255,0.94)" />
              </svg>
            </div>

            <div className="suspect-info">
              <div className="suspect-name">
                {s.name}
                {isMurderer && <span className="murderer-badge">☠ ASESINO</span>}
              </div>

              <ul className="clue-list">
                {s.clues.map((c, k) => (
                  <li key={k} className="suspect-clue">{c}</li>
                ))}
              </ul>

              {placed && !showSolution && (
                <div className="placed-label">F{placed.row + 1} · C{placed.col + 1}</div>
              )}
              {showSolution && (
                <div className="placed-label">F{s.row + 1} · C{s.col + 1}</div>
              )}
            </div>
          </div>
        );
      })}

      <div className="victim-card">
        <div className="suspect-avatar victim-avatar">✝</div>
        <div className="suspect-info">
          <div className="suspect-name">
            {victim.name} <span className="victim-tag">víctima</span>
          </div>
          <ul className="clue-list">
            <li className="suspect-clue">{victim.clue}</li>
          </ul>
          {showSolution && (
            <div className="placed-label">F{victim.row + 1} · C{victim.col + 1}</div>
          )}
        </div>
      </div>
    </div>
  );
}
