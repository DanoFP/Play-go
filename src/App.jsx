import { useState, useCallback } from 'react';
import { generatePuzzle, checkPlacement, isCorrectSolution, canPlaceAt, SCENARIOS } from './utils/puzzleGenerator.js';
import GameBoard from './components/GameBoard.jsx';
import SuspectList from './components/SuspectList.jsx';
import './App.css';

function computeAutoEliminated(placements, N) {
  const auto = new Set();
  for (const [, pos] of Object.entries(placements)) {
    if (!pos) continue;
    for (let c = 0; c < N; c++) {
      if (c !== pos.col) auto.add(`${pos.row},${c}`);
    }
    for (let r = 0; r < N; r++) {
      if (r !== pos.row) auto.add(`${r},${pos.col}`);
    }
  }
  return auto;
}

function freshState(numSuspects, scenarioId) {
  const puzzle = generatePuzzle(numSuspects, scenarioId);
  const placements = Object.fromEntries(puzzle.suspects.map(s => [s.id, null]));
  return { puzzle, placements, manualEliminated: new Set(), activeSuspect: puzzle.suspects[0].id };
}

export default function App() {
  const [numSuspects, setNumSuspects] = useState(3);
  const [scenarioId, setScenarioId] = useState(null);
  const [state, setState] = useState(() => freshState(3, null));
  const [gameStatus, setGameStatus] = useState('playing');
  const [message, setMessage] = useState('');

  const { puzzle, placements, manualEliminated, activeSuspect } = state;
  const autoEliminated = computeAutoEliminated(placements, puzzle.N);
  const eliminated = new Set([...autoEliminated, ...manualEliminated]);

  const scenarioGroup = numSuspects <= 3 ? SCENARIOS.small : SCENARIOS.large;

  function newGame(n = numSuspects, sid = scenarioId) {
    setState(freshState(n, sid));
    setGameStatus('playing');
    setMessage('');
  }

  function handleDifficultyChange(n) {
    // Reset scenarioId if switching to a group that doesn't have it
    const newGroup = n <= 3 ? SCENARIOS.small : SCENARIOS.large;
    const newSid = newGroup.find(s => s.id === scenarioId) ? scenarioId : null;
    setNumSuspects(n);
    setScenarioId(newSid);
    setState(freshState(n, newSid));
    setGameStatus('playing');
    setMessage('');
  }

  function handleScenarioChange(sid) {
    setScenarioId(sid);
    setState(freshState(numSuspects, sid));
    setGameStatus('playing');
    setMessage('');
  }

  const handleCellClick = useCallback((row, col) => {
    if (gameStatus !== 'playing') return;

    setState(prev => {
      const key = `${row},${col}`;
      const newManual = new Set(prev.manualEliminated);
      const newPlacements = { ...prev.placements };

      const occupyingSuspect = Object.entries(newPlacements).find(
        ([, p]) => p && p.row === row && p.col === col
      );

      if (prev.activeSuspect) {
        if (occupyingSuspect && occupyingSuspect[0] === prev.activeSuspect) {
          newPlacements[prev.activeSuspect] = null;
          return { ...prev, placements: newPlacements };
        }
        if (occupyingSuspect) return prev;
        if (!canPlaceAt(puzzle, row, col)) return prev;

        newPlacements[prev.activeSuspect] = { row, col };
        newManual.delete(key);
        const check = checkPlacement(puzzle, newPlacements);
        if (!check.valid) return prev;
        return { ...prev, placements: newPlacements, manualEliminated: newManual };
      } else {
        if (occupyingSuspect) return prev;
        if (newManual.has(key)) {
          newManual.delete(key);
        } else {
          newManual.add(key);
        }
        return { ...prev, manualEliminated: newManual };
      }
    });
  }, [gameStatus, puzzle]);

  function handleSelectSuspect(id) {
    setState(prev => ({
      ...prev,
      activeSuspect: prev.activeSuspect === id ? null : id,
    }));
  }

  function handleCheck() {
    if (isCorrectSolution(puzzle, placements)) {
      setGameStatus('won');
      setMessage(`¡Correcto! El asesino es ${puzzle.suspects.find(s => s.id === puzzle.murderer)?.name}.`);
    } else {
      setMessage('Solución incorrecta. Revisa las pistas.');
    }
  }

  function handleReveal() {
    setGameStatus('revealed');
    const murdererName = puzzle.suspects.find(s => s.id === puzzle.murderer)?.name;
    setMessage(`El asesino era ${murdererName}.`);
  }

  const allPlaced = puzzle.suspects.every(s => placements[s.id] !== null);

  const activeScenario = puzzle.scenario;

  return (
    <div className="app">
      <header className="header">
        <div className="case-label">EXPEDIENTE MURDOKU</div>
        <h1 className="title">
          {activeScenario ? `${activeScenario.icon} ${activeScenario.name}` : '🔍 Detective'}
        </h1>
        <p className="subtitle">Encuentra al asesino usando las pistas</p>
      </header>

      <div className="controls-bar">
        <div className="difficulty-group">
          <span className="difficulty-label">Sospechosos:</span>
          {[3, 4, 5].map(n => (
            <button
              key={n}
              className={`btn-diff ${numSuspects === n ? 'btn-diff-active' : ''}`}
              onClick={() => handleDifficultyChange(n)}
            >
              {n}
            </button>
          ))}
        </div>
        <button className="btn btn-new" onClick={() => newGame()}>Nueva partida</button>
      </div>

      <div className="scenario-bar">
        <span className="difficulty-label">Escenario:</span>
        <button
          className={`btn-scenario ${!scenarioId ? 'btn-scenario-active' : ''}`}
          onClick={() => handleScenarioChange(null)}
        >
          🎲 Aleatorio
        </button>
        {scenarioGroup.map(s => (
          <button
            key={s.id}
            className={`btn-scenario ${scenarioId === s.id ? 'btn-scenario-active' : ''}`}
            onClick={() => handleScenarioChange(s.id)}
          >
            {s.icon} {s.name}
          </button>
        ))}
      </div>

      <div className="rules-banner">
        <strong>Reglas:</strong> Seleccioná un sospechoso y hacé click en la celda donde estaba según la pista.
        Cada persona ocupa una fila y columna únicas. La víctima ocupa la última casilla libre.
        El asesino es quien comparte habitación con la víctima.
      </div>

      <div className="game-area">
        <GameBoard
          puzzle={puzzle}
          userPlacements={placements}
          eliminated={eliminated}
          onCellClick={handleCellClick}
          gameStatus={gameStatus}
        />

        <div className="side-panel">
          <SuspectList
            puzzle={puzzle}
            userPlacements={placements}
            activeSuspect={activeSuspect}
            onSelectSuspect={handleSelectSuspect}
            gameStatus={gameStatus}
          />

          {gameStatus === 'playing' && (
            <div className="action-group">
              <button
                className="btn btn-check"
                onClick={handleCheck}
                disabled={!allPlaced}
                title={!allPlaced ? 'Colocá todos los sospechosos primero' : ''}
              >
                Verificar solución
              </button>
              <button className="btn btn-reveal" onClick={handleReveal}>
                Revelar respuesta
              </button>
            </div>
          )}

          {message && (
            <div className={`message ${gameStatus === 'won' ? 'message-win' : gameStatus === 'revealed' ? 'message-reveal' : 'message-error'}`}>
              {message}
              {(gameStatus === 'won' || gameStatus === 'revealed') && (
                <button className="btn btn-new" style={{ marginTop: 10 }} onClick={() => newGame()}>
                  Jugar de nuevo
                </button>
              )}
            </div>
          )}

          <div className="legend">
            <h4 style={{ margin: '0 0 6px' }}>Instrucciones</h4>
            <ul>
              <li>Seleccioná un sospechoso de la lista</li>
              <li>Clickeá la celda correcta en el mapa</li>
              <li>Al colocar un sospechoso, su fila y columna se tachan solas</li>
              <li>Sin sospechoso activo: click marca/desmarca ✕ manual</li>
              <li>Click en celda propia del sospechoso activo: lo retira</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
