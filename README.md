# PC Stats Monitor 🖥️📱

Turn your Android phone into a dedicated, real-time hardware monitoring dashboard for your Windows PC! This system streams live CPU/GPU load, temperatures, clocks, power, and RAM metrics directly to your device via a secure, ultra-low latency USB connection.

The entire system operates as an autonomous **Zero-Touch Automation Engine**. Once configured, you never need to touch your phone, tap any interfaces, or manage active windows — the software syncs completely with your PC's power and lock states.

---

## 📸 Screenshots & Media Gallery

| Mobile Display UI | Desktop Control Panel |
| :---: | :---: |
| ![Mobile UI](./assets/app_screenshot.jpg) | ![Desktop UI](./assets/Windows_app.png) |

---

## 🚀 Step 1: Download the Packages

No external web pages or installation wizards are required. Click the links below to **directly download** the production packages immediately to your device:

- 📱 **[Download Mobile App Package (.APK.ZIP)](./assets/pc_stats_monitor.apk.zip?raw=true)** — *Extract and install on your Android phone*
- 💻 **[Download Desktop Agent Package (.ZIP)](./assets/DesktopStatsSender.zip?raw=true)** — *Extract and run on your Windows PC*

---

## 📱 Step 2: Android Phone Configuration (One-Time Setup)

Because this application bridges low-level hardware data directly over USB, your phone needs minor permission adjustments to establish the communication tunnel.

### 1. Enable Developer Mode & USB Debugging

1. Open your smartphone's **Settings** menu and navigate to **About Phone** (*System Info*).
2. Locate the **Build Number** (*Derleme Numarası*) and tap it continuously **7 times** until a prompt appears stating: *"You are now a developer!"*
3. Go back to the main Settings screen, search for **Developer Options** (*Geliştirici Seçenekleri*), enter the menu, and switch **ON** the **USB Debugging** (*USB Hata Ayıklama*) toggle.

### 2. Install and Trust the Mobile App

1. Extract the downloaded `pc_stats_monitor.apk.zip` and transfer the `.apk` file to your phone (or extract it directly on the device).
2. Tap the file to initiate the package installer.
3. **Security exemption pop-up:** Android will trigger a *"Blocked by Play Protect"* or *"Unknown App"* warning because the binary is compiled outside the official Google Play Store ecosystem.
4. Tap **"More details"** (*Daha fazla detay*) on the pop-up warning, then tap **"Install anyway"** (*Yine de yükle*).
5. Open the app once, connect your device to the PC using a data-capable USB cable, and check the box for *"Always allow USB debugging from this computer"* when prompted.

---

## 💻 Step 3: Windows PC Setup & Set-and-Forget

### 1. Extract the Distribution Layout

Extract the contents of `DesktopStatsSender.zip` into a secure directory of your choice on your local machine.

> **⚠️ The Infrastructure Rule:** You'll notice a `platform-tools` directory sitting adjacent to the main execution binary `PCStatsSender.exe`. **Do not move, rename, or separate the `platform-tools` folder from the `.exe`!** The desktop agent requires this structural layout to spawn child ADB processes.

```text
📂 PC_Stats_Sender_Distribution/
├── 📂 platform-tools/       <- Native Android SDK binaries (do not modify!)
└── 📄 PCStatsSender.exe     <- Unified core engine binary
```

### 2. Activate Zero-Touch Background Boot

1. Double-click **`PCStatsSender.exe`** on your PC to open the local interface window.
2. Check the **"Run on Startup"** (*Başlangıçta Çalıştır*) checkbox in the control panel UI.
3. Close the window. The application will gracefully hide itself into your Windows system tray (near the system clock) and run quietly in the background.

---

## 🔄 The Zero-Touch Automation Engine

Once initialized, **you can put your phone on a desk mount and completely forget about it.** The background services handle the entire lifecycle seamlessly, with no manual interaction required:

- **PC Startup / Reboot** — The Windows agent boots silently in the background, maps local port forwarding automatically, wakes the connected phone's screen, overrides ambient device locks, pushes the mobile app to the foreground, and starts real-time telemetry streaming instantly.
- **Workstation Lock (`Win + L`)** — Locking your Windows workstation automatically sends an ADB signal to put the connected phone's screen to sleep, conserving battery. Unlocking your user profile immediately wakes the mobile device and resumes rendering.
- **PC Shutdown / Low-Power Sleep** — The moment the host PC shuts down or enters sleep mode, the telemetry stream stops. The mobile app's built-in **5-second lease rule** detects the data timeout, disables active screen wake-locks, and lets your Android device dim and turn off its screen naturally.

---

## 🔧 Diagnostics & Automated Self-Healing

- **Frozen gauges / connection lost:** Make sure your USB cable supports high-speed data transfer (charge-only cables will not work). To force a manual reconnect without restarting your system, right-click the app icon in your Windows system tray and select **"Reconnect."**
- **Missing `platform-tools` folder:** If this dependency folder is deleted or corrupted, the app will show a desktop warning balloon. Open the PC interface and click **"Repair ADB Tools."** The software will asynchronously pull fresh official binaries from Google's servers, unpack them, and patch its environment automatically — no user interaction required.