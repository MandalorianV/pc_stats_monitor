class PcStatsDto {
  final double cpuLoad;
  final double cpuTemp;
  final double cpuClock;
  final double cpuWatt;
  final double gpuLoad;
  final double gpuTemp;
  final double gpuClock;
  final double gpuWatt;
  final double gpuVramUsedMb;
  final double ramUsage;
  final double ramUsedGb;
  final double ramTotalGb;
  final double totalWatt;

  const PcStatsDto({
    this.cpuLoad = 0,
    this.cpuTemp = 0,
    this.cpuClock = 0,
    this.cpuWatt = 0,
    this.gpuLoad = 0,
    this.gpuTemp = 0,
    this.gpuClock = 0,
    this.gpuWatt = 0,
    this.gpuVramUsedMb = 0,
    this.ramUsage = 0,
    this.ramUsedGb = 0,
    this.ramTotalGb = 0,
    this.totalWatt = 0,
  });

  factory PcStatsDto.fromJson(Map<String, dynamic> json) {
    return PcStatsDto(
      cpuLoad: _toDouble(json['CpuLoad']),
      cpuTemp: _toDouble(json['CpuTemp']),
      cpuClock: _toDouble(json['CpuClock']),
      cpuWatt: _toDouble(json['CpuWatt']),
      gpuLoad: _toDouble(json['GpuLoad']),
      gpuTemp: _toDouble(json['GpuTemp']),
      gpuClock: _toDouble(json['GpuClock']),
      gpuWatt: _toDouble(json['GpuWatt']),
      gpuVramUsedMb: _toDouble(json['GpuVramUsedMb']),
      ramUsage: _toDouble(json['RamUsage']),
      ramUsedGb: _toDouble(json['RamUsedGb']),
      ramTotalGb: _toDouble(json['RamTotalGb']),
      totalWatt: _toDouble(json['TotalWatt']),
    );
  }

  static double _toDouble(dynamic v) {
    if (v == null) return 0;
    if (v is num) return v.toDouble();
    return double.tryParse(v.toString()) ?? 0;
  }
}
