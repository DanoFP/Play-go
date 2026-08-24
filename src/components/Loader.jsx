// Mientras el worker busca un caso. Los casos grandes en difícil pueden tardar
// varios segundos: se buscan muchos tableros y se descarta todo el que no sea
// resoluble por deducción pura, así que conviene decir qué está pasando.

const STEPS = [
  'Levantando el plano de la escena…',
  'Repartiendo el mobiliario…',
  'Tomando declaración a los sospechosos…',
  'Comprobando que el caso se pueda deducir sin adivinar…',
];

export default function Loader({ full = false }) {
  return (
    <div className={`loader ${full ? 'loader-full' : ''}`}>
      <svg viewBox="0 0 64 64" width="52" height="52" className="loader-glass">
        <circle cx="27" cy="27" r="17" fill="rgba(240,192,64,0.08)" stroke="#c9a542" strokeWidth="3.5" />
        <line x1="39" y1="39" x2="55" y2="55" stroke="#c9a542" strokeWidth="5" strokeLinecap="round" />
        <circle className="loader-scan" cx="27" cy="27" r="7" fill="none"
                stroke="rgba(240,192,64,0.55)" strokeWidth="2" />
      </svg>

      <div className="loader-text">
        <div className="loader-title">Preparando el caso</div>
        <ul className="loader-steps">
          {STEPS.map((s, i) => (
            <li key={i} style={{ animationDelay: `${i * 0.45}s` }}>{s}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}
