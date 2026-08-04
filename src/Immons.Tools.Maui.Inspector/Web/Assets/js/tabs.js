// Edit-history panel, docked at the bottom of the Inspector view.
async function toggleHistory() {
  showView('inspector');
  const panel = document.getElementById('histpanel');
  panel.hidden = !panel.hidden;
  document.getElementById('histBtn').classList.toggle('active', !panel.hidden);
  if (!panel.hidden) loadHistory();
}

async function loadHistory() {
  const r = await fetch('/api/history');
  const data = await r.json();
  const list = document.getElementById('histlist');
  list.innerHTML = data.entries.length ? '' : '<div class="hentry">No edits yet.</div>';

  for (const e of data.entries) {
    const div = document.createElement('div');
    div.className = 'hentry';

    const meta = document.createElement('div');
    meta.className = 'meta';
    meta.textContent = e.time + '  ·  ' + e.element + '  ·  ' + e.section + ' / ' + e.name;
    if (e.canUndo) {
      const undo = document.createElement('button');
      undo.textContent = '⟲ Undo';
      undo.onclick = async () => {
        const res = await fetch('/api/history/undo', { method: 'POST', body: JSON.stringify({ seq: e.seq }) });
        if ((await res.json()).ok) {
          await loadHistory();
          if (selectedId != null) await loadProps(selectedId, true);
          if (TREE_LABEL_PROPS.includes(e.name))
            await refreshAll(true);
        }
      };
      meta.appendChild(undo);
    }
    div.appendChild(meta);

    const vals = document.createElement('div');
    vals.className = 'vals';
    const oldSpan = document.createElement('span');
    oldSpan.className = 'old';
    oldSpan.textContent = e.old === '' ? '(unset)' : e.old;
    const newSpan = document.createElement('span');
    newSpan.className = 'new';
    newSpan.textContent = e.new === '' ? '(unset)' : e.new;
    vals.append(oldSpan, '  →  ', newSpan);
    div.appendChild(vals);

    list.appendChild(div);
  }
}

// loadNetwork lives in mock.js — the Network tab also hosts breakpoints and mock rules.

async function loadLogs() {
  const data = await (await fetch('/api/logs')).json();
  const list = document.getElementById('loglist');
  list.innerHTML = data.entries.length ? '' : '<div class="logrow">No logs yet — call builder.Logging.AddMauiInspector() in MauiProgram.</div>';
  for (const e of data.entries) {
    const div = document.createElement('div');
    div.className = 'logrow';
    const lvl = document.createElement('span');
    lvl.className = 'lvl-' + e.level;
    lvl.textContent = e.level.padEnd(11);
    const cat = document.createElement('span');
    cat.className = 'cat';
    cat.textContent = ' [' + e.category + '] ';
    div.append(e.time + '  ', lvl, cat, e.message);
    list.appendChild(div);
  }
}
