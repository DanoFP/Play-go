const COLORS = [
  '#e74c3c', '#3498db', '#2ecc71', '#f39c12', '#9b59b6',
  '#1abc9c', '#e67e22', '#34495e', '#e91e63', '#00bcd4',
];

export default function SuspectList({ puzzle, userPlacements, activeSuspect, onSelectSuspect, gameStatus }) {
  const { suspects, victim, murderer } = puzzle;
  const showSolution = gameStatus === 'won' || gameStatus === 'revealed';

  return (
    <div className="suspect-list">
      <h3 className="panel-title">Sospechosos</h3>

      {suspects.map((s, i) => {
        const placed = userPlacements[s.id];
        const isActive = activeSuspect === s.id;
        const isMurderer = showSolution && s.id === murderer;

        return (
          <div
            key={s.id}
            className={`suspect-card ${isActive ? 'suspect-active' : ''} ${isMurderer ? 'suspect-murderer' : ''} ${placed ? 'suspect-placed' : ''}`}
            onClick={() => gameStatus === 'playing' && onSelectSuspect(s.id)}
            style={{ '--accent': COLORS[i % COLORS.length] }}
          >
            <div className="suspect-avatar" style={{ background: COLORS[i % COLORS.length] }}>
              {s.name[0]}
            </div>
            <div className="suspect-info">
              <div className="suspect-name">
                {s.name}
                {isMurderer && <span className="murderer-badge"> ☠ ASESINO</span>}
              </div>
              <div className="suspect-clue">"{s.clue}"</div>
              {placed && !showSolution && (
                <div className="suspect-placed-label">Fila {placed.row + 1}, Col {placed.col + 1}</div>
              )}
              {showSolution && (
                <div className="suspect-placed-label">Fila {s.row + 1}, Col {s.col + 1}</div>
              )}
            </div>
          </div>
        );
      })}

      <div className="victim-card">
        <div className="suspect-avatar victim-avatar">✝</div>
        <div className="suspect-info">
          <div className="suspect-name">{victim.name} <span style={{ opacity: 0.6, fontSize: 11 }}>(víctima)</span></div>
          <div className="suspect-clue">"{victim.clue}"</div>
          {showSolution && (
            <div className="suspect-placed-label">Fila {victim.row + 1}, Col {victim.col + 1}</div>
          )}
        </div>
      </div>
    </div>
  );
}
