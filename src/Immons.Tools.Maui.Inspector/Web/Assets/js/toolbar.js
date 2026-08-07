// Header toolbar: mode toggles mirrored to the device via POST {"on":bool}.

function setSelectUi(on) {
  selectMode = on;
  document.getElementById('selectBtn').classList.toggle('active', on);
}

function toggleSelect() {
  setSelectUi(!selectMode);
  fetch('/api/select-mode', { method: 'POST', body: JSON.stringify({ on: selectMode }) });
}

function setOverlayUi(on) {
  overlayShown = on;
  document.getElementById('overlayBtn').classList.toggle('active', on);
}

function toggleOverlay() {
  setOverlayUi(!overlayShown);
  fetch('/api/overlay', { method: 'POST', body: JSON.stringify({ on: overlayShown }) });
}

function setWysiwygUi(on) {
  wysiwyg = on;
  document.getElementById('wysiwygBtn').classList.toggle('active', on);
  updateHint();
}

function toggleWysiwyg() {
  setWysiwygUi(!wysiwyg);
  fetch('/api/wysiwyg', { method: 'POST', body: JSON.stringify({ on: wysiwyg }) });
}

function setMeasureUi(on) {
  measure = on;
  document.getElementById('measureBtn').classList.toggle('active', on);
  updateHint();
}

function toggleMeasure() {
  setMeasureUi(!measure);
  fetch('/api/measure-mode', { method: 'POST', body: JSON.stringify({ on: measure }) });
  if (!measure) { compareId = null; markRows(); }
}

function setPaintUi(on) {
  debugPaint = on;
  document.getElementById('paintBtn').classList.toggle('active', on);
}

function togglePaint() {
  setPaintUi(!debugPaint);
  fetch('/api/debug-paint', { method: 'POST', body: JSON.stringify({ on: debugPaint }) });
}

function setPerfUi(on) {
  perfOn = on;
  document.getElementById('perfBtn').classList.toggle('active', on);
  if (!on) document.getElementById('perfout').textContent = '';
}

function togglePerf() {
  setPerfUi(!perfOn);
  fetch('/api/perf', { method: 'POST', body: JSON.stringify({ on: perfOn }) });
}

function setSlowUi(on) {
  slowOn = on;
  document.getElementById('slowBtn').classList.toggle('active', on);
}

function toggleSlow() {
  setSlowUi(!slowOn);
  fetch('/api/slow-animations', { method: 'POST', body: JSON.stringify({ on: slowOn }) });
}

function toggleMirror() {
  const panel = document.getElementById('mirrorpanel');
  const on = panel.hidden;
  panel.hidden = !on;
  document.getElementById('mirrorBtn').classList.toggle('active', on);
  if (on) {
    const img = document.getElementById('mirrorimg');
    const tick = () => { img.src = (window.apiBase || '') + '/api/screenshot?t=' + Date.now(); };
    tick();
    mirrorTimer = setInterval(tick, 800);
  } else if (mirrorTimer) {
    clearInterval(mirrorTimer);
    mirrorTimer = null;
  }
}

// Click on the mirror: with Select mode on it selects the element under the cursor;
// with it off the tap is forwarded into the app itself (window-dp coordinates) and the
// mirror takes keyboard focus, so typing reaches the app's focused text input too.
const mirrorImgEl = document.getElementById('mirrorimg');
mirrorImgEl.tabIndex = 0;
mirrorImgEl.addEventListener('click', (e) => {
  if (!windowDp) return;
  // mirrorPointToDp (structure.js) is rotation-aware — the axes swap when the
  // screenshot's orientation disagrees with the last-known window size.
  const [x, y] = mirrorPointToDp(e);
  const route = selectMode ? '/api/select-at' : '/api/tap';
  fetch(route, { method: 'POST', body: JSON.stringify({ x: x, y: y }) });
});

// Keyboard pass-through (Select off + mirror focused): characters and editing keys go to
// the app's focused Entry/Editor; Escape gives the keyboard back to the inspector.
const mirrorEditKeys = ['Backspace', 'Delete', 'Enter', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
mirrorImgEl.addEventListener('keydown', (e) => {
  if (selectMode) return;
  if (e.key === 'Escape') { mirrorImgEl.blur(); return; }
  let payload = null;
  if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) payload = { text: e.key };
  else if (mirrorEditKeys.includes(e.key)) payload = { key: e.key };
  if (!payload) return;
  e.preventDefault();
  e.stopPropagation();
  fetch('/api/key', { method: 'POST', body: JSON.stringify(payload) });
});
mirrorImgEl.addEventListener('paste', (e) => {
  if (selectMode) return;
  const text = (e.clipboardData || window.clipboardData)?.getData('text');
  if (!text) return;
  e.preventDefault();
  e.stopPropagation();
  fetch('/api/key', { method: 'POST', body: JSON.stringify({ text: text }) });
});

// The mirror is the heart of the inspector — have it running from the first paint.
toggleMirror();

async function clearHl() {
  compareId = null;
  markRows();
  await fetch('/api/clear', { method: 'POST' });
}
