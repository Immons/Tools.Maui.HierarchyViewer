// Connection state of the panel ↔ app link. Without it the panel looks alive after the app
// goes away: buttons still click, nothing happens. Three states, because "reachable" and
// "able to act" are not the same thing — a backgrounded app still answers HTTP while its main
// thread is parked, so edits would queue up and appear to do nothing.
let connState = '';   // '' = not known yet, so the first result always renders
let connDevice = '';

function setConnected(up, foreground) {
  const state = !up ? 'down' : (foreground === false ? 'bg' : 'up');
  if (state === connState) return;
  connState = state;

  const host = document.getElementById('conn');
  const label = document.getElementById('win');
  if (!host || !label) return;

  host.classList.toggle('up', state === 'up');
  host.classList.toggle('bg', state === 'bg');
  host.classList.toggle('down', state === 'down');

  if (state === 'up') {
    host.title = 'Connected to the app';
    if (connDevice) { label.textContent = connDevice; connDevice = ''; }
  } else {
    if (!connDevice) connDevice = label.textContent;
    if (state === 'bg') {
      host.title = 'The app is not responding — most likely in the background (iOS suspends the whole process). '
        + 'Bring it back on screen; edits made now would not apply.';
      label.textContent = 'app in background';
    } else {
      host.title = 'No connection to the app — it may have been stopped, restarted on another port, or lost its adb forward';
      label.textContent = 'disconnected';
    }
  }
}
