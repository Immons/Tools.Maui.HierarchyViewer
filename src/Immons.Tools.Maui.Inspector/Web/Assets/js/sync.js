// Device ↔ web sync: poll selection, compare target and mode flags every second.
setInterval(async () => {
  try {
    // A backgrounded app does not refuse the connection — iOS suspends the whole process, so the
    // request simply never comes back. Without this timeout the poll hangs forever and the panel
    // keeps showing the last (green) state while nothing works.
    const ctl = new AbortController();
    const timer = setTimeout(() => ctl.abort(), 2500);
    let r;
    try {
      r = await fetch('/api/selection', { signal: ctl.signal });
    } finally {
      clearTimeout(timer);
    }
    const d = await r.json();
    setConnected(true, d.fg !== false);

    if (d.measure !== measure)
      setMeasureUi(d.measure);

    if (d.wysiwyg !== wysiwyg)
      setWysiwygUi(d.wysiwyg);

    if (d.select !== undefined && d.select !== selectMode)
      setSelectUi(d.select);

    if (d.overlay !== undefined && d.overlay !== overlayShown)
      setOverlayUi(d.overlay);

    if (d.paint !== undefined && d.paint !== debugPaint)
      setPaintUi(d.paint);

    if (d.slow !== undefined && d.slow !== slowOn)
      setSlowUi(d.slow);

    if (d.perf !== undefined) {
      if ((d.perf != null) !== perfOn) setPerfUi(d.perf != null);
      document.getElementById('perfout').textContent =
        d.perf ? (d.perf.fps + ' fps · avg ' + d.perf.avg + ' ms · worst ' + d.perf.worst + ' ms') : '';
    }

    if (d.sync !== undefined && d.sync !== syncConnected) {
      syncConnected = d.sync;
      updateHint();
    }

    if (d.hseq !== undefined && d.hseq !== histSeq) {
      histSeq = d.hseq;
      if (!document.getElementById('histpanel').hidden)
        await loadHistory();
    }

    if (activeView === 'network') refreshNetworkView();
    else if (activeView === 'logs') loadLogs();

    const cmp = d.compare ?? null;
    if (cmp !== compareId) {
      compareId = cmp;
      if (compareId != null && !document.querySelector('.row[data-id="' + compareId + '"]'))
        await refreshAll();
      if (compareId != null) reveal(compareId);
      markRows();
    }

    if (d.id != null && d.id !== selectedId) {
      selectedId = d.id;
      if (!reveal(d.id)) {
        await refreshAll();
        reveal(d.id);
      }
      markRows();
      await loadProps(d.id, false);
    }
  } catch (e) {
    // Timed out = the process is alive but parked (backgrounded). Refused = it is really gone.
    if (e && e.name === 'AbortError')
      setConnected(true, false);
    else
      setConnected(false);
  }
}, 1000);

refreshAll();

showVersion();   // header: package version + "newer available" hint
