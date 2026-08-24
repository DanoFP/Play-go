// Smoke test de la interfaz. Necesita Playwright y un servidor sirviendo el
// build en el puerto 4200:
//   npm run build && npx vite preview --port 4200 &
//   npx playwright@latest install chromium   (si hace falta)
//   npm run test:ui
import { chromium } from 'playwright';
const b = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || undefined });
const p = await b.newPage({ viewport:{width:1200,height:1200} });
const errs=[]; p.on('pageerror', e=>errs.push('PAGEERROR: '+e.message));
let fails = 0;
const check = (ok,msg)=>{ if(!ok){console.log('✗ '+msg); fails++;} else console.log('✓ '+msg); };

for (const [n,diff] of [[3,'facil'],[4,'normal'],[6,'dificil']]) {
  await p.goto('http://localhost:4200/', {waitUntil:'networkidle'});
  await p.getByRole('button',{name:String(n),exact:true}).click();
  await p.waitForTimeout(1500);
  const label = {facil:'Fácil',normal:'Normal',dificil:'Difícil'}[diff];
  await p.getByRole('button',{name:label,exact:true}).click();
  await p.waitForTimeout(2500);

  const cards = await p.locator('.suspect-card, .victim-card').count();
  check(cards === n+1, `${n}sus/${diff}: ${cards} fichas (esperado ${n+1}, víctima incluida)`);

  const steps = await p.locator('.card-step').allTextContents();
  check(steps.join(',') === [...Array(n+1).keys()].map(i=>i+1).join(','), `${n}sus/${diff}: fichas numeradas 1..${n+1} en orden (${steps.join(',')})`);

  // la ficha 1 viene preseleccionada: click en celda debe colocarla
  const before = await p.locator('.board-svg text').count();
  await p.locator('.cell-hit:not(.cell-hit-blocked)').first().click();
  await p.waitForTimeout(250);
  const after = await p.locator('.board-svg text').count();
  check(after > before, `${n}sus/${diff}: colocar ficha dibuja token`);
  check(await p.locator('.suspect-card.suspect-placed, .victim-card.suspect-placed').count() >= 1,
        `${n}sus/${diff}: la ficha colocada se marca en el panel`);

  // deseleccionar y comprobar que el click pasa a marcar descartes
  await p.locator('.suspect-card, .victim-card').first().click();
  await p.waitForTimeout(150);
  const t0 = await p.locator('.board-svg text').count();
  await p.locator('.cell-hit:not(.cell-hit-blocked)').nth(3).click();
  await p.waitForTimeout(200);
  check(await p.locator('.board-svg text').count() === t0,
        `${n}sus/${diff}: sin selección el click no coloca a nadie`);

  // rendirse revela solución completa incluida la víctima
  await p.getByRole('button',{name:'Rendirse'}).click();
  await p.waitForTimeout(400);
  const badge = await p.locator('.murderer-badge').count();
  check(badge === 1, `${n}sus/${diff}: se marca exactamente 1 asesino`);
  const solved = await p.locator('.placed-label').count();
  check(solved === n+1, `${n}sus/${diff}: se revelan las ${n+1} posiciones`);
}
console.log(errs.length ? '\nERRORES JS:\n'+errs.join('\n') : '\nsin errores JS');
console.log(fails ? `\n${fails} comprobaciones fallidas` : '\ntodas las comprobaciones pasan');
await b.close();
process.exit(fails||errs.length ? 1 : 0);
