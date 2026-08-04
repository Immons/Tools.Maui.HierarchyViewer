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
    const tick = () => { img.src = '/api/screenshot?t=' + Date.now(); };
    tick();
    mirrorTimer = setInterval(tick, 800);
  } else if (mirrorTimer) {
    clearInterval(mirrorTimer);
    mirrorTimer = null;
  }
}

// Click on the mirror selects the element under the cursor (window-dp coordinates).
document.getElementById('mirrorimg').addEventListener('click', (e) => {
  if (!windowDp) return;
  const img = e.target;
  const rect = img.getBoundingClientRect();
  const x = (e.clientX - rect.left) / rect.width * windowDp[0];
  const y = (e.clientY - rect.top) / rect.height * windowDp[1];
  fetch('/api/select-at', { method: 'POST', body: JSON.stringify({ x: x, y: y }) });
});

async function clearHl() {
  compareId = null;
  markRows();
  await fetch('/api/clear', { method: 'POST' });
}
