import 'package:flutter/material.dart';
import '../constants/app_colors.dart';
import 'load_gauge.dart';

class CoreCard extends StatelessWidget {
  final String title;
  final Color accentColor;
  final double loadValue;
  final double tempValue;
  final double clockValue;
  final double wattValue;
  final double? vramValue;

  const CoreCard({
    super.key,
    required this.title,
    required this.accentColor,
    required this.loadValue,
    required this.tempValue,
    required this.clockValue,
    required this.wattValue,
    this.vramValue,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 18),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(22),
        border:
            Border.all(color: accentColor.withValues(alpha: 0.25), width: 1),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            title,
            style: TextStyle(
              color: accentColor,
              fontSize: 18,
              fontWeight: FontWeight.w900,
              letterSpacing: 3,
            ),
          ),
          Expanded(
            child: Center(
              child: LoadGauge(
                loadValue: loadValue,
                tempValue: tempValue,
                color: accentColor,
              ),
            ),
          ),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              _MiniDetail(
                  icon: Icons.speed,
                  value: '${clockValue.toStringAsFixed(0)} MHz',
                  color: accentColor),
              _MiniDetail(
                  icon: Icons.bolt,
                  value: '${wattValue.toStringAsFixed(0)} W',
                  color: accentColor),
              if (vramValue != null)
                _MiniDetail(
                    icon: Icons.memory,
                    value: 'VRAM ${(vramValue! / 1024).toStringAsFixed(1)} GB',
                    color: accentColor),
            ],
          ),
        ],
      ),
    );
  }
}

class _MiniDetail extends StatelessWidget {
  final IconData icon;
  final String value;
  final Color color;

  const _MiniDetail(
      {required this.icon, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color.withValues(alpha: 0.85)),
        const SizedBox(width: 6),
        Text(
          value,
          style: const TextStyle(
              color: Colors.white70, fontSize: 12, fontWeight: FontWeight.w600),
        ),
      ],
    );
  }
}
