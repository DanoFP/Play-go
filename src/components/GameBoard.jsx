import { OBJECT_TYPES, canPlaceAt } from '../utils/puzzleGenerator.js';

const CELL_SIZE = 64;

export default function GameBoard({ puzzle, userPlacements, eliminated, onCellClick, gameStatus }) {
  const { N, rooms, suspects, victim } = puzzle;

  function getRoomAt(row, col) {
    return rooms.find(r => r.cells.some(c => c.row === row && c.col === col));
  }

  function getObjectAt(row, col) {
    for (const room of rooms) {
      const obj = room.objects.find(o => o.row === row && o.col === col);
      if (obj) return obj;
    }
    return null;
  }

  function getSuspectAt(row, col) {
    return suspects.find(s => {
      const p = userPlacements[s.id];
      return p && p.row === row && p.col === col;
    });
  }

  function isEliminated(row, col) {
    return eliminated.has(`${row},${col}`);
  }

  const showSolution = gameStatus === 'won' || gameStatus === 'revealed';

  function getSolutionSuspectAt(row, col) {
    if (!showSolution) return null;
    return suspects.find(s => s.row === row && s.col === col);
  }

  function isVictimCell(row, col) {
    return showSolution && victim.row === row && victim.col === col;
  }

  const boardW = N * CELL_SIZE;
  const boardH = N * CELL_SIZE;

  return (
    <div className="board-wrapper">
      <div
        className="board"
        style={{ width: boardW, height: boardH, position: 'relative' }}
      >
        {/* Room backgrounds */}
        {rooms.map(room => (
          <div
            key={room.id}
            className="room-bg"
            style={{
              position: 'absolute',
              left: room.colStart * CELL_SIZE,
              top: room.rowStart * CELL_SIZE,
              width: room.width * CELL_SIZE,
              height: room.height * CELL_SIZE,
              background: room.color,
              border: '2px solid #555',
              boxSizing: 'border-box',
              borderRadius: 4,
            }}
          />
        ))}

        {/* Room labels */}
        {rooms.map(room => (
          <div
            key={`label-${room.id}`}
            className="room-label"
            style={{
              position: 'absolute',
              left: room.colStart * CELL_SIZE,
              top: room.rowStart * CELL_SIZE,
              width: room.width * CELL_SIZE,
              fontSize: 10,
              textAlign: 'center',
              fontWeight: 700,
              color: '#444',
              paddingTop: 3,
              pointerEvents: 'none',
              zIndex: 10,
              textTransform: 'uppercase',
              letterSpacing: 1,
            }}
          >
            {room.name}
          </div>
        ))}

        {/* Grid cells */}
        {Array.from({ length: N }, (_, row) =>
          Array.from({ length: N }, (_, col) => {
            const obj = getObjectAt(row, col);
            const placedSuspect = getSuspectAt(row, col);
            const solutionSuspect = getSolutionSuspectAt(row, col);
            const xed = isEliminated(row, col);
            const victimHere = isVictimCell(row, col);
            const isMurdererCell = showSolution && solutionSuspect && solutionSuspect.id === puzzle.murderer;
            const blocked = obj && !OBJECT_TYPES[obj.type].occupiable;

            return (
              <div
                key={`${row}-${col}`}
                className={`cell ${xed && !blocked ? 'cell-x' : ''} ${isMurdererCell ? 'cell-murderer' : ''} ${blocked ? 'cell-blocked' : ''}`}
                style={{
                  position: 'absolute',
                  left: col * CELL_SIZE,
                  top: row * CELL_SIZE,
                  width: CELL_SIZE,
                  height: CELL_SIZE,
                  boxSizing: 'border-box',
                  border: '1px solid rgba(0,0,0,0.15)',
                  cursor: gameStatus === 'playing' && !blocked ? 'pointer' : 'default',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  zIndex: 5,
                  userSelect: 'none',
                }}
                onClick={() => gameStatus === 'playing' && onCellClick(row, col)}
              >
                {/* Object icon */}
                {obj && (
                  <span style={{ fontSize: 20, opacity: 0.7, position: 'absolute', top: 4, right: 4 }}>
                    {OBJECT_TYPES[obj.type].icon}
                  </span>
                )}

                {/* X mark */}
                {xed && !placedSuspect && !victimHere && !solutionSuspect && (
                  <span style={{ fontSize: 22, color: '#c0392b', fontWeight: 900 }}>✕</span>
                )}

                {/* Placed suspect (user) */}
                {placedSuspect && !showSolution && (
                  <div className="token token-suspect">
                    {placedSuspect.name[0]}
                  </div>
                )}

                {/* Solution suspects */}
                {showSolution && solutionSuspect && (
                  <div className={`token ${isMurdererCell ? 'token-murderer' : 'token-suspect-solved'}`}>
                    {solutionSuspect.name[0]}
                    <span style={{ fontSize: 9, display: 'block', lineHeight: 1 }}>
                      {solutionSuspect.name}
                    </span>
                  </div>
                )}

                {/* Victim */}
                {victimHere && (
                  <div className="token token-victim">
                    {victim.name[0]}
                    <span style={{ fontSize: 9, display: 'block', lineHeight: 1 }}>
                      {victim.name}
                    </span>
                  </div>
                )}
              </div>
            );
          })
        )}

        {/* Grid lines overlay */}
        <svg
          style={{ position: 'absolute', top: 0, left: 0, pointerEvents: 'none', zIndex: 6 }}
          width={boardW}
          height={boardH}
        >
          {Array.from({ length: N + 1 }, (_, i) => (
            <line key={`h${i}`} x1={0} y1={i * CELL_SIZE} x2={boardW} y2={i * CELL_SIZE} stroke="rgba(0,0,0,0.08)" strokeWidth={1} />
          ))}
          {Array.from({ length: N + 1 }, (_, i) => (
            <line key={`v${i}`} x1={i * CELL_SIZE} y1={0} x2={i * CELL_SIZE} y2={boardH} stroke="rgba(0,0,0,0.08)" strokeWidth={1} />
          ))}
        </svg>
      </div>
    </div>
  );
}
