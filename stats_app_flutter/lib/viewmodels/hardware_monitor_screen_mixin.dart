import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:wakelock_plus/wakelock_plus.dart';
import '../models/pc_stats_dto.dart';
import '../views/hardware_monitor_screen.dart';

mixin HardwareMonitorScreenMixin on State<HardwareMonitorScreen>
    implements WidgetsBindingObserver {
  static const int _port = 5000;
  static const Duration _idleSleepDuration = Duration(seconds: 5);

  ServerSocket? _server;
  Socket? _client;
  StreamSubscription<String>? _sub;
  Timer? _idleTimer;
  bool _wakelockActive = true;

  // ValueNotifiers instead of setState
  final ValueNotifier<PcStatsDto> statsNotifier =
      ValueNotifier(const PcStatsDto());
  final ValueNotifier<bool> connectionNotifier = ValueNotifier(false);

  void initViewModel() {
    WidgetsBinding.instance.addObserver(this);
    WakelockPlus.enable();
    _resetIdleTimer();
    _startServer();
  }

  void disposeViewModel() {
    WidgetsBinding.instance.removeObserver(this);
    WakelockPlus.disable();
    _idleTimer?.cancel();
    _sub?.cancel();
    _client?.destroy();
    _server?.close();
    statsNotifier.dispose();
    connectionNotifier.dispose();
  }

  void _resetIdleTimer() {
    _idleTimer?.cancel();
    _idleTimer = Timer(_idleSleepDuration, _onIdleTimeout);

    if (!_wakelockActive) {
      WakelockPlus.enable();
      _wakelockActive = true;
    }
  }

  void _onIdleTimeout() {
    if (_wakelockActive) {
      WakelockPlus.disable();
      _wakelockActive = false;
    }
  }

  Future<void> _startServer() async {
    try {
      _server = await ServerSocket.bind(InternetAddress.anyIPv4, _port);
      _server!.listen(_handleClient, onError: (_) => _restartServer());
    } catch (_) {
      Future.delayed(const Duration(seconds: 3), _startServer);
    }
  }

  void _restartServer() {
    _server?.close();
    Future.delayed(const Duration(seconds: 2), _startServer);
  }

  void _handleClient(Socket socket) {
    _client?.destroy();
    _client = socket;
    connectionNotifier.value = true;

    _sub?.cancel();
    _sub = socket
        .cast<List<int>>()
        .transform(utf8.decoder)
        .transform(const LineSplitter())
        .listen(
          _onLine,
          onDone: _onDisconnected,
          onError: (_) => _onDisconnected(),
          cancelOnError: true,
        );
  }

  void _onLine(String line) {
    _resetIdleTimer();
    if (line.trim().isEmpty) return;
    try {
      final json = jsonDecode(line) as Map<String, dynamic>;
      statsNotifier.value = PcStatsDto.fromJson(json);
    } catch (_) {}
  }

  void _onDisconnected() {
    connectionNotifier.value = false;
    statsNotifier.value = const PcStatsDto();
  }

  // App Lifecycle intercepts to force sticky full screen
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {}
}
