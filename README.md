# PC Hardware Monitor Dashboard (Flutter Client)

A lightweight, performance-focused Flutter application designed to transform an old Android device into a dedicated real-time hardware monitoring client for your PC setup. Optimized specifically for **AMOLED** displays to save energy and protect screen health.

## 🚀 Key Features

- **MVVM Architecture:** Clean separation of concerns using highly performant `Stateful + ViewMixin` layer.
- **Zero Invalidation Overhead:** Replaced legacy `setState` mechanics with granular `ValueNotifier` hooks. Only affected components re-render.
- **Embedded Socket Server:** Listens directly on port `5000` for sat-level raw TCP payloads.
- **AMOLED Optimization:** Strict True Black (`#000000`) canvas context minimizing active pixel counts.
- **Wakelock Protection:** Keeps screen alive during live data stream, gracefully shifting down back to system sleep intervals if the telemetry disconnects for more than 30s.

## 📦 Required Dependencies

Add this under dependencies block in your `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  wakelock_plus: ^1.2.8