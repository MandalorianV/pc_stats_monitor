# PC Stats Monitor 🖥️📱

Turn your Android phone into a dedicated, real-time hardware monitoring dashboard for your Windows PC! This system streams live CPU/GPU load, temperatures, clocks, power, and RAM metrics directly to your device via a secure, ultra-low latency USB connection.

The entire system operates as an autonomous **Zero-Touch Automation Engine**. Once configured, you never need to touch your phone, tap any interfaces, or manage active windows — the software syncs completely with your PC's power and lock states.

---

## 📸 Screenshots

| Mobile Display UI | Desktop Control Panel |
| :---: | :---: |
| ![Mobile UI](./assets/app_screenshot.jpg) | ![Desktop UI](./assets/Windows_app.png) |

---

## 🚀 Step 1: Download the Packages

Click the links below to **directly download** the production packages:

| Package | Description |
|---|---|
| 📱 [**pc_stats_monitor.zip**](https://github.com/user-attachments/files/29590993/pc_stats_monitor.zip) | Android app — extract and install the APK on your phone |
| 💻 [**DesktopStatsSender.zip**](https://github.com/user-attachments/files/29591599/DesktopStatsSender.zip) | Windows tray agent — extract and run on your PC |

> **Requirement:** [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) must be installed on the Windows PC.

---

## 📱 Step 2: Android Phone Setup (One-Time)

### 1. Enable Developer Mode & USB Debugging

1. Open **Settings → About Phone** and tap **Build Number** 7 times until *"You are now a developer!"* appears.
2. Go back to Settings, find **Developer Options**, and enable **USB Debugging**.

### 2. Install the App

1. Extract `pc_stats_monitor.zip` and transfer the `.apk` to your phone (or extract directly on device).
2. Tap the `.apk` to install. Android may show a *"Blocked by Play Protect"* or *"Unknown App"* warning — tap **More details → Install anyway**.
3. Open the app, connect the phone to your PC via a data-capable USB cable, and accept the *"Allow USB debugging from this computer?"* prompt. Check **"Always allow from this computer"**.

---

## 💻 Step 3: Windows PC Setup

### 1. Extract the Agent

Extract `DesktopStatsSender.zip` to any permanent folder (e.g. `C:\PcStatsAgent\`).

> **⚠️ Important:** If a `platform-tools` folder is missing, don't worry — the agent detects this automatically and offers a **one-click repair** to download it from Google's servers.

```
📂 PcStatsAgent/
├── 📂 platform-tools/    ← ADB binaries (downloaded automatically if missing)
└── 📄 Monitor.Worker.exe ← Main agent
```

### 2. First Run

1. Right-click `Monitor.Worker.exe` → **Run as administrator** (required for hardware sensor access).
2. The app starts hidden in the system tray (look for the icon near the clock — click the `^` arrow if it's hidden).
3. Double-click the tray icon to open the control panel.
4. Check **"Start with Windows"** — the agent will now launch automatically on every boot, no Task Scheduler needed.

---

## 🔄 Zero-Touch Automation

Once set up, **put your phone on a desk mount and forget about it:**

- **PC Startup** — Agent boots silently, sets up ADB port forwarding, wakes the phone, and starts streaming.
- **Win + L (Lock)** — Phone screen turns off automatically to save battery.
- **Unlock** — Phone wakes up and the app comes to the foreground.
- **PC Shutdown** — Telemetry stops; phone screen turns off on its own after ~30 seconds.

---

## 🔧 Troubleshooting

| Symptom | Fix |
|---|---|
| `ADB not found` notification | Open the tray window → click **Repair ADB Tools** — downloads automatically |
| Phone shows **"WAITING FOR PC"** | Open tray window → click **Reconnect** (only visible when disconnected) |
| Unplugging cable doesn't reconnect | Click **Reconnect** — restarts the local ADB server which can get stuck |
| Sensor values stuck at `0` | Run `Monitor.Worker.exe` **as Administrator** — needed for temperature/power sensors |
| Phone screen won't turn off | Set phone's screen timeout to 30–60 s in Android settings |

---

## 🏗️ Building from Source

### Windows Agent
```powershell
cd stats_sender_dotnet/Monitor.Worker
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```
Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Output: `Monitor.Worker.exe` (~2.5 MB)

### Flutter App
```bash
cd flutter_app
flutter pub get
flutter run --release       # installs directly to connected phone
flutter build apk --release # produces an APK file
```
Requires [Flutter SDK](https://flutter.dev).

---

## 📁 Repository Structure

```
pc_stats_monitor/
├── flutter_app/              Android app (Flutter) — the on-phone dashboard
├── stats_sender_dotnet/       Windows tray agent (.NET solution)
│   ├── Monitor.Core/           Shared DTOs
│   ├── Monitor.Hardware/       LibreHardwareMonitor sensor reading
│   ├── Monitor.Network/        TCP streaming, ADB control, self-repair
│   └── Monitor.Worker/         WinForms tray UI + background worker
└── assets/                    Screenshots and release files
```

---

## 📄 License

MIT — free to fork, modify, and use in your own setup.