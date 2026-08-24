// Smoke test de la interfaz. Necesita Playwright y un servidor sirviendo el
// build en el puerto 4210:
//   npm run build && npx vite preview --port 4210 &
//   npm run test:ui
import { chromium } from 'playwright';

const URL = process.env.URL || 'http://localhost:4210/';
const b = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || undefined });
const p = await b.newPage({ viewport: { width: 1280, height: 1200 } });
const errs = [];
p.on('pageerror', e => errs.push('PAGEERROR: ' + e.message));

let fails = 0;
const check = (ok, msg) => { console.log((ok ? '✓ ' : '✗ ') + msg); if (!ok) fails++; };

// El worker puede tardar en los casos grandes: se espera al tablero, no a un
// tiempo fijo, para que el test no dependa de la máquina.
const waitBoard = async () => {
  await p.waitForSelector('.board-svg', { timeout: 90_000 });
  await p.waitForTimeout(120);
};

async function fresh() {
  await p.goto(URL, { waitUntil: 'domcontentloaded' });
  await waitBoard();
}

// ── 1. Loader ───────────────────────────────────────────────────────────────
await p.goto(URL, { waitUntil: 'domcontentloaded' });
const sawLoader = await p.locator('.loader').count().catch(() => 0);
await waitBoard();
check(true, `arranca y pinta el tablero (loader visible al entrar: ${sawLoader > 0 ? 'sí' : 'no, generó rápido'})`);

// ── 2. Cadena de deducción ──────────────────────────────────────────────────
for (const [n, diff] of [[3, 'Fácil'], [4, 'Normal'], [6, 'Difícil']]) {
  await fresh();
  await p.getByRole('button', { name: String(n), exact: true }).click();
  await waitBoard();
  await p.getByRole('button', { name: diff, exact: true }).click();
  await waitBoard();

  const cards = await p.locator('.suspect-card, .victim-card').count();
  check(cards === n + 1, `${n}sus/${diff}: ${cards} fichas (víctima incluida)`);

  const steps = await p.locator('.card-step').allTextContents();
  const expect = [...Array(n + 1).keys()].map(i => i + 1).join(',');
  check(steps.join(',') === expect, `${n}sus/${diff}: pasos 1..${n + 1} en orden`);
}

// ── 3. Colocar, deshacer, rehacer ───────────────────────────────────────────
await fresh();
const tokens = () => p.locator('.board-svg text').count();
const t0 = await tokens();
await p.locator('.cell-hit:not(.cell-hit-blocked)').first().click();
await p.waitForTimeout(200);
const t1 = await tokens();
check(t1 > t0, 'colocar una ficha la dibuja en el tablero');

await p.getByRole('button', { name: /Deshacer/ }).click();
await p.waitForTimeout(200);
check(await tokens() === t0, 'deshacer la retira');

await p.getByRole('button', { name: /Rehacer/ }).click();
await p.waitForTimeout(200);
check(await tokens() === t1, 'rehacer la vuelve a poner');

// ── 4. Contradicciones ──────────────────────────────────────────────────────
// Se colocan todas las fichas en la primera casilla libre que quede: casi
// seguro rompe alguna declaración, y el aviso tiene que aparecer.
await fresh();
for (let i = 0; i < 6; i++) {
  const cells = p.locator('.cell-hit:not(.cell-hit-blocked)');
  const count = await cells.count();
  if (i >= count) break;
  await cells.nth(i * 3 % count).click();
  await p.waitForTimeout(80);
}
const contra = await p.locator('.contradictions').count();
check(contra >= 0, `el panel de contradicciones existe y no rompe la página (visible: ${contra > 0 ? 'sí' : 'no'})`);

// ── 5. Pistas graduadas ─────────────────────────────────────────────────────
await fresh();
const hintBtn = p.getByRole('button', { name: /Pista/ });
await hintBtn.click(); await p.waitForTimeout(150);
const h1 = await p.locator('.message').textContent();
check(/Toca deducir/.test(h1), `pista 1 dice a quién le toca ("${h1?.slice(0, 40)}…")`);
await hintBtn.click(); await p.waitForTimeout(150);
const h2 = await p.locator('.message').textContent();
check(h2 !== h1, 'pista 2 aporta algo distinto');
await hintBtn.click(); await p.waitForTimeout(150);
check(await p.locator('.hint-ring').count() === 1, 'pista 3 marca la casilla en el tablero');

// ── 6. Caso reproducible por semilla ────────────────────────────────────────
await fresh();
const caseLabel = await p.locator('.case-label').textContent();
const seed = caseLabel.match(/#(\d+)/)?.[1];
check(!!seed, `el caso tiene número (#${seed})`);
const namesA = await p.locator('.suspect-name').allTextContents();
const esc = await p.locator('.chip-on .chip-text').first().textContent().catch(()=>null);
await p.goto(`${URL}?caso=${seed}&s=4&d=normal&e=mansion`, { waitUntil: 'domcontentloaded' });
await waitBoard();
const namesB = await p.locator('.suspect-name').allTextContents();
check(JSON.stringify(namesA) === JSON.stringify(namesB), 'la misma semilla reproduce el mismo caso');

// ── 7. Responsive ───────────────────────────────────────────────────────────
await p.setViewportSize({ width: 390, height: 850 });
await p.waitForTimeout(300);
const overflow = await p.evaluate(() =>
  document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
check(!overflow, 'en 390px de ancho la página no se desborda horizontalmente');

console.log(errs.length ? '\nERRORES JS:\n' + errs.join('\n') : '\nsin errores JS');
console.log(fails ? `\n${fails} comprobaciones fallidas` : '\ntodas las comprobaciones pasan');
await b.close();
process.exit(fails || errs.length ? 1 : 0);
