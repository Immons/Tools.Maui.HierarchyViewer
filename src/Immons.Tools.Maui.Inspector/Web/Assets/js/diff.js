// Dump diff: baseline in localStorage, line-based LCS, rendered in a new tab.
async function dumpDiff(e) {
  const dump = await (await fetch('/api/dump')).text();
  if (e.shiftKey) {
    localStorage.removeItem('hvBaseline');
  }
  const base = localStorage.getItem('hvBaseline');
  if (!base) {
    localStorage.setItem('hvBaseline', dump);
    const w = window.open('');
    w.document.write('<pre style="font:13px monospace;padding:16px">Baseline saved ('
      + dump.split('\n').length + ' lines).\nMake some changes, then click Δ Diff again.\nShift-click Δ Diff to reset the baseline.</pre>');
    return;
  }

  const a = base.split('\n'), b = dump.split('\n');
  const n = a.length, m = b.length;
  // LCS table (dumps are typically hundreds of lines — fine)
  const dp = Array.from({ length: n + 1 }, () => new Int32Array(m + 1));
  for (let i = n - 1; i >= 0; i--)
    for (let j = m - 1; j >= 0; j--)
      dp[i][j] = a[i] === b[j] ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]);

  const esc = (s) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
  let out = '', i = 0, j = 0, changes = 0;
  while (i < n && j < m) {
    if (a[i] === b[j]) { out += '<span class="s">  ' + esc(a[i]) + '</span>\n'; i++; j++; }
    else if (dp[i + 1][j] >= dp[i][j + 1]) { out += '<span class="d">- ' + esc(a[i]) + '</span>\n'; i++; changes++; }
    else { out += '<span class="a">+ ' + esc(b[j]) + '</span>\n'; j++; changes++; }
  }
  for (; i < n; i++, changes++) out += '<span class="d">- ' + esc(a[i]) + '</span>\n';
  for (; j < m; j++, changes++) out += '<span class="a">+ ' + esc(b[j]) + '</span>\n';

  const w = window.open('');
  w.document.write('<title>Dump diff</title><style>body{background:#1e2028;color:#9ba0ae;font:12px ui-monospace,Menlo,monospace;padding:14px}'
    + '.a{color:#7fd48a}.d{color:#e08585}.s{opacity:.55}</style>'
    + '<div style="margin-bottom:10px;color:#ececf1">' + changes + ' changed line(s) vs baseline. Shift-click Δ Diff in the inspector to reset.</div>'
    + '<pre>' + out + '</pre>');
}
