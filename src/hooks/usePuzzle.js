import { useCallback, useEffect, useRef, useState } from 'react';
import { generatePuzzle } from '../utils/puzzleGenerator.js';

// Pide puzzles al worker y expone {puzzle, loading, error, request}.
// Si el worker no está disponible (entorno sin soporte, o falla al crearse),
// cae a generación síncrona: es peor experiencia pero el juego sigue andando.

export default function usePuzzle(initial) {
  const workerRef = useRef(null);
  const reqId = useRef(0);
  const [puzzle, setPuzzle] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    try {
      workerRef.current = new Worker(
        new URL('../utils/puzzleWorker.js', import.meta.url),
        { type: 'module' }
      );
    } catch {
      workerRef.current = null;
    }
    return () => workerRef.current?.terminate();
  }, []);

  const request = useCallback(({ numSuspects, scenarioId, difficulty, seed = null }) => {
    const id = ++reqId.current;
    setLoading(true);
    setError(null);

    const worker = workerRef.current;
    if (!worker) {
      // Fallback síncrono. El setTimeout deja pintar el loader antes de bloquear.
      setTimeout(() => {
        try {
          const p = generatePuzzle(numSuspects, scenarioId, difficulty, seed);
          if (id === reqId.current) { setPuzzle(p); setLoading(false); }
        } catch (err) {
          if (id === reqId.current) { setError(String(err)); setLoading(false); }
        }
      }, 30);
      return;
    }

    const onMessage = e => {
      if (e.data.id !== reqId.current) return;   // respuesta de una petición vieja
      worker.removeEventListener('message', onMessage);
      if (e.data.error) { setError(e.data.error); setLoading(false); return; }
      setPuzzle(e.data.puzzle);
      setLoading(false);
    };
    worker.addEventListener('message', onMessage);
    worker.postMessage({ id, numSuspects, scenarioId, difficulty, seed });
  }, []);

  useEffect(() => { request(initial); }, []);   // primer caso

  return { puzzle, loading, error, request };
}
