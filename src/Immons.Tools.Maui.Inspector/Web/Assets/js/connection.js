// Connection state of the panel ↔ app link. Without it the panel looks alive after the app
// goes away: buttons still click, nothing happens.
let connected = null;   // null = not known yet, so the first result always renders

function setConnected(up) {
  if (up === connected) return;
  connected = up;

  const host = document.getElementById('conn');
  if (!host) return;
  host.classList.toggle('up', up);
  host.classList.toggle('down', !up);
  host.title = up
    ? 'Connected to the app'
    : 'No connection to the app — it may have been stopped, restarted on another port, or lost its adb forward';

  const label = document.getElementById('win');
  if (!up && label) {
    // Keep what we knew about the device, just say it is gone.
    connection_lastDevice = connection_lastDevice || label.textContent;
    label.textContent = 'disconnected';
  } else if (up && label && connection_lastDevice) {
    label.textContent = connection_lastDevice;
    connection_lastDevice = '';
  }
}

let connection_lastDevice = '';
