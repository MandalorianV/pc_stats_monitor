# PC Stats Monitor

Turn an old Android phone into a dedicated, always-on hardware monitor for your gaming PC. A lightweight Windows tray agent reads your CPU/GPU sensors and streams them over USB to a Flutter app running on the phone, which displays them on a full-screen AMOLED-friendly dashboard.

<p align="center">
  <img src="docs/screenshots/dashboard_overview.jpg" width="700" alt="Dashboard running on the phone, mounted next to the PC" />
</p>

## Overview

```
┌──────────────────────┐        USB (ADB port-forward)        ┌──────────────────────┐
│      Windows PC       │  ───────────────────────────────►   │     Android phone      │
│                        │        TCP, JSON, every 800ms        │                        │
│   Monitor.Worker.exe   │                                       │     Flutter app        │
│  (system tray agent)   │  ◄───────────────────────────────   │  (full-screen gauges)  │
└──────────────────────┘        ADB wake/sleep commands         └──────────────────────┘
```

- The **Windows agent** runs silently in the system tray, reads CPU/GPU/RAM sensors via LibreHardwareMonitor, and streams them as JSON over a TCP socket forwarded through ADB.
- The **Flutter app** runs full-screen and landscape-locked on the phone, listens on that socket, and renders the live data as animated AMOLED-black gauges.
- The agent also wakes/sleeps the phone screen automatically based on PC lock state and connection activity, so the phone behaves like a real always-on display.

## Features

- 🖥️ **Live CPU & GPU monitoring** — load, temperature, clock speed, power draw, VRAM usage
- 🌑 **True-black AMOLED UI** — three-column layout (CPU / Total Power & RAM / GPU), color-coded gauges that shift from green to red as load increases
- 🔌 **Plug-and-play over USB** — uses `adb forward`, no need to find the phone's IP address or set up WiFi/tethering
- 🔋 **Self-healing connection** — automatic reconnect on cable unplug/replug, including an adb-server restart after repeated failures, plus a manual "Reconnect" button for instant recovery
- 🛠️ **Self-repairing ADB** — if the `platform-tools` folder goes missing, the agent detects it, shows a tray notification, and re-downloads it with a single click — no manual setup required at all
- 😴 **Smart sleep behavior** — phone screen turns off automatically when the PC is locked, shut down, or idle for 30+ seconds without data
- 🚀 **Starts automatically** — agent launches hidden in the tray on Windows startup; phone app wakes itself and comes to the foreground when the PC connects
- 🧹 **Buttons only appear when needed** — "Reconnect" and "Repair ADB Tools" stay hidden unless there's an actual problem to fix

## Screenshots

| Dashboard | Windows Tray Agent |
|---|---|
| ![Dashboard](docs/screenshots/dashboard_overview.jpg) | ![Tray agent](docs/screenshots/tray_agent.png) |

> Add your own screenshots to `docs/screenshots/` — see [Adding screenshots](#adding-screenshots) below.

## Repository structure

```
PcStatsMonitor/
├── flutter_app/         Android app (Flutter) — the on-phone dashboard
├── windows_agent/        Windows tray agent (.NET) — reads sensors, streams data
│   ├── Monitor.Core/      Shared DTOs and models
│   ├── Monitor.Hardware/  LibreHardwareMonitor sensor reading
│   ├── Monitor.Network/   TCP streaming + ADB control + self-repair
│   └── Monitor.Worker/    Tray UI (WinForms) + background worker entry point
└── docs/screenshots/     Images used in this README
```

## Getting started

### 1. Windows agent

**Requirements:** [.NET SDK](https://dotnet.microsoft.com/download) (the project targets `net10.0-windows`), Windows 10/11.

1. Clone the repo and build:
   ```powershell
   cd windows_agent/Monitor.Worker
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
   ```
2. Copy the contents of `./publish` to wherever you want to keep the agent (e.g. `C:\PcStatsAgent\`).
3. Run `Monitor.Worker.exe` once manually **as Administrator** (required for hardware sensor access via LibreHardwareMonitor). It starts hidden in the system tray — look for its icon in the tray overflow area.
   - If `adb.exe` isn't found, you'll get a tray notification and the window will pop up with a **Repair ADB Tools** button — click it and the agent downloads the latest `platform-tools` automatically. No manual ADB install needed.
4. **Run it automatically on every Windows startup:**
   - Open Task Scheduler (`taskschd.msc`) → Create Task (not "Basic Task")
   - General tab: check **"Run with highest privileges"** (needed for sensor access)
   - Triggers tab: New → **At log on**
   - Actions tab: Start a program → point it directly at `Monitor.Worker.exe`
   - Save

No `.bat` or `.vbs` wrapper scripts are needed — the agent handles the ADB port forward and device detection internally.

### 2. Flutter app (phone)

**Requirements:** [Flutter SDK](https://flutter.dev), an Android phone, a USB cable.

1. On the phone: enable Developer Options (tap Build Number 7 times in *Settings → About phone*), then enable **USB debugging**.
2. Connect the phone via USB, accept the "Allow USB debugging?" prompt, and check the **"Always allow from this computer"** box.
3. Build and install:
   ```bash
   cd flutter_app
   flutter pub get
   flutter run --release
   ```
4. Mount the phone next to your PC in landscape orientation. The app locks to landscape, goes full-screen, and stays awake automatically while data is flowing — it sleeps on its own after ~30 seconds without data or when the PC locks/shuts down.

### 3. First run checklist

- [ ] Windows agent running in the tray (check Task Manager for `Monitor.Worker.exe` if you don't see the tray icon)
- [ ] Phone connected via USB with debugging authorized
- [ ] Flutter app installed and open
- [ ] Tray window shows "Connected (phone)" in green — if not, see [Troubleshooting](#troubleshooting)

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tray window shows "ADB not found" | Click **Repair ADB Tools** — downloads `platform-tools` automatically |
| Phone shows "WAITING FOR PC" indefinitely | Open the tray window and click **Reconnect** (only visible while disconnected) |
| Unplugging/replugging the cable doesn't reconnect | Click **Reconnect** — this restarts the local adb server, which clears a stale device state that a plain retry can't fix. The agent also does this automatically after a few failed attempts. |
| Sensor values stuck at 0 | Make sure the agent is running **as Administrator** — LibreHardwareMonitor needs elevated privileges to read temperature/power sensors |
| Phone screen stays off even with PC on | Check the phone's own screen timeout setting — the agent wakes it on connect, but very short OS-level timeouts can still kick in between updates |

## Adding screenshots

1. Take photos/screenshots of the phone dashboard and the Windows tray window.
2. Drop them into `docs/screenshots/` (e.g. `dashboard_overview.jpg`, `tray_agent.png`).
3. Reference them in this README using `![Alt text](docs/screenshots/filename.png)`.

## License

MIT — feel free to fork and adapt for your own setup.