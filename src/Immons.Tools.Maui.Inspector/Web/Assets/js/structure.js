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
  structMenu.append(add, copy, copyDeep, paste, wrap, unwrap, up, down, rem);
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
    .then(data => { if (toolboxDragType && data.ok) moveDropHl(data); else hideDropHl(); })
    .catch(hideDropHl);
});
mirrorImg.addEventListener('dragleave', () => { unarmMirror(); hideDropHl(); });

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
