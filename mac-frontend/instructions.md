Here is a clear product plan you can hand to a coding LLM as a spec.

## 1. Product summary

**Platform:** macOS
**App type:** Electron desktop app
**Goal:**
A minimal desktop app that:

1. Runs in the background.
2. Continuously captures what the user sees on screen.
3. Sends screenshots to a backend service for analysis (dummy for now).
4. When a phishing risk is detected (dummy logic), shows a small, clean warning panel just under the cursor.

For this MVP, phishing detection is simulated. The main focus is:

- The transparent overlay.
- Screen capture and image sending.
- A simple client side state machine for alerts.
- A small visual panel that follows the cursor only when an alert is active.

---

## 2. High level architecture

Electron app with:

1. **Main process**

   - Creates and manages windows.
   - Manages app lifecycle and macOS specific behavior.
   - Coordinates screen capture permissions.
   - Handles communication between:

     - Screen capture logic.
     - Overlay UI window.
     - Backend request module.

2. **Renderer process: Overlay window**

   - Transparent window that covers the whole screen.
   - Listens for alert events from the main process.
   - Tracks cursor position.
   - Renders a small, minimal panel below the cursor when an alert is active.

3. **Preload scripts**

   - Provide a safe IPC bridge between renderer and main.
   - Expose minimal methods, for example:

     - `window.api.onPhishingAlert(...)`
     - `window.api.onAlertClear(...)`

4. **Backend API (dummy)**

   - For the MVP, assume a fixed HTTP endpoint such as `http://localhost:3000/analyze`.
   - The Electron app sends a frame plus metadata.
   - Backend returns a structured JSON response, even if it is dummy.

---

## 3. User experience

**Normal state:**

- App runs in the background.
- User does not see anything on screen.
- No dock icon is necessary during normal use, but a simple tray icon is useful:

  - Left click: show a minimal menu (quit, pause, settings).
  - Right click: same menu.

**When phishing is detected:**

1. User is interacting normally with the screen.
2. Backend response for the current frame flags `isPhishing = true`.
3. The app triggers an overlay alert:

   - A small, rounded rectangle panel appears just under the cursor position.
   - The rest of the screen is untouched and still fully interactive.

4. Panel content:

   - Short warning title.
   - One line description.
   - Optional small icon, for example a subtle warning icon.

5. The alert auto hides after a few seconds, unless a new alert arrives.

**User interaction with the panel:**

- The overlay window is click through by default, to not interfere with normal use.
- Only the panel area intercepts clicks, so in a future iteration the user could click for more info.
- For this MVP, clicking the panel can simply dismiss it.

---

## 4. Windows and layout design

**Overlay window**

- Type: Frameless, transparent Electron BrowserWindow.
- Size: Full screen, covers primary display.
  If multi monitor support is needed later, the same concept can be extended.
- Properties:

  - Always on top.
  - Transparent background.
  - Ignored by mouse except over the panel area.

- Renderer responsibilities:

  - Maintain a hidden panel component, initially `display: none`.
  - When an alert starts, show the panel and update its position based on cursor coordinates.
  - When alert ends, hide the panel again.

**Possible second hidden window (optional)**

- A secondary, hidden window or background module in the main process can handle screen capture and backend logic.
- This keeps the overlay renderer focused on visuals.

---

## 5. Screen capture flow

Goal: Capture what the user sees and send frames at an adjustable interval.

**Steps:**

1. On app startup:

   - Request macOS screen recording permission when first capturing.
   - Use Electron screen capture features (desktopCapturer with getUserMedia).
   - Choose the full screen source.

2. Capture strategy:

   - Set an interval, for example every N milliseconds (configurable).
   - Grab a frame from the screen stream.
   - Downscale or compress to a manageable size for prototype:

     - For example, reduce resolution and convert to JPEG or PNG.

   - Package frame plus metadata:

     - Timestamp.
     - Active app identifier (if available).
     - Screen resolution.

3. Send to backend:

   - Use a simple HTTP POST to the dummy backend.
   - Endpoint suggested: `POST /analyze`
   - Request example:

     ```json
     {
       "image": "<base64 encoded frame>",
       "timestamp": 1234567890,
       "app": "com.apple.Safari"
     }
     ```

4. Receive response:

   - Response example:

     ```json
     {
       "isPhishing": false,
       "score": 0.12,
       "reason": "dummy"
     }
     ```

   - For the MVP, the backend can simply:

     - Randomly return phishing true or false.
     - Or use a simple rule based on app name or timer.

5. Main process logic:

   - For each frame response, update a simple state in the main process.
   - When `isPhishing` changes from false to true, emit an IPC event to the overlay renderer: `phishingAlert`.
   - When a certain cooldown passes without `isPhishing = true`, emit `clearAlert`.

---

## 6. Phishing detection logic (dummy)

**Goal:** Provide realistic structure, without real ML.

**Dummy behavior proposal:**

- Maintain internal state in the main process:

  - `lastAlertTime`.

- On each backend response:

  - If current time minus `lastAlertTime` is greater than a minimum gap, and a simple condition is met (for example a random threshold), then:

    - Consider it a phishing event and update `lastAlertTime`.

- This simulates sporadic alerts without overwhelming the user.

This logic can be entirely inside the main process and independent of the backend, or the backend can return `isPhishing` as random. For now, either approach is fine. What matters is that the main process emits clear events to the renderer.

---

## 7. Cursor tracking and panel positioning

**Requirements:**

- The panel should appear slightly below and to the right of the cursor.
- It should follow the cursor while the alert is visible.

**Plan:**

1. The renderer listens for cursor position updates.
2. Cursor position source:

   - Use a Node module that can read the global cursor position on macOS.
   - Expose this via IPC or preload to the renderer.

3. Update loop:

   - Use a small interval, for example 30 to 60 times per second.
   - On each tick:

     - Read cursor position `{x, y}`.
     - If an alert is active:

       - Position the panel with a small offset. Example: `panelX = x + 12`, `panelY = y + 16`.

4. Boundaries:

   - Clamp the panel position to stay fully within the screen.
   - If the cursor is near the bottom, move the panel above the cursor instead.

---

## 8. UI design for the panel

Style direction: minimal, clean, visually clear but not intrusive.

**Panel components:**

- Container:

  - Rounded rectangle.
  - Slight drop shadow.
  - Background: solid dark or light tone with high contrast against text.

- Contents:

  - Optional small icon on the left (warning icon).
  - Title text:

    - Example: “Possible phishing attempt”.
    - Bold, slightly larger font.

  - Subtitle text:

    - Example: “Look carefully before entering credentials”.
    - Smaller, regular font.

- Size:

  - Width: around 260 - 320 px.
  - Height: dynamic based on content, but compact.

- Animations:

  - Simple fade in and fade out.
  - No heavy animations for MVP.

**Interaction:**

- Click panel:

  - For MVP: dismiss the alert.
  - Optionally log a console message for future extension.

---

## 9. App lifecycle and states

**States in the main process:**

- `idle`
  App started, but not capturing yet. Used for initial permissions and onboarding.

- `capturing`
  Screen capture loop running. Frames being sent to backend. No active alert.

- `alert_active`
  At least one alert is active. The overlay window shows the warning panel.

**Transitions:**

1. Startup:

   - Initialize and show tray icon.
   - Ask for permission on first capture attempt.
   - Move to `capturing`.

2. While capturing:

   - Receive responses, evaluate dummy phishing logic.
   - If phishing event: emit `phishingAlert` to overlay and move to `alert_active`.

3. While alert is active:

   - Keep sending frames.
   - If no new phishing event appears for a certain time window, emit `clearAlert` and move back to `capturing`.

4. Quit:

   - From tray menu or macOS standard ways.

---

## 10. IPC contract between main and renderer

The contract should be simple and explicit.

**From main to renderer (overlay):**

- `phishingAlert`

  - Payload:

    ```json
    {
      "message": "Possible phishing attempt",
      "details": "Dummy detection, for now",
      "severity": "warning"
    }
    ```

- `clearAlert`

  - No payload or minimal payload.

**From renderer to main:**

- `alertDismissed`

  - Sent when user clicks the panel, to allow the main process to keep track, although it does not change the detection logic.

These events are exposed via the preload script, for example:

```js
// In preload
contextBridge.exposeInMainWorld("api", {
  onPhishingAlert: (callback) =>
    ipcRenderer.on("phishingAlert", (_event, data) => callback(data)),
  onClearAlert: (callback) => ipcRenderer.on("clearAlert", callback),
  alertDismissed: () => ipcRenderer.send("alertDismissed"),
});
```

(The exact implementation can be left to the coding LLM, the interface is what matters.)

---

## 11. Project structure outline

Suggested structure, to give the coding LLM a clear map:

- `package.json`

  - Scripts for `dev`, `build`, and `start`.

- `src/main/`

  - `main.ts` or `main.js`
    Entry point. Creates windows, sets up IPC, starts screen capture loop.
  - `capture.ts`
    Handles screen capture, frame encoding, and sending to backend.
  - `phishingLogic.ts`
    Dummy detection logic and state.
  - `ipc.ts`
    Sets up channels for communication with overlay.

- `src/preload/`

  - `overlayPreload.ts`
    Exposes IPC helpers to renderer.

- `src/renderer/overlay/`

  - `index.html`
  - `overlay.tsx` or `overlay.js`
    Root renderer code.
  - `Panel.tsx`
    Visual component for the warning panel.
  - `styles.css` or styled components.

---

## 12. Future extension points (for later phases)

Not for implementation now, but important to keep in mind while structuring:

- Replace dummy backend with a real phishing detection service.
- Use the active app and URL (where possible) as input for detection.
- Multi monitor support.
- Settings window with:

  - Sensitivity.
  - Pause or disable.
  - Logging.

The current MVP should be structured so the only part that changes later is the backend response and phishing logic module, while the overlay and UX remain stable.
