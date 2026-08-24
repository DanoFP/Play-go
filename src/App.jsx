import { useState, useCallback, useEffect, useRef, useMemo } from 'react';
import {
  checkPlacement, isCorrectSolution, canPlaceAt, getHint, findContradictions,
  SCENARIOS, DIFFICULTIES,
} from './utils/puzzleGenerator.js';
import usePuzzle from './hooks/usePuzzle.js';
import GameBoard from './components/GameBoard.jsx';
import SuspectList from './components/SuspectList.jsx';
import ObjectLegend from './components/ObjectLegend.jsx';
import Loader from './components/Loader.jsx';
import './App.css';

const STORE_KEY = 'murdoku.v1';

// Colocar a alguien descarta su fila y su columna enteras.
function computeAutoEliminated(placements, H, W) {
  const auto = new Set();
  for (const pos of Object.values(placements)) {
    if (!pos) continue;
    for (let c = 0; c < W; c++) if (c !== pos.col) auto.add(`${pos.row},${c}`);
    for (let r = 0; r < H; r++) if (r !== pos.row) auto.add(`${r},${pos.col}`);
  }
  return auto;
}

const peopleOf = p => [...p.suspects, p.victim];
const emptyPlacements = p => Object.fromEntries(peopleOf(p).map(x => [x.id, null]));

const formatTime = sec =>
  `${String(Math.floor(sec / 60)).padStart(2, '0')}:${String(sec % 60).padStart(2, '0')}`;

function loadStats() {
  try { return JSON.parse(localStorage.getItem(STORE_KEY))?.stats || { solved: 0, best: {} }; }
  catch { return { solved: 0, best: {} }; }
}
function saveStats(stats) {
  try { localStorage.setItem(STORE_KEY, JSON.stringify({ stats })); } catch { /* modo privado */ }
}

export default function App() {
  const [numSuspects, setNumSuspects] = useState(4);
  const [scenarioId, setScenarioId]   = useState(SCENARIOS[0].id);
  const [difficulty, setDifficulty]   = useState('normal');

  const { puzzle, loading, error, request } = usePuzzle({
    numSuspects: 4, scenarioId: SCENARIOS[0].id, difficulty: 'normal',
  });

  // El historial es la fuente de verdad de las colocaciones: deshacer y rehacer
  // se mueven por él, y el estado actual es siempre history[cursor].
  const [history, setHistory] = useState([{}]);
  const [cursor, setCursor]   = useState(0);
  const [manualX, setManualX] = useState(new Set());
  const [active, setActive]   = useState(null);

  const [gameStatus, setGameStatus] = useState('playing');
  const [message, setMessage]       = useState('');
  const [hint, setHint]             = useState(null);
  const [hintLevel, setHintLevel]   = useState(0);
  const [seconds, setSeconds]       = useState(0);
  const [stats, setStats]           = useState(loadStats);
  const [copied, setCopied]         = useState(false);

  const hintTimer = useRef(null);
  const placements = history[cursor];

  // Cada puzzle nuevo reinicia la partida.
  useEffect(() => {
    if (!puzzle) return;
    const empty = emptyPlacements(puzzle);
    setHistory([empty]);
    setCursor(0);
    setManualX(new Set());
    setActive(peopleOf(puzzle).sort((a, b) => a.step - b.step)[0].id);
    setGameStatus('playing');
    setMessage('');
    setHint(null);
    setHintLevel(0);
    setSeconds(0);
    setCopied(false);
  }, [puzzle]);

  useEffect(() => {
    if (gameStatus !== 'playing' || loading || !puzzle) return;
    const t = setInterval(() => setSeconds(s => s + 1), 1000);
    return () => clearInterval(t);
  }, [gameStatus, loading, puzzle]);

  useEffect(() => () => clearTimeout(hintTimer.current), []);

  const contradictions = useMemo(
    () => (puzzle && gameStatus === 'playing' ? findContradictions(puzzle, placements) : []),
    [puzzle, placements, gameStatus]
  );

  function newCase(over = {}) {
    request({
      numSuspects: over.numSuspects ?? numSuspects,
      scenarioId:  over.scenarioId  ?? scenarioId,
      difficulty:  over.difficulty  ?? difficulty,
      seed: over.seed ?? null,
    });
  }

  function commit(next) {
    setHistory(h => [...h.slice(0, cursor + 1), next]);
    setCursor(c => c + 1);
  }

  const handleCellClick = useCallback((row, col) => {
    if (gameStatus !== 'playing' || !puzzle) return;
    const k = `${row},${col}`;
    const occupant = Object.entries(placements).find(([, p]) => p && p.row === row && p.col === col);

    if (active) {
      if (occupant && occupant[0] === active) {
        commit({ ...placements, [active]: null });
        return;
      }
      if (occupant) return;
      if (!canPlaceAt(puzzle, row, col)) return;
      const next = { ...placements, [active]: { row, col } };
      if (!checkPlacement(puzzle, next).valid) return;
      setManualX(s => { const n = new Set(s); n.delete(k); return n; });
      commit(next);

      // Pasar automáticamente al siguiente de la cadena que falte.
      const pending = peopleOf(puzzle)
        .filter(p => p.id !== active && !next[p.id])
        .sort((a, b) => a.step - b.step);
      setActive(pending.length ? pending[0].id : null);
      return;
    }

    if (occupant) return;
    setManualX(s => { const n = new Set(s); n.has(k) ? n.delete(k) : n.add(k); return n; });
  }, [gameStatus, puzzle, placements, active, cursor]);

  function handleCheck() {
    if (isCorrectSolution(puzzle, placements)) {
      setGameStatus('won');
      const name = puzzle.suspects.find(s => s.id === puzzle.murderer)?.name;
      setMessage(`Caso resuelto en ${formatTime(seconds)}. El asesino es ${name}.`);
      const bkey = `${numSuspects}-${puzzle.difficulty}`;
      const next = {
        solved: stats.solved + 1,
        best: { ...stats.best, [bkey]: Math.min(stats.best[bkey] ?? Infinity, seconds) },
      };
      setStats(next); saveStats(next);
    } else {
      setMessage('Esa reconstrucción no encaja con las declaraciones. Revisá las pistas.');
    }
  }

  function handleReveal() {
    setGameStatus('revealed');
    const name = puzzle.suspects.find(s => s.id === puzzle.murderer)?.name;
    setMessage(`El asesino era ${name}, que compartía habitación con ${puzzle.victim.name}.`);
  }

  // Las pistas van por niveles: primero a quién le toca, después su habitación,
  // y solo al tercer pedido la casilla. Pedir ayuda deja de ser rendirse.
  function handleHint() {
    const level = hintLevel + 1;
    const h = getHint(puzzle, placements, level);
    if (!h) return;
    setHintLevel(level);
    setMessage(h.text);
    setHint(h.cell);
    if (h.cell) {
      clearTimeout(hintTimer.current);
      hintTimer.current = setTimeout(() => setHint(null), 5000);
    }
  }

  async function shareCase() {
    // El escenario forma parte de la identidad del caso: sin él la semilla
    // reproduciría el mismo plano pero en otro edificio.
    const url = `${location.origin}${location.pathname}`
      + `?caso=${puzzle.seed}&s=${numSuspects}&d=${puzzle.difficulty}&e=${puzzle.scenario.id}`;
    try { await navigator.clipboard.writeText(url); setCopied(true); setTimeout(() => setCopied(false), 2000); }
    catch { setMessage(`Caso #${puzzle.seed}`); }
  }

  // Un caso compartido por URL se carga al abrir.
  useEffect(() => {
    const q = new URLSearchParams(location.search);
    const seed = Number(q.get('caso'));
    if (!seed) return;
    const n = Number(q.get('s')) || 4;
    const d = DIFFICULTIES[q.get('d')] ? q.get('d') : 'normal';
    const e = SCENARIOS.some(s => s.id === q.get('e')) ? q.get('e') : null;
    setNumSuspects(n); setDifficulty(d);
    if (e) setScenarioId(e);
    request({ numSuspects: n, scenarioId: e, difficulty: d, seed });
  }, []);

  if (error) {
    return (
      <div className="app">
        <div className="message message-reveal" style={{ marginTop: 40 }}>
          No se pudo generar el caso: {error}
          <button className="btn btn-new" onClick={() => newCase()}>Reintentar</button>
        </div>
      </div>
    );
  }

  if (!puzzle) return <Loader full />;

  const ids = peopleOf(puzzle).map(p => p.id);
  const placedCount = ids.filter(id => placements[id]).length;
  const allPlaced = placedCount === ids.length;
  const carved = puzzle.live.size < puzzle.H * puzzle.W;
  const bestKey = `${numSuspects}-${puzzle.difficulty}`;
  const best = stats.best[bestKey];

  return (
    <div className="app">
      <header className="header">
        <div className="case-label">
          Caso #{puzzle.seed} · planta {puzzle.H}×{puzzle.W}{carved ? ' irregular' : ''}
        </div>
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
              <button key={s.id} className={`chip ${scenarioId === s.id ? 'chip-on' : ''}`}
                      disabled={loading}
                      onClick={() => { setScenarioId(s.id); newCase({ scenarioId: s.id }); }}
                      title={s.name}>
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
                <button key={n} className={`chip chip-sm ${numSuspects === n ? 'chip-on' : ''}`}
                        disabled={loading}
                        onClick={() => { setNumSuspects(n); newCase({ numSuspects: n }); }}>
                  {n}
                </button>
              ))}
            </div>
          </div>

          <div className="control-group">
            <span className="control-label">Dificultad</span>
            <div className="chip-row">
              {Object.entries(DIFFICULTIES).map(([k, d]) => (
                <button key={k} className={`chip chip-sm ${difficulty === k ? 'chip-on' : ''}`}
                        disabled={loading}
                        onClick={() => { setDifficulty(k); newCase({ difficulty: k }); }}>
                  {d.label}
                </button>
              ))}
            </div>
          </div>

          <button className="btn btn-new" onClick={() => newCase()} disabled={loading}>
            Caso nuevo
          </button>
          <button className="btn btn-ghost" onClick={shareCase} disabled={loading}>
            {copied ? '¡Copiado!' : 'Compartir caso'}
          </button>
        </div>
      </div>

      <div className="statusbar">
        <span className="stat"><span className="stat-k">Tiempo</span> {formatTime(seconds)}</span>
        <span className="stat"><span className="stat-k">Colocados</span> {placedCount}/{ids.length}</span>
        <span className="stat"><span className="stat-k">Pasos</span> {puzzle.depth}</span>
        {best !== undefined && (
          <span className="stat"><span className="stat-k">Mejor</span> {formatTime(best)}</span>
        )}
        <span className="stat"><span className="stat-k">Resueltos</span> {stats.solved}</span>
        <span className="stat stat-diff">{DIFFICULTIES[puzzle.difficulty].label}</span>
      </div>

      <div className="game-area">
        <div className="board-column">
          {loading && <Loader />}
          {!loading && (
            <GameBoard
              puzzle={puzzle}
              userPlacements={placements}
              eliminated={new Set([...computeAutoEliminated(placements, puzzle.H, puzzle.W), ...manualX])}
              onCellClick={handleCellClick}
              gameStatus={gameStatus}
              hint={hint}
            />
          )}

          <div className="undo-row">
            <button className="btn btn-ghost" onClick={() => setCursor(c => Math.max(0, c - 1))}
                    disabled={cursor === 0 || gameStatus !== 'playing'}>
              ↶ Deshacer
            </button>
            <button className="btn btn-ghost" onClick={() => setCursor(c => Math.min(history.length - 1, c + 1))}
                    disabled={cursor >= history.length - 1 || gameStatus !== 'playing'}>
              Rehacer ↷
            </button>
          </div>

          {contradictions.length > 0 && (
            <div className="contradictions">
              <div className="contra-title">✕ No encaja con {contradictions.length === 1 ? 'una declaración' : `${contradictions.length} declaraciones`}</div>
              {contradictions.slice(0, 4).map((c, i) => (
                <div key={i} className="contra-item">
                  <strong>{c.name}:</strong> «{c.text}»
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="side-panel">
          <SuspectList
            puzzle={puzzle}
            userPlacements={placements}
            activeSuspect={active}
            onSelectSuspect={id => setActive(a => (a === id ? null : id))}
            gameStatus={gameStatus}
            contradictions={contradictions}
          />

          {gameStatus === 'playing' && (
            <div className="action-group">
              <button className="btn btn-check" onClick={handleCheck} disabled={!allPlaced}
                      title={!allPlaced ? 'Colocá a todos primero' : ''}>
                Resolver caso
              </button>
              <button className="btn btn-hint" onClick={handleHint}>
                Pista {hintLevel > 0 && `(${hintLevel}/3)`}
              </button>
              <button className="btn btn-reveal" onClick={handleReveal}>Rendirse</button>
            </div>
          )}

          {message && (
            <div className={`message message-${gameStatus === 'won' ? 'win' : gameStatus === 'revealed' ? 'reveal' : 'note'}`}>
              <span>{message}</span>
              {(gameStatus === 'won' || gameStatus === 'revealed') && (
                <button className="btn btn-new" onClick={() => newCase()}>Caso nuevo</button>
              )}
            </div>
          )}

          <ObjectLegend objects={puzzle.objects} />

          <div className="legend">
            <div className="legend-title">Cómo se juega</div>
            <ul>
              <li>Las fichas están <strong>en orden de deducción</strong>: la 1 sale con su
                  propia declaración, y cada siguiente se abre con las anteriores.</li>
              <li>Cada persona ocupa una <strong>fila y una columna únicas</strong>.</li>
              <li>El mobiliario con trama <strong>bloquea</strong> la casilla.</li>
              <li>Al colocar a alguien, su fila y columna se tachan solas.</li>
              <li>Sin nadie seleccionado, el click marca o desmarca un descarte ✕.</li>
              <li>El asesino es quien comparte habitación con la víctima.</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
