import { useState, useCallback } from 'react';
import { generatePuzzle, checkPlacement, isCorrectSolution } from './utils/puzzleGenerator.js';
import GameBoard from './components/GameBoard.jsx';
import SuspectList from './components/SuspectList.jsx';
import './App.css';

function freshState(numSuspects) {
  const puzzle = generatePuzzle(numSuspects);
  const placements = Object.fromEntries(puzzle.suspects.map(s => [s.id, null]));
  return { puzzle, placements, eliminated: new Set(), activeSuspect: puzzle.suspects[0].id };
}

export default function App() {
  const [numSuspects, setNumSuspects] = useState(3);
  const [state, setState] = useState(() => freshState(3));
  const [gameStatus, setGameStatus] = useState('playing'); // playing | won | lost | revealed
  const [message, setMessage] = useState('');

  const { puzzle, placements, eliminated, activeSuspect } = state;

  function newGame(n = numSuspects) {
    setState(freshState(n));
    setGameStatus('playing');
    setMessage('');
  }

  function handleDifficultyChange(n) {
    setNumSuspects(n);
    setState(freshState(n));
    setGameStatus('playing');
    setMessage('');
  }

  const handleCellClick = useCallback((row, col) => {
    if (gameStatus !== 'playing') return;

    setState(prev => {
      const key = `${row},${col}`;
      const newElim = new Set(prev.eliminated);
      const newPlacements = { ...prev.placements };

      // Is there already a suspect placed here (by user)?
      const occupyingSuspect = Object.entries(newPlacements).find(
        ([, p]) => p && p.row === row && p.col === col
      );

      if (prev.activeSuspect) {
        // If active suspect already placed here → remove them (toggle off)
        if (occupyingSuspect && occupyingSuspect[0] === prev.activeSuspect) {
          newPlacements[prev.activeSuspect] = null;
          return { ...prev, placements: newPlacements, eliminated: newElim };
        }
        // If another suspect is here → don't overwrite
        if (occupyingSuspect) return prev;

        // Place active suspect here (remove from old position first)
        newPlacements[prev.activeSuspect] = { row, col };
        // Remove X if present
        newElim.delete(key);

        const check = checkPlacement(puzzle, newPlacements);
        if (!check.valid) {
          return prev; // reject invalid placement silently
        }
        return { ...prev, placements: newPlacements, eliminated: newElim };
      } else {
        // No active suspect: toggle X mark (only on empty cells)
        if (occupyingSuspect) return prev;
        if (newElim.has(key)) {
          newElim.delete(key);
        } else {
          newElim.add(key);
        }
        return { ...prev, eliminated: newElim };
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

  return (
    <div className="app">
      <header className="header">
        <h1 className="title">🔍 Detective</h1>
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
              <li>Sin sospechoso activo: click marca ✕ (elimina)</li>
              <li>Click en celda ocupada por el sospechoso activo: lo retira</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
