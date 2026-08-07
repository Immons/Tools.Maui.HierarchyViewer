// Structure editing: right-click a tree row → add a child from the catalog / remove the element.

let structCatalog = null;
let structMenu = null;
let structBack = null;

document.getElementById('tree').addEventListener('contextmenu', (e) => {
  const row = e.target.closest('.row');
  if (!row || !row.dataset.id) return;
  e.preventDefault();
  openStructureMenu(parseInt(row.dataset.id, 10), e.clientX, e.clientY);
});
document.addEventListener('click', () => closeStructureMenu());
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') { closeStructureMenu(); closeCatalog(); }
  if (e.key === 'Delete' || e.key === 'Backspace') {
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
    if (selectedId == null || document.getElementById('view-inspector').hidden) return;
    e.preventDefault();
    removeElement(selectedId);
  }
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'c') {
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
    if (window.getSelection()?.toString()) return; // real text copy wins
    if (selectedId == null || document.getElementById('view-inspector').hidden) return;
    copyElement(selectedId, e.shiftKey); // Shift = force (custom controls with their content)
  }
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'v') {
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
    if (copiedElementId == null || selectedId == null || document.getElementById('view-inspector').hidden) return;
    e.preventDefault();
    pasteElement(selectedId, copiedElementId, copiedForce);
  }
  if ((e.ctrlKey || e.metaKey) && !e.shiftKey && e.key.toLowerCase() === 'z') {
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
    if (document.getElementById('view-inspector').hidden) return;
    e.preventDefault();
    undoLastEdit();
  }
  if ((e.ctrlKey || e.metaKey) && ((e.shiftKey && e.key.toLowerCase() === 'z') || e.key.toLowerCase() === 'y')) {
    const t = document.activeElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable)) return;
    if (document.getElementById('view-inspector').hidden) return;
    e.preventDefault();
    redoLastEdit();
  }
});

let copiedElementId = null;
let copiedForce = false;

function copyElement(id, force) {
  copiedElementId = id;
  copiedForce = force;
  flashHint(force ? 'Copied with content — Ctrl+V pastes into the selection'
                  : 'Copied — Ctrl+V pastes into the selection');
}

function flashHint(text) {
  const hint = document.getElementById('hint');
  hint.textContent = text;
  clearTimeout(flashHint._t);
  flashHint._t = setTimeout(() => { hint.textContent = ''; }, 3000);
}

async function pasteElement(targetId, sourceId, force) {
  const r = await fetch('/api/element/' + targetId + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'paste', source: sourceId, force: !!force }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Paste failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
  onRowClick(data.id);
}

// Ctrl/Cmd+Shift+Z (or Ctrl+Y) — redo the most recently undone entry.
async function redoLastEdit() {
  const res = await (await fetch('/api/history/redo', { method: 'POST', body: '{}' })).json();
  if (!res.ok) return;
  await refreshAll(true);
  refreshHistoryIfOpen();
  if (selectedId != null) await loadProps(selectedId, true);
}

// Ctrl/Cmd+Z — undo the newest undoable entry from the edit history.
async function undoLastEdit() {
  const data = await (await fetch('/api/history')).json();
  const entry = data.entries.find(en => en.canUndo && !en.undone);
  if (!entry) return;
  const res = await (await fetch('/api/history/undo', {
    method: 'POST', body: JSON.stringify({ seq: entry.seq }),
  })).json();
  if (!res.ok) return;
  await refreshAll(true);
  refreshHistoryIfOpen();
  if (selectedId != null) await loadProps(selectedId, true);
}

document.getElementById('mirrorMaxBtn').addEventListener('click', () => {
  const panel = document.getElementById('mirrorpanel');
  const view = document.getElementById('view-inspector');
  const on = !panel.classList.contains('maximized');
  // Maximized = three columns: tree 25% | mirror | properties (mirror and props split the rest).
  if (on) view.insertBefore(panel, document.getElementById('right'));
  else document.getElementById('left').appendChild(panel);
  panel.classList.toggle('maximized', on);
  view.classList.toggle('mirrormax', on);
  document.getElementById('mirrorMaxBtn').textContent = on ? '🗗' : '⛶';
  document.getElementById('mirrorMaxBtn').title = on ? 'Dock the mirror back' : 'Expand the mirror to a full column';
});

function openStructureMenu(id, x, y) {
  closeStructureMenu();
  structMenu = el('div', 'ctxmenu');
  const add = el('div', 'ctxitem', '＋ Add element…');
  add.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); openCatalog(id, 'add'); };
  const copy = el('div', 'ctxitem', '⧉ Copy');
  copy.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); copyElement(id, false); };
  const copyDeep = el('div', 'ctxitem', '⧉ Copy with content (force)');
  copyDeep.title = 'Also copies the internal subtree of custom controls — use for wrapper-style controls';
  copyDeep.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); copyElement(id, true); };
  const paste = el('div', 'ctxitem', '⧉ Paste here');
  paste.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); if (copiedElementId != null) pasteElement(id, copiedElementId, copiedForce); };
  const autoId = el('div', 'ctxitem', '🆔 Unique AutomationId…');
  autoId.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); openAutoIdDialog(id); };
  const extract = el('div', 'ctxitem', '✂ Extract style…');
  extract.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); openExtractStyle(id); };
  const wrap = el('div', 'ctxitem', '▣ Wrap in…');
  wrap.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); openCatalog(id, 'wrap'); };
  const unwrap = el('div', 'ctxitem', '⬚ Unwrap');
  unwrap.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); unwrapElement(id); };
  const up = el('div', 'ctxitem', '↑ Move up');
  up.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); moveElement(id, -1); };
  const down = el('div', 'ctxitem', '↓ Move down');
  down.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); moveElement(id, 1); };
  const rem = el('div', 'ctxitem danger', '✕ Remove element');
  rem.onclick = (e) => { e.stopPropagation(); closeStructureMenu(); removeElement(id); };
  structMenu.append(add, copy, copyDeep, paste, extract, autoId, wrap, unwrap, up, down, rem);
  document.body.appendChild(structMenu);
  const rect = structMenu.getBoundingClientRect();
  structMenu.style.left = Math.min(x, innerWidth - rect.width - 8) + 'px';
  structMenu.style.top = Math.min(y, innerHeight - rect.height - 8) + 'px';
}

function closeStructureMenu() {
  if (structMenu) { structMenu.remove(); structMenu = null; }
}

async function openCatalog(parentId, action) {
  if (!structCatalog) {
    const r = await fetch('/api/structure/catalog');
    structCatalog = (await r.json()).controls;
  }
  closeCatalog();

  const wrapMode = action === 'wrap';
  structBack = el('div', 'modalback');
  structBack.onclick = (e) => { if (e.target === structBack) closeCatalog(); };
  const panel = el('div', 'catalogpanel');

  const head = el('div', 'cataloghead');
  head.appendChild(el('span', 'catalogtitle', wrapMode ? 'Wrap in container' : 'Add element'));
  const input = document.createElement('input');
  input.type = 'text';
  input.placeholder = 'Search controls…';
  input.className = 'catalogsearch';
  head.appendChild(input);

  const list = el('div', 'cataloglist');
  const render = () => {
    const q = input.value.trim().toLowerCase();
    list.innerHTML = '';
    for (const c of structCatalog) {
      if (wrapMode && !c.container) continue;
      if (q && !c.name.toLowerCase().includes(q) && !c.description.toLowerCase().includes(q)) continue;
      const row = el('div', 'catalogrow');
      const name = el('div', 'catalogname', c.name);
      if (c.custom) name.appendChild(el('span', 'catalogbadge custom', 'custom'));
      if (c.container) name.appendChild(el('span', 'catalogbadge', 'container'));
      row.appendChild(name);
      row.appendChild(el('div', 'catalogdesc', c.description));
      row.onclick = () => (wrapMode ? wrapElement(parentId, c.type) : addElement(parentId, c.type));
      list.appendChild(row);
    }
    if (!list.children.length) list.appendChild(el('div', 'catalogdesc empty', 'No controls match.'));
  };
  input.oninput = render;
  input.onkeydown = (e) => {
    if (e.key === 'Enter') list.querySelector('.catalogrow')?.click();
  };

  panel.append(head, list);
  structBack.appendChild(panel);
  document.body.appendChild(structBack);
  render();
  input.focus();
}

function closeCatalog() {
  if (structBack) { structBack.remove(); structBack = null; }
}

async function addElement(parentId, type) {
  const r = await fetch('/api/element/' + parentId + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'add', type }),
  });
  const data = await r.json();
  closeCatalog();
  if (!data.ok) { alert('Add failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
  onRowClick(data.id);
}

async function wrapElement(id, type) {
  const r = await fetch('/api/element/' + id + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'wrap', type }),
  });
  const data = await r.json();
  closeCatalog();
  if (!data.ok) { alert('Wrap failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
  onRowClick(data.id);
}

async function unwrapElement(id) {
  const r = await fetch('/api/element/' + id + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'unwrap' }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Unwrap failed: ' + (data.error || 'unknown error')); return; }
  if (selectedId === id) selectedId = null;
  await refreshAll(true);
  refreshHistoryIfOpen();
}

async function removeElement(id) {
  const r = await fetch('/api/element/' + id + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'remove' }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Remove failed: ' + (data.error || 'unknown error')); return; }
  if (selectedId === id) selectedId = null;
  await refreshAll(true);
  refreshHistoryIfOpen();
}

async function moveElement(id, delta) {
  if (!delta) return;
  const r = await fetch('/api/element/' + id + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'move', delta }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Move failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
}

// The history panel refreshes itself only on open/undo — structural ops must nudge it.
function refreshHistoryIfOpen() {
  if (!document.getElementById('histpanel').hidden) loadHistory();
}

// ---- toolbox: drag a control from the palette onto the mirror ------------------------------

let toolboxDragType = null;

(async function initToolbox() {
  if (!structCatalog) {
    try {
      const r = await fetch('/api/structure/catalog');
      structCatalog = (await r.json()).controls;
    } catch { return; }
  }
  const list = document.getElementById('toolboxlist');
  const search = document.getElementById('toolboxsearch');
  const render = () => {
    const q = search.value.trim().toLowerCase();
    list.innerHTML = '';
    for (const c of structCatalog) {
      if (q && !c.name.toLowerCase().includes(q)) continue;
      const chip = el('div', 'toolchip', c.name);
      if (c.custom) chip.appendChild(el('span', 'catalogbadge custom', 'custom'));
      chip.draggable = true;
      chip.title = c.description;
      chip.addEventListener('dragstart', (e) => {
        toolboxDragType = c.type;
        e.dataTransfer.effectAllowed = 'copy';
        e.dataTransfer.setData('text/plain', c.type);
      });
      chip.addEventListener('dragend', () => { toolboxDragType = null; unarmMirror(); hideDropHl(); });
      list.appendChild(chip);
    }
  };
  search.oninput = render;
  render();
})();

const mirrorImg = document.getElementById('mirrorimg');

function unarmMirror() { mirrorImg.classList.remove('droparmed'); }

let dropHlAt = 0;

// Window size in dp, axis-corrected for rotation: windowDp refreshes only with the tree,
// but the screenshot's aspect always reflects the CURRENT orientation — when they disagree,
// the device rotated and the axes swap.
function effectiveWindowDp() {
  if (!windowDp) return null;
  if (mirrorImg.naturalWidth > 0 && mirrorImg.naturalHeight > 0) {
    const imgLandscape = mirrorImg.naturalWidth > mirrorImg.naturalHeight;
    const dpLandscape = windowDp[0] > windowDp[1];
    if (imgLandscape !== dpLandscape) return [windowDp[1], windowDp[0]];
  }
  return windowDp;
}

function mirrorPointToDp(e) {
  const dp = effectiveWindowDp();
  const rect = mirrorImg.getBoundingClientRect();
  return [(e.clientX - rect.left) / rect.width * dp[0],
          (e.clientY - rect.top) / rect.height * dp[1]];
}

function moveDropHl(data) {
  let hl = document.getElementById('mirrordrophl');
  if (!hl) {
    hl = el('div');
    hl.id = 'mirrordrophl';
    hl.appendChild(el('span', 'droplabel'));
    document.body.appendChild(hl);
  }
  const dp = effectiveWindowDp();
  const rect = mirrorImg.getBoundingClientRect();
  hl.style.left = rect.left + data.x / dp[0] * rect.width + 'px';
  hl.style.top = rect.top + data.y / dp[1] * rect.height + 'px';
  hl.style.width = data.w / dp[0] * rect.width + 'px';
  hl.style.height = data.h / dp[1] * rect.height + 'px';
  hl.querySelector('.droplabel').textContent = data.label;
  hl.style.display = 'block';
}

function hideDropHl() {
  const hl = document.getElementById('mirrordrophl');
  if (hl) hl.style.display = 'none';
}

mirrorImg.addEventListener('dragover', (e) => {
  if (!toolboxDragType || !windowDp) return;
  e.preventDefault();
  e.dataTransfer.dropEffect = 'copy';
  mirrorImg.classList.add('droparmed');

  // Ask the device which container sits under the cursor — throttled.
  const now = Date.now();
  if (now - dropHlAt < 120) return;
  dropHlAt = now;
  const [x, y] = mirrorPointToDp(e);
  fetch('/api/structure/drop-target', { method: 'POST', body: JSON.stringify({ x, y }) })
    .then(r => r.json())
    .then(data => {
      if (toolboxDragType && data.ok) { moveDropHl(data); showSnapLines(data, [x, y]); }
      else { hideDropHl(); clearSnapLines(); }
    })
    .catch(() => { hideDropHl(); clearSnapLines(); });
});
mirrorImg.addEventListener('dragleave', () => { unarmMirror(); hideDropHl(); clearSnapLines(); });

// The screenshot's orientation drives the maximized layout: landscape → toolbox below the mirror.
mirrorImg.addEventListener('load', () => {
  document.getElementById('mirrorpanel').classList.toggle(
    'landscape', mirrorImg.naturalWidth > mirrorImg.naturalHeight);
});

// ---- mirror zoom & pan ---------------------------------------------------------------------

let mirrorZoom = 1;
let mirrorTx = 0;
let mirrorTy = 0;
let mirrorPanState = null;
let mirrorPanned = false;

const mirrorZoomSlider = document.getElementById('mirrorZoomSlider');
const mirrorZoomVal = document.getElementById('mirrorZoomVal');

function applyMirrorTransform() {
  mirrorImg.style.transform = `translate(${mirrorTx}px, ${mirrorTy}px) scale(${mirrorZoom})`;
  if (typeof renderMirrorAdorners === 'function') renderMirrorAdorners();
  mirrorZoomVal.textContent = Math.round(mirrorZoom * 100) + '%';
  const pct = Math.round(mirrorZoom * 100);
  if (parseInt(mirrorZoomSlider.value, 10) !== pct) mirrorZoomSlider.value = pct;
}

mirrorZoomSlider.addEventListener('input', () => {
  mirrorZoom = mirrorZoomSlider.value / 100;
  applyMirrorTransform();
});

document.getElementById('mirrorFitBtn').addEventListener('click', () => {
  mirrorZoom = 1;
  mirrorTx = 0;
  mirrorTy = 0;
  applyMirrorTransform();
});

// Pinch/ctrl+wheel zooms too, centered on the current view.
document.getElementById('mirrorview').addEventListener('wheel', (e) => {
  if (!e.ctrlKey && !e.metaKey) return;
  e.preventDefault();
  mirrorZoom = Math.min(3, Math.max(0.25, mirrorZoom * (e.deltaY < 0 ? 1.1 : 1 / 1.1)));
  applyMirrorTransform();
}, { passive: false });

// Drag pans; a pan must not fire the click-to-select underneath.
mirrorImg.addEventListener('pointerdown', (e) => {
  if (e.button !== 0) return;
  mirrorPanState = { x: e.clientX, y: e.clientY, tx: mirrorTx, ty: mirrorTy };
  mirrorPanned = false;
});
window.addEventListener('pointermove', (e) => {
  if (!mirrorPanState) return;
  const dx = e.clientX - mirrorPanState.x;
  const dy = e.clientY - mirrorPanState.y;
  if (!mirrorPanned && Math.hypot(dx, dy) < 5) return;
  mirrorPanned = true;
  mirrorImg.classList.add('panning');
  mirrorTx = mirrorPanState.tx + dx;
  mirrorTy = mirrorPanState.ty + dy;
  applyMirrorTransform();
});
window.addEventListener('pointerup', () => {
  mirrorPanState = null;
  mirrorImg.classList.remove('panning');
});
mirrorImg.addEventListener('click', (e) => {
  if (mirrorPanned) {
    e.stopImmediatePropagation();
    e.preventDefault();
    mirrorPanned = false;
  }
}, true);

// Right-click on the mirror: the same structural menu as in the tree, for the element under
// the cursor (which also gets selected, so the menu visibly refers to it).
mirrorImg.addEventListener('contextmenu', async (e) => {
  if (!windowDp) return;
  e.preventDefault();
  const [x, y] = mirrorPointToDp(e);
  const r = await fetch('/api/structure/hit', { method: 'POST', body: JSON.stringify({ x, y }) });
  const data = await r.json();
  if (!data.ok) return;
  await refreshAll(true);
  refreshHistoryIfOpen();
  reveal(data.id);
  onRowClick(data.id);
  openStructureMenu(data.id, e.clientX, e.clientY);
});
mirrorImg.addEventListener('drop', async (e) => {
  unarmMirror();
  hideDropHl();
  clearSnapLines();
  if (!toolboxDragType || !windowDp) return;
  e.preventDefault();
  const [x, y] = mirrorPointToDp(e);
  const type = toolboxDragType;
  toolboxDragType = null;

  const r = await fetch('/api/structure/add-at', {
    method: 'POST', body: JSON.stringify({ x, y, type }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Add failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
  onRowClick(data.id);
});

// ---- drag & drop reorder (same parent only — order elsewhere is set by properties) ----------

let dragRow = null;

const structTree = document.getElementById('tree');

structTree.addEventListener('dragstart', (e) => {
  const row = e.target.closest('.row');
  if (!row || !row.dataset.id) { e.preventDefault(); return; }
  dragRow = row;
  row.classList.add('dragging');
  e.dataTransfer.effectAllowed = 'move';
  e.dataTransfer.setData('text/plain', row.dataset.id);
});

structTree.addEventListener('dragend', () => {
  if (dragRow) dragRow.classList.remove('dragging');
  dragRow = null;
  clearDropMarks();
});

structTree.addEventListener('dragover', (e) => {
  const target = dropTargetAt(e);
  clearDropMarks();
  if (!target) return;
  e.preventDefault();
  e.dataTransfer.dropEffect = 'move';
  target.row.classList.add(target.mode === 'into' ? 'dropinto'
    : target.mode === 'before' ? 'dropabove' : 'dropbelow');
  if (target.mode !== 'into') {
    // Show which parent receives the element.
    const parentRow = target.row.parentElement.parentElement.parentElement?.querySelector(':scope > .row');
    if (parentRow) parentRow.classList.add('dropparent');
  }
});

structTree.addEventListener('drop', async (e) => {
  const target = dropTargetAt(e);
  clearDropMarks();
  if (!target || !dragRow) return;
  e.preventDefault();

  const id = parseInt(dragRow.dataset.id, 10);
  const targetId = parseInt(target.row.dataset.id, 10);

  if (target.mode === 'into') {
    await reparentElement(id, targetId, 0, false);
    return;
  }

  const kids = target.row.parentElement.parentElement;   // target's .kids container
  const sameParent = kids === dragRow.parentElement.parentElement;
  if (sameParent) {
    const nodes = [...kids.children];
    const fromIndex = nodes.indexOf(dragRow.parentElement);
    const targetIndex = nodes.indexOf(target.row.parentElement);
    let to = target.mode === 'before' ? targetIndex : targetIndex + 1;
    if (to > fromIndex) to--;                            // removal shifts everything after the source
    if (to !== fromIndex) await moveElement(id, to - fromIndex);
    return;
  }

  // Dropped between children of another parent: reparent next to that sibling.
  const parentRowEl = kids.parentElement?.querySelector(':scope > .row');
  if (!parentRowEl || !parentRowEl.dataset.id) return;   // root level — nothing to attach to
  await reparentElement(id, parseInt(parentRowEl.dataset.id, 10), targetId, target.mode === 'before');
});

function dropTargetAt(e) {
  if (!dragRow) return null;
  const row = e.target.closest('.row');
  if (!row || row === dragRow || !row.dataset.id) return null;
  if (dragRow.parentElement.contains(row.parentElement)) return null; // own subtree
  const rect = row.getBoundingClientRect();
  const y = e.clientY - rect.top;
  const sameParent = row.parentElement.parentElement === dragRow.parentElement.parentElement;
  if (sameParent) return { row, mode: y < rect.height / 2 ? 'before' : 'after' };
  if (y < rect.height * 0.25) return { row, mode: 'before' };
  if (y > rect.height * 0.75) return { row, mode: 'after' };
  return { row, mode: 'into' };
}

function clearDropMarks() {
  document.querySelectorAll('.row.dropabove, .row.dropbelow, .row.dropinto, .row.dropparent')
    .forEach(r => r.classList.remove('dropabove', 'dropbelow', 'dropinto', 'dropparent'));
}

async function reparentElement(id, parentId, siblingId, before) {
  const r = await fetch('/api/element/' + id + '/structure', {
    method: 'POST', body: JSON.stringify({ op: 'reparent', parent: parentId, sibling: siblingId, before }),
  });
  const data = await r.json();
  if (!data.ok) { alert('Move failed: ' + (data.error || 'unknown error')); return; }
  await refreshAll(true);
  refreshHistoryIfOpen();
}

// ---- mirror adorners: alignment pins + visual grid designer ---------------------------------

let gridInfo = null;
let gridInfoForId = null;

function dpToView(x, y) {
  const dp = effectiveWindowDp();
  const rect = mirrorImg.getBoundingClientRect();
  const view = document.getElementById('mirrorview').getBoundingClientRect();
  return [rect.left - view.left + x / dp[0] * rect.width,
          rect.top - view.top + y / dp[1] * rect.height];
}

function dpScale() {
  const dp = effectiveWindowDp();
  const rect = mirrorImg.getBoundingClientRect();
  return [rect.width / dp[0], rect.height / dp[1]];
}

function adornerHost() {
  let host = document.getElementById('adorners');
  if (!host) {
    host = el('div');
    host.id = 'adorners';
    document.getElementById('mirrorview').appendChild(host);
  }
  return host;
}

async function renderMirrorAdorners() {
  const panel = document.getElementById('mirrorpanel');
  const host = adornerHost();
  host.innerHTML = '';
  const meta = window.selMeta;
  if (panel.hidden || !meta || meta.id == null || !meta.rect || !windowDp) return;

  renderAlignmentPins(host, meta);
  await renderGridDesigner(host, meta);
}

// VS-style pins: left+right = Fill, one side = Start/End, none = Center.
function renderAlignmentPins(host, meta) {
  const [x, y] = dpToView(meta.rect.x, meta.rect.y);
  const [sx, sy] = dpScale();
  const w = meta.rect.w * sx, h = meta.rect.h * sy;

  const engagedH = meta.h === 'Fill' ? 'LR' : meta.h === 'Start' ? 'L' : meta.h === 'End' ? 'R' : '';
  const engagedV = meta.v === 'Fill' ? 'TB' : meta.v === 'Start' ? 'T' : meta.v === 'End' ? 'B' : '';

  const pins = [
    { key: 'L', left: x - 7, top: y + h / 2 - 5, axis: 'h' },
    { key: 'R', left: x + w - 3, top: y + h / 2 - 5, axis: 'h' },
    { key: 'T', left: x + w / 2 - 5, top: y - 7, axis: 'v' },
    { key: 'B', left: x + w / 2 - 5, top: y + h - 3, axis: 'v' },
  ];
  for (const pin of pins) {
    const engaged = (pin.axis === 'h' ? engagedH : engagedV).includes(pin.key);
    const dot = el('div', 'alignpin' + (engaged ? ' on' : ''));
    dot.style.left = pin.left + 'px';
    dot.style.top = pin.top + 'px';
    dot.title = 'Alignment pin — click to anchor/release this edge';
    dot.onclick = (e) => {
      e.stopPropagation();
      togglePin(pin.axis, pin.key, pin.axis === 'h' ? engagedH : engagedV);
    };
    host.appendChild(dot);
  }
}

async function togglePin(axis, key, engaged) {
  const next = engaged.includes(key) ? engaged.replace(key, '') : engaged + key;
  const both = axis === 'h' ? 'LR' : 'TB';
  const startKey = axis === 'h' ? 'L' : 'T';
  let value;
  if (next.includes(both[0]) && next.includes(both[1])) value = 'Fill';
  else if (next.includes(startKey)) value = 'Start';
  else if (next.length) value = 'End';
  else value = 'Center';

  await fetch('/api/element/' + window.selMeta.id + '/property', {
    method: 'POST',
    body: JSON.stringify({ section: 'Layout', name: axis === 'h' ? 'HorizontalOptions' : 'VerticalOptions', value }),
  });
  if (selectedId != null) loadProps(selectedId, true);
}

async function renderGridDesigner(host, meta) {
  if (gridInfoForId !== meta.id) {
    gridInfoForId = meta.id;
    try {
      gridInfo = await (await fetch('/api/structure/grid-info', {
        method: 'POST', body: JSON.stringify({ id: meta.id }),
      })).json();
    } catch { gridInfo = { ok: false }; }
  }
  if (!gridInfo || !gridInfo.ok) return;

  const g = gridInfo;
  const [sx, sy] = dpScale();
  const [gx, gy] = dpToView(g.x, g.y);
  const gw = g.w * sx, gh = g.h * sy;

  const frame = el('div', 'gridframe');
  frame.style.left = gx + 'px';
  frame.style.top = gy + 'px';
  frame.style.width = gw + 'px';
  frame.style.height = gh + 'px';
  host.appendChild(frame);

  const makeLine = (vertical, edgeDp, trackIndex, defs, section) => {
    const line = el('div', vertical ? 'gridline v' : 'gridline h');
    if (vertical) {
      line.style.left = (edgeDp - g.x) * sx - 2 + 'px';
      line.style.top = '0px';
      line.style.height = gh + 'px';
    } else {
      line.style.top = (edgeDp - g.y) * sy - 2 + 'px';
      line.style.left = '0px';
      line.style.width = gw + 'px';
    }
    line.title = `${section} ${trackIndex}: ${defs[trackIndex]} — drag to resize (sets absolute dp)`;

    line.addEventListener('pointerdown', (down) => {
      down.preventDefault();
      down.stopPropagation();
      line.setPointerCapture(down.pointerId);
      const start = vertical ? down.clientX : down.clientY;
      const move = (ev) => {
        const delta = (vertical ? ev.clientX : ev.clientY) - start;
        line.style.transform = vertical ? `translateX(${delta}px)` : `translateY(${delta}px)`;
      };
      const up = async (ev) => {
        line.releasePointerCapture(down.pointerId);
        window.removeEventListener('pointermove', move);
        window.removeEventListener('pointerup', up);
        const deltaDp = ((vertical ? ev.clientX : ev.clientY) - start) / (vertical ? sx : sy);
        const prevEdge = vertical ? g.colEdges[trackIndex] : g.rowEdges[trackIndex];
        const size = Math.max(4, Math.round(edgeDp + deltaDp - prevEdge));
        await fetch('/api/element/' + meta.id + '/property', {
          method: 'POST',
          body: JSON.stringify({ section, name: `${section === 'Rows' ? 'Row' : 'Column'} ${trackIndex}`, value: String(size) }),
        });
        gridInfoForId = null; // re-fetch geometry
        if (selectedId != null) loadProps(selectedId, true);
      };
      window.addEventListener('pointermove', move);
      window.addEventListener('pointerup', up);
    });
    frame.appendChild(line);
  };

  for (let i = 1; i < g.rowEdges.length - 1; i++) makeLine(false, g.rowEdges[i], i - 1, g.rows, 'Rows');
  for (let i = 1; i < g.colEdges.length - 1; i++) makeLine(true, g.colEdges[i], i - 1, g.cols, 'Columns');

  const addBar = el('div', 'gridaddbar');
  const addRow = el('button', '', '+row');
  addRow.title = 'Add a row (Auto)';
  addRow.onclick = () => runGridAction(meta.id, 'Rows', '＋ Add row (Auto)');
  const addCol = el('button', '', '+col');
  addCol.title = 'Add a column (Auto)';
  addCol.onclick = () => runGridAction(meta.id, 'Columns', '＋ Add column (Auto)');
  addBar.append(addRow, addCol);
  frame.appendChild(addBar);
}

async function runGridAction(id, section, label) {
  await fetch('/api/element/' + id + '/action', {
    method: 'POST', body: JSON.stringify({ section, name: label }),
  });
  gridInfoForId = null;
  await refreshAll(true);
}

mirrorImg.addEventListener('load', () => { if (typeof renderMirrorAdorners === 'function') renderMirrorAdorners(); });

// ---- snap lines while dragging from the toolbox ---------------------------------------------

function showSnapLines(data, cursorDp) {
  clearSnapLines();
  if (!data.children || !data.children.length) return;
  const rect = mirrorImg.getBoundingClientRect();
  const dp = effectiveWindowDp();
  const threshold = 6;

  const xs = [], ys = [];
  for (const c of data.children) {
    xs.push(c.x, c.x + c.w, c.x + c.w / 2);
    ys.push(c.y, c.y + c.h, c.y + c.h / 2);
  }
  const nearest = (arr, v) => arr.reduce((best, e) =>
    Math.abs(e - v) < Math.abs(best - v) ? e : best, arr[0]);

  const host = document.body;
  const nx = nearest(xs, cursorDp[0]);
  if (Math.abs(nx - cursorDp[0]) <= threshold) {
    const line = el('div', 'snapline v');
    line.style.left = rect.left + nx / dp[0] * rect.width + 'px';
    line.style.top = rect.top + 'px';
    line.style.height = rect.height + 'px';
    host.appendChild(line);
  }
  const ny = nearest(ys, cursorDp[1]);
  if (Math.abs(ny - cursorDp[1]) <= threshold) {
    const line = el('div', 'snapline h');
    line.style.top = rect.top + ny / dp[1] * rect.height + 'px';
    line.style.left = rect.left + 'px';
    line.style.width = rect.width + 'px';
    host.appendChild(line);
  }
}

function clearSnapLines() {
  document.querySelectorAll('.snapline').forEach(l => l.remove());
}

// ---- XAML preview of the selection ----------------------------------------------------------

async function toggleXamlPreview() {
  const pre = document.getElementById('xamlpre');
  const on = pre.hidden;
  pre.hidden = !on;
  document.getElementById('xamlPreviewBtn').classList.toggle('active', on);
  if (on) refreshXamlPreview();
}

async function refreshXamlPreview() {
  const pre = document.getElementById('xamlpre');
  if (pre.hidden) return;
  if (selectedId == null) { pre.textContent = '(no selection)'; return; }
  try {
    pre.textContent = await (await fetch('/api/element/' + selectedId + '/xaml')).text();
  } catch {
    pre.textContent = '(unavailable)';
  }
}

// ---- Extract style: property picker dialog --------------------------------------------------

async function openExtractStyle(id) {
  let data;
  try {
    data = await (await fetch('/api/element/' + id + '/style-candidates')).json();
  } catch { return; }
  if (!data.candidates.length) { alert('No local property values to extract on this element.'); return; }

  closeCatalog();
  structBack = el('div', 'modalback');
  structBack.onclick = (e) => { if (e.target === structBack) closeCatalog(); };
  const panel = el('div', 'catalogpanel');

  const head = el('div', 'cataloghead');
  head.appendChild(el('span', 'catalogtitle', 'Extract style'));
  const keyInput = document.createElement('input');
  keyInput.type = 'text';
  keyInput.className = 'catalogsearch';
  keyInput.value = data.type + 'Style';
  keyInput.title = 'Resource key for the new style';
  head.appendChild(keyInput);

  const list = el('div', 'cataloglist');
  const checks = [];
  for (const candidate of data.candidates) {
    const row = el('label', 'catalogrow extractrow');
    const check = document.createElement('input');
    check.type = 'checkbox';
    check.checked = candidate.checked;
    check.dataset.name = candidate.name;
    checks.push(check);
    row.appendChild(check);
    const name = el('div', 'catalogname', candidate.name);
    row.appendChild(name);
    row.appendChild(el('div', 'catalogdesc', candidate.value));
    list.appendChild(row);
  }

  const foot = el('div', 'cataloghead');
  const go = document.createElement('button');
  go.textContent = 'Extract';
  go.onclick = async () => {
    const props = checks.filter(c => c.checked).map(c => c.dataset.name);
    const r = await (await fetch('/api/element/' + id + '/structure', {
      method: 'POST',
      body: JSON.stringify({ op: 'extract-style', key: keyInput.value.trim(), props }),
    })).json();
    if (!r.ok) { alert('Extract failed: ' + (r.error || 'unknown error')); return; }
    closeCatalog();
    await refreshAll(true);
    refreshHistoryIfOpen();
    if (selectedId != null) await loadProps(selectedId, true);
    if (!document.getElementById('resback').hidden) loadResources();
  };
  foot.appendChild(go);

  panel.append(head, list, foot);
  structBack.appendChild(panel);
  document.body.appendChild(structBack);
  keyInput.focus();
  keyInput.select();
}

// ---- Unique AutomationId for data-templated items -------------------------------------------

async function openAutoIdDialog(id) {
  let data;
  try {
    data = await (await fetch('/api/element/' + id + '/automationid-candidates')).json();
  } catch { return; }
  if (data.error) { alert(data.error); return; }
  if (!data.candidates.length) { alert('The BindingContext has no simple properties to bind to.'); return; }

  closeCatalog();
  structBack = el('div', 'modalback');
  structBack.onclick = (e) => { if (e.target === structBack) closeCatalog(); };
  const panel = el('div', 'catalogpanel');

  const head = el('div', 'cataloghead');
  head.appendChild(el('span', 'catalogtitle', 'AutomationId from item data (' + data.count + ' instances)'));
  const prefix = document.createElement('input');
  prefix.type = 'text';
  prefix.className = 'catalogsearch';
  prefix.value = data.type.toLowerCase();
  prefix.title = 'Prefix — the id becomes prefix-{value}; empty = raw value';
  head.appendChild(prefix);

  const list = el('div', 'cataloglist');
  let picked = null;
  const rows = [];
  const renderPreviews = () => {
    for (const r of rows) {
      const p = prefix.value.trim();
      r.previewEl.textContent = '→ ' + r.preview.map(v => (p ? p + '-' : '') + v).join(', ')
        + (data.count > r.preview.length ? ', …' : '')
        + (r.unique ? '' : '   ⚠ not unique across instances');
    }
  };
  prefix.oninput = renderPreviews;

  for (const candidate of data.candidates) {
    const row = el('label', 'catalogrow extractrow');
    const radio = document.createElement('input');
    radio.type = 'radio';
    radio.name = 'autoidprop';
    radio.onchange = () => { picked = candidate.name; };
    row.appendChild(radio);
    row.appendChild(el('div', 'catalogname', candidate.name + (candidate.unique ? '' : ' ⚠')));
    const previewEl = el('div', 'catalogdesc', '');
    row.appendChild(previewEl);
    rows.push({ preview: candidate.preview, previewEl: previewEl, unique: candidate.unique });
    list.appendChild(row);
  }
  renderPreviews();

  const foot = el('div', 'cataloghead');
  const go = document.createElement('button');
  go.textContent = 'Bind AutomationId';
  go.onclick = async () => {
    if (!picked) { alert('Pick a property.'); return; }
    const p = prefix.value.trim();
    const r = await (await fetch('/api/element/' + id + '/automationid-bind', {
      method: 'POST',
      body: JSON.stringify({ path: picked, format: p ? p + '-{0}' : '' }),
    })).json();
    if (!r.ok) { alert('Bind failed: ' + (r.error || 'unknown error')); return; }
    closeCatalog();
    await refreshAll(true);
    if (selectedId != null) await loadProps(selectedId, true);
  };
  foot.appendChild(go);

  panel.append(head, list, foot);
  structBack.appendChild(panel);
  document.body.appendChild(structBack);
}
