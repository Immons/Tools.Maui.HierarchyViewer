// Package version in the header, with a hint when a newer one is on nuget.org.
// The check is a plain GET of a public index and fails silently when offline.
const NUGET_INDEX = 'https://api.nuget.org/v3-flatcontainer/immons.tools.maui.inspector/index.json';

function compareVersions(a, b) {
  const pa = a.split('.').map(Number), pb = b.split('.').map(Number);
  for (let i = 0; i < Math.max(pa.length, pb.length); i++) {
    const d = (pa[i] || 0) - (pb[i] || 0);
    if (d) return d;
  }
  return 0;
}

async function showVersion() {
  const host = document.getElementById('ver');
  if (!host) return;
  let current = '';
  try {
    current = (await (await fetch('/api/ping')).json()).version || '';
  } catch { return; }
  if (!current) return;

  host.textContent = 'v' + current;
  host.title = 'Immons.Tools.Maui.Inspector ' + current;

  try {
    const versions = (await (await fetch(NUGET_INDEX)).json()).versions || [];
    const stable = versions.filter(v => !v.includes('-'));
    const latest = stable[stable.length - 1];
    if (!latest) return;
    if (compareVersions(latest, current) > 0) {
      host.textContent = 'v' + current + ' → ' + latest + ' available';
      host.classList.add('verold');
      host.title = 'Newer package on nuget.org: ' + latest;
    } else {
      host.classList.add('verok');
      host.title = 'Immons.Tools.Maui.Inspector ' + current + ' — latest';
    }
  } catch {
    // No connection to nuget.org — the local version alone is still useful.
  }
}
