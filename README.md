# PC Stats Monitor

Turn an old Android phone into a dedicated, always-on hardware monitor for your Windows PC. A lightweight tray agent reads CPU/GPU/RAM sensors and streams them over USB to a Flutter app running on the phone — no Wi-Fi, no IP configuration, just a cable.

<p align="center">
  <img src="docs/screenshots/dashboard.jpg" width="700" alt="PC Stats Monitor dashboard" />
</p>

---

## ⬇️ Download

| File | Description |
|---|---|
| [**DesktopStatsSender.zip**](https://github.com/MandalorianV/pc_stats_monitor/releases/download/v1.0.0/DesktopStatsSender.zip) | Windows tray agent — runs on your PC |
| [**pc_stats_monitor.zip**](https://github.com/MandalorianV/pc_stats_monitor/releases/download/v1.0.0/pc_stats_monitor.zip) | Flutter source — build and install on the phone |

> **Requirement:** [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) must be installed on the Windows PC to run the agent.

---

## How it works

```
┌──────────────────────┐      USB (ADB port-forward)      ┌──────────────────────┐
│      Windows PC       │  ─────────────────────────────►  │     Android phone      │
│                        │      TCP · JSON · 800ms          │                        │
│   DesktopStatsSender   │                                   │     Flutter app        │
│   (system tray agent)  │  ◄─────────────────────────────  │  (full-screen gauges)  │
└──────────────────────┘      ADB wake/sleep commands       └──────────────────────┘
```

- The **Windows agent** runs in the system tray, reads sensors via LibreHardwareMonitor, and streams JSON over TCP forwarded through ADB.
- The **Flutter app** listens on that socket and renders live data as animated AMOLED-black gauges.
- The agent wakes/sleeps the phone automatically on PC lock/unlock and shutdown.

---

## Features

- 🖥️ **Live monitoring** — CPU & GPU load, temperature, clock speed, power draw, VRAM; RAM usage in GB; total system power
- 🌑 **True-black AMOLED UI** — three-column layout (CPU / Total Power & RAM / GPU), gauges that shift green → red as load increases
- 🔌 **Plug-and-play over USB** — ADB port-forward, no IP setup needed
- 🔋 **Self-healing connection** — auto-reconnect on cable unplug/replug; adb server restart after repeated failures
- 🛠️ **Self-repairing ADB** — if `platform-tools` is missing, the agent detects it and re-downloads with one click
- 😴 **Smart sleep** — phone screen off on PC lock/shutdown/30 s idle, back on when PC unlocks
- 🚀 **Start with Windows** — checkbox in the tray window, no Task Scheduler needed
- 🧹 **Clean UI** — Reconnect and Repair buttons only appear when there's actually a problem

---

## Setup

### 1. Windows agent

1. Install [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
2. Download and extract [**DesktopStatsSender.zip**](https://github.com/MandalorianV/pc_stats_monitor/releases/download/v1.0.0/DesktopStatsSender.zip) to any folder (e.g. `C:\PcStatsAgent\`).
3. Run `Monitor.Worker.exe` **as Administrator** (right-click → Run as administrator). Required for hardware sensor access.
   - The app starts hidden in the system tray — look for the icon in the tray overflow (^ arrow near the clock).
   - If `platform-tools` is missing, a notification pops up and the **Repair ADB Tools** button downloads it automatically.
4. Double-click the tray icon to open the window. Check **"Start with Windows"** to launch automatically on every boot — no extra setup needed.

### 2. Android phone

1. **Enable Developer Options:** Settings → About phone → tap *Build number* 7 times.
2. **Enable USB debugging:** Settings → Developer options → USB debugging → ON.
3. Connect the phone via USB and accept the *"Allow USB debugging?"* prompt. Check **"Always allow from this computer"**.
4. Install the Flutter app — choose one of:
   - **Build from source** (requires [Flutter SDK](https://flutter.dev)):
     ```bash
     cd flutter_app
     flutter pub get
     flutter run --release
     ```
   - **Install the APK** from [Releases](https://github.com/MandalorianV/pc_stats_monitor/releases/tag/v1.0.0) (if an APK is provided).
5. Open the app on the phone. It locks to landscape and stays awake while data is flowing.

### 3. First-run checklist

- [ ] Agent running in the tray (`Monitor.Worker.exe` visible in Task Manager)
- [ ] Phone connected via USB with debugging authorized
- [ ] Flutter app open on the phone
- [ ] Tray window shows **"Connected (phone)"** in green ✅

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `ADB not found` notification on startup | Click **Repair ADB Tools** in the tray window — downloads `platform-tools` automatically |
| Phone shows **"WAITING FOR PC"** indefinitely | Open tray window → click **Reconnect** |
| Unplugging/replugging the cable doesn't reconnect | Click **Reconnect** — this restarts the local adb server which can get stuck after a physical disconnect. The agent also does this automatically after ~5 failed attempts. |
| All sensor values show `0` | Run the agent **as Administrator** — LibreHardwareMonitor needs elevated rights to read temperature and power sensors |
| Phone screen stays on after PC shutdown | Check the phone's own screen timeout — set it to 30–60 s for fastest sleep after disconnect |

---

## Repository structure

```
PcStatsMonitor/
├── flutter_app/         Android app (Flutter) — the on-phone dashboard
├── stats_sender_dotnet/  Windows tray agent (.NET solution)
│   ├── Monitor.Core/      Shared DTOs and source generation context
│   ├── Monitor.Hardware/  LibreHardwareMonitor sensor reading service
│   ├── Monitor.Network/   TCP streaming, ADB control, self-repair
│   └── Monitor.Worker/    WinForms tray UI + background worker (entry point)
└── docs/screenshots/     Images used in this README
```

---

## Building from source

### Windows agent
```powershell
cd stats_sender_dotnet/Monitor.Worker
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```
Output: `./publish/Monitor.Worker.exe` (~2.5 MB, requires .NET 10 Desktop Runtime)

### Flutter app
```bash
cd flutter_app
flutter pub get
flutter run --release        # installs directly to connected phone
# or
flutter build apk --release  # produces build/app/outputs/flutter-apk/app-release.apk
```

---

## License

MIT — free to fork, modify, and use in your own setup.