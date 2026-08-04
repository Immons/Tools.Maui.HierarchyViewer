// Keyboard navigation: ↑/↓ walk the visible tree rows (selecting on the device too),
// ←/→ collapse/expand the current node. Works also while the search box is focused.
function visibleRows() {
  return [...document.querySelectorAll('#tree .row')].filter(r => r.offsetParent !== null);
}

document.addEventListener('keydown', (e) => {
  const t = e.target;
  const inSearch = t && t.id === 'search';
  if (!inSearch && t && (t.tagName === 'INPUT' || t.tagName === 'SELECT' || t.tagName === 'TEXTAREA')) return;

  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    const rows = visibleRows();
    if (!rows.length) return;
    e.preventDefault();
    // In measure mode the arrows move the compare target, anchored on it.
    const anchor = (measure && compareId != null) ? compareId : selectedId;
    let idx = rows.findIndex(r => +r.dataset.id === anchor);
    if (idx < 0) idx = e.key === 'ArrowDown' ? -1 : rows.length;
    idx = e.key === 'ArrowDown' ? Math.min(idx + 1, rows.length - 1) : Math.max(idx - 1, 0);
    onRowClick(+rows[idx].dataset.id);
    rows[idx].scrollIntoView({ block: 'nearest' });
  } else if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && !inSearch) {
    const row = document.querySelector('.row[data-id="' + selectedId + '"]');
    if (!row) return;
    const node = row.parentElement;
    if (!node.querySelector(':scope > .kids')) return;
    e.preventDefault();
    const collapse = e.key === 'ArrowLeft';
    node.classList.toggle('collapsed', collapse);
    const c = node.querySelector(':scope > .row > .caret');
    if (c) c.textContent = collapse ? '▸' : '▾';
  }
});
