// Multi-device hot reload: edits made in this panel are mirrored to sibling apps
// (same build on other simulators/devices), addressed by XAML source identity —
// stable across platforms and idioms, unlike per-process element ids.
let mirrorTargets = [];
try { mirrorTargets = JSON.parse(localStorage.getItem('hvMirrors') || '[]'); } catch { }

function saveMirrors() {
  localStorage.setItem('hvMirrors', JSON.stringify(mirrorTargets));
}

async function scanMirrors() {
  const found = [];
  for (let port = 9295; port <= 9309; port++) {
    const base = 'http://' + location.hostname + ':' + port;
    if (base === location.origin) continue;
    try {
      const ctl = new AbortController();
      setTimeout(() => ctl.abort(), 700);
      const d = await (await fetch(base + '/api/ping', { signal: ctl.signal })).json();
      found.push({ url: base, label: d.app + ' · ' + d.device });
    } catch { /* nothing on this port */ }
  }
  for (const f of found) {
    const existing = mirrorTargets.find(t => t.url === f.url);
    if (existing) existing.label = f.label;
    else mirrorTargets.push({ ...f, on: true });
  }
  saveMirrors();
  renderMirrors();
}

function addMirrorManually() {
  const value = document.getElementById('mirroraddr').value.trim();
  if (!value) return;
  const url = value.startsWith('http') ? value : 'http://' + value;
  if (!mirrorTargets.find(t => t.url === url))
    mirrorTargets.push({ url: url, label: url, on: true });
  document.getElementById('mirroraddr').value = '';
  saveMirrors();
  renderMirrors();
}

function renderMirrors() {
  const list = document.getElementById('mirrorslist');
  list.innerHTML = mirrorTargets.length ? '' : ' (no targets — Scan or add host:port)';
  for (const t of mirrorTargets) {
    const item = document.createElement('label');
    item.className = 'mirroritem';
    const cb = Object.assign(document.createElement('input'), { type: 'checkbox', checked: t.on });
    cb.onchange = () => { t.on = cb.checked; saveMirrors(); };
    const del = Object.assign(document.createElement('button'), { textContent: '✕', className: 'clearbtn' });
    del.onclick = (e) => {
      e.preventDefault();
      mirrorTargets = mirrorTargets.filter(x => x !== t);
      saveMirrors();
      renderMirrors();
    };
    item.append(cb, ' ' + t.label + ' ', del);
    list.appendChild(item);
  }
}

// Fan-out core: posts the payload to every enabled target and flashes per-target results.
async function mirrorFanOut(path, payload) {
  const active = mirrorTargets.filter(t => t.on).slice();
  // "All instances" re-applies the edit locally through the same source-identity matcher,
  // so every element created from that XAML line (all DataTemplate rows) is updated.
  if (allInstances && path !== '/api/mock/rules/scenario')
    active.push({ url: location.origin, label: 'this device', on: true });
  if (!active.length) return;
  const results = [];
  await Promise.all(active.map(async t => {
    try {
      const r = await (await fetch(t.url + path, { method: 'POST', body: JSON.stringify(payload) })).json();
      results.push(shortLabel(t) + (r.applied === undefined ? ' ✓' : r.applied > 0 ? ' ✓' + r.applied : ' —'));
    } catch {
      results.push(shortLabel(t) + ' ✗offline');
    }
  }));
  const hint = document.getElementById('hint');
  hint.textContent = '🖧 ' + results.join('  ·  ');
  setTimeout(() => { if (hint.textContent.startsWith('🖧')) updateHint(); }, 4000);
}

// Called by apply() after a successful local edit; targets match by XAML source identity.
function mirrorApply(section, name, value, clear) {
  if (!currentSource) return;
  mirrorFanOut('/api/broadcast/property',
    { source: currentSource, ...currentIdentity, section: section, name: name, value: value, clear: !!clear });
}

// Structural actions: add/remove span, shadow, grid definitions, create FormattedText…
function mirrorAction(section, name) {
  if (!currentSource) return;
  mirrorFanOut('/api/broadcast/action', { source: currentSource, ...currentIdentity, section: section, name: name });
}

// Mock rules are device state, not element state — mirroring replaces each target's set
// with this device's set (ids are per-device, so it's a wipe-and-recreate sync).
async function mirrorRulesSync() {
  const active = mirrorTargets.filter(t => t.on);
  if (!active.length) return;
  const mine = await (await fetch('/api/mock/rules')).json();
  await Promise.all(active.map(async t => {
    try {
      const theirs = await (await fetch(t.url + '/api/mock/rules')).json();
      for (const r of theirs.rules)
        await fetch(t.url + '/api/mock/rules/delete', { method: 'POST', body: JSON.stringify({ id: r.id }) });
      for (const name of (theirs.scenarios || []).filter(n => !(mine.scenarios || []).includes(n)))
        await fetch(t.url + '/api/mock/rules/scenario/remove', { method: 'POST', body: JSON.stringify({ name: name }) });
      for (const name of (mine.scenarios || []))
        await fetch(t.url + '/api/mock/rules/scenario/add', { method: 'POST', body: JSON.stringify({ name: name }) });
      for (const r of mine.rules)
        await fetch(t.url + '/api/mock/rules/save', { method: 'POST', body: JSON.stringify({ ...r, id: 0 }) });
      await fetch(t.url + '/api/mock/rules/scenario', { method: 'POST', body: JSON.stringify({ name: mine.activeScenario || '' }) });
    } catch { /* target offline — it will pick rules up on the next sync */ }
  }));
}

function shortLabel(t) {
  return (t.label.split('·')[1] || t.label).trim().split(' ').slice(0, 2).join(' ');
}
