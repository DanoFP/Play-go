import { useState, useCallback, useEffect, useRef } from 'react';
import {
  generatePuzzle, checkPlacement, isCorrectSolution, canPlaceAt, getHint,
  SCENARIOS, DIFFICULTIES,
} from './utils/puzzleGenerator.js';
import GameBoard from './components/GameBoard.jsx';
import SuspectList from './components/SuspectList.jsx';
import ObjectLegend from './components/ObjectLegend.jsx';
import './App.css';

// Al colocar un sospechoso se descarta su fila y su columna enteras.
function computeAutoEliminated(placements, N) {
  const auto = new Set();
  for (const pos of Object.values(placements)) {
    if (!pos) continue;
    for (let c = 0; c < N; c++) if (c !== pos.col) auto.add(`${pos.row},${c}`);
    for (let r = 0; r < N; r++) if (r !== pos.row) auto.add(`${r},${pos.col}`);
  }
  return auto;
}

function freshState(numSuspects, scenarioId, difficulty) {
  const puzzle = generatePuzzle(numSuspects, scenarioId, difficulty);
  return {
    puzzle,
    placements: Object.fromEntries(puzzle.suspects.map(s => [s.id, null])),
    manualEliminated: new Set(),
    activeSuspect: puzzle.suspects[0].id,
  };
}

function formatTime(sec) {
  const m = String(Math.floor(sec / 60)).padStart(2, '0');
  const s = String(sec % 60).padStart(2, '0');
  return `${m}:${s}`;
}

export default function App() {
  const [numSuspects, setNumSuspects] = useState(4);
  const [scenarioId, setScenarioId]   = useState(SCENARIOS[0].id);
  const [difficulty, setDifficulty]   = useState('normal');
  const [state, setState]             = useState(() => freshState(4, SCENARIOS[0].id, 'normal'));
  const [gameStatus, setGameStatus]   = useState('playing');
  const [message, setMessage]         = useState('');
  const [hint, setHint]               = useState(null);
  const [hintsUsed, setHintsUsed]     = useState(0);
  const [seconds, setSeconds]         = useState(0);

  const { puzzle, placements, manualEliminated, activeSuspect } = state;
  const autoEliminated = computeAutoEliminated(placements, puzzle.N);
  const eliminated = new Set([...autoEliminated, ...manualEliminated]);

  const hintTimer = useRef(null);

  useEffect(() => {
    if (gameStatus !== 'playing') return;
    const t = setInterval(() => setSeconds(s => s + 1), 1000);
    return () => clearInterval(t);
  }, [gameStatus]);

  useEffect(() => () => clearTimeout(hintTimer.current), []);

  function reset(n = numSuspects, sid = scenarioId, diff = difficulty) {
    setState(freshState(n, sid, diff));
    setGameStatus('playing');
    setMessage('');
    setHint(null);
    setHintsUsed(0);
    setSeconds(0);
  }

  const handleCellClick = useCallback((row, col) => {
    if (gameStatus !== 'playing') return;

    setState(prev => {
      const k = `${row},${col}`;
      const manual = new Set(prev.manualEliminated);
      const next = { ...prev.placements };

      const occupant = Object.entries(next).find(([, p]) => p && p.row === row && p.col === col);

      if (prev.activeSuspect) {
        // Click sobre la propia ficha → retirarla.
        if (occupant && occupant[0] === prev.activeSuspect) {
          next[prev.activeSuspect] = null;
          return { ...prev, placements: next };
        }
        if (occupant) return prev;                       // ocupada por otro
        if (!canPlaceAt(prev.puzzle, row, col)) return prev;  // mobiliario

        next[prev.activeSuspect] = { row, col };
        if (!checkPlacement(prev.puzzle, next).valid) return prev;
        manual.delete(k);
        return { ...prev, placements: next, manualEliminated: manual };
      }

      // Sin sospechoso activo: alternar marca manual de descarte.
      if (occupant) return prev;
      manual.has(k) ? manual.delete(k) : manual.add(k);
      return { ...prev, manualEliminated: manual };
    });
  }, [gameStatus]);

  function handleSelectSuspect(id) {
    setState(prev => ({ ...prev, activeSuspect: prev.activeSuspect === id ? null : id }));
  }

  function handleCheck() {
    if (isCorrectSolution(puzzle, placements)) {
      setGameStatus('won');
      const name = puzzle.suspects.find(s => s.id === puzzle.murderer)?.name;
      setMessage(`Caso resuelto en ${formatTime(seconds)}. El asesino es ${name}.`);
    } else {
      setMessage('Esa distribución no encaja con las declaraciones. Revisá las pistas.');
    }
  }

  function handleReveal() {
    setGameStatus('revealed');
    const name = puzzle.suspects.find(s => s.id === puzzle.murderer)?.name;
    setMessage(`El asesino era ${name}, que compartía habitación con ${puzzle.victim.name}.`);
  }

  function handleHint() {
    const s = getHint(puzzle, placements);
    if (!s) return;
    setHint({ row: s.row, col: s.col });
    setHintsUsed(n => n + 1);
    setMessage(`Pista: ${s.name} estaba en la casilla marcada.`);
    clearTimeout(hintTimer.current);
    hintTimer.current = setTimeout(() => setHint(null), 4000);
  }

  const allPlaced = puzzle.suspects.every(s => placements[s.id]);
  const placedCount = puzzle.suspects.filter(s => placements[s.id]).length;

  return (
    <div className="app">
      <header className="header">
        <div className="case-label">Expediente Murdoku · caso {puzzle.N}×{puzzle.N}</div>
        <h1 className="title">
          <span className="title-icon">{puzzle.scenario.icon}</span> {puzzle.scenario.name}
        </h1>
        <p className="subtitle">
          Reconstruí la escena y descubrí quién compartía habitación con la víctima
        </p>
      </header>

      <div className="controls">
        <div className="control-group">
          <span className="control-label">Escenario</span>
          <div className="chip-row">
            {SCENARIOS.map(s => (
              <button
                key={s.id}
                className={`chip ${scenarioId === s.id ? 'chip-on' : ''}`}
                onClick={() => { setScenarioId(s.id); reset(numSuspects, s.id, difficulty); }}
                title={s.name}
              >
                <span className="chip-icon">{s.icon}</span>
                <span className="chip-text">{s.name}</span>
              </button>
            ))}
          </div>
        </div>

        <div className="control-row">
          <div className="control-group">
            <span className="control-label">Sospechosos</span>
            <div className="chip-row">
              {[3, 4, 5, 6].map(n => (
                <button
                  key={n}
                  className={`chip chip-sm ${numSuspects === n ? 'chip-on' : ''}`}
                  onClick={() => { setNumSuspects(n); reset(n, scenarioId, difficulty); }}
                >
                  {n}
                </button>
              ))}
            </div>
          </div>

          <div className="control-group">
            <span className="control-label">Dificultad</span>
            <div className="chip-row">
              {Object.entries(DIFFICULTIES).map(([k, d]) => (
                <button
                  key={k}
                  className={`chip chip-sm ${difficulty === k ? 'chip-on' : ''}`}
                  onClick={() => { setDifficulty(k); reset(numSuspects, scenarioId, k); }}
                >
                  {d.label}
                </button>
              ))}
            </div>
          </div>

          <button className="btn btn-new" onClick={() => reset()}>Caso nuevo</button>
        </div>
      </div>

      <div className="statusbar">
        <span className="stat"><span className="stat-k">Tiempo</span> {formatTime(seconds)}</span>
        <span className="stat"><span className="stat-k">Colocados</span> {placedCount}/{puzzle.suspects.length}</span>
        <span className="stat"><span className="stat-k">Pistas</span> {hintsUsed}</span>
        <span className="stat stat-diff">{DIFFICULTIES[puzzle.difficulty].label}</span>
      </div>

      <div className="game-area">
        <GameBoard
          puzzle={puzzle}
          userPlacements={placements}
          eliminated={eliminated}
          onCellClick={handleCellClick}
          gameStatus={gameStatus}
          hint={hint}
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
                title={!allPlaced ? 'Colocá a todos los sospechosos primero' : ''}
              >
                Resolver caso
              </button>
              <button className="btn btn-hint" onClick={handleHint}>Pista</button>
              <button className="btn btn-reveal" onClick={handleReveal}>Rendirse</button>
            </div>
          )}

          {message && (
            <div className={`message message-${gameStatus === 'won' ? 'win' : gameStatus === 'revealed' ? 'reveal' : 'note'}`}>
              <span>{message}</span>
              {(gameStatus === 'won' || gameStatus === 'revealed') && (
                <button className="btn btn-new" onClick={() => reset()}>Caso nuevo</button>
              )}
            </div>
          )}

          <ObjectLegend objects={puzzle.objects} />

          <div className="legend">
            <div className="legend-title">Cómo se juega</div>
            <ul>
              <li>Cada persona ocupa una <strong>fila y una columna únicas</strong>.</li>
              <li>El mobiliario oscurecido <strong>bloquea</strong> la casilla: nadie puede estar ahí.</li>
              <li>Al colocar a alguien, su fila y columna se tachan solas.</li>
              <li>Sin sospechoso activo, el click marca o desmarca un descarte ✕.</li>
              <li>La víctima queda en la última casilla libre; el asesino es quien comparte habitación con ella.</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
