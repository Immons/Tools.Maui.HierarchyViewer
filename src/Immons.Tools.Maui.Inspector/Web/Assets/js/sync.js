// Device ↔ web sync: poll selection, compare target and mode flags every second.
setInterval(async () => {
  try {
    const r = await fetch('/api/selection');
    const d = await r.json();
    setConnected(true);

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
  } catch {
    // The app is gone (stopped, restarted on another port, adb forward dropped). Say so —
    // otherwise the panel keeps accepting clicks that quietly do nothing.
    setConnected(false);
  }
}, 1000);

refreshAll();

showVersion();   // header: package version + "newer available" hint
