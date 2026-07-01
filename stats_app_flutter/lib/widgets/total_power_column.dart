import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class TotalPowerColumn extends StatelessWidget {
  final double totalWatt;
  final double ramUsage;
  final double ramUsedGb;
  final double ramTotalGb;

  const TotalPowerColumn({
    super.key,
    required this.totalWatt,
    required this.ramUsage,
    required this.ramUsedGb,
    required this.ramTotalGb,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(
            color: AppColors.powerAccent.withValues(alpha: 0.25), width: 1),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          const Text(
            'TOTAL WATT',
            style: TextStyle(
              color: AppColors.powerAccent,
              fontSize: 16,
              fontWeight: FontWeight.w900,
              letterSpacing: 2,
            ),
          ),
          Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                totalWatt.toStringAsFixed(0),
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 48,
                  fontWeight: FontWeight.w800,
                  shadows: [
                    Shadow(
                      color: AppColors.powerAccent.withValues(alpha: 0.8),
                      blurRadius: 18,
                    ),
                  ],
                ),
              ),
              const Text(
                'WATT',
                style: TextStyle(
                  color: Colors.white38,
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 2,
                ),
              ),
            ],
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'RAM',
                    style: TextStyle(
                      color: Colors.white38,
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 2,
                    ),
                  ),
                  Text(
                    '${ramUsage.toStringAsFixed(0)}%',
                    style: const TextStyle(
                      color: AppColors.ramAccent,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  Text(
                    '${ramUsedGb.toStringAsFixed(1)} GB / ${ramTotalGb.toStringAsFixed(1)} GB',
                    style: const TextStyle(
                      color: AppColors.ramAccent,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(
                  value: (ramUsage / 100).clamp(0.0, 1.0),
                  minHeight: 10,
                  backgroundColor: AppColors.trackBackground,
                  valueColor:
                      const AlwaysStoppedAnimation<Color>(AppColors.ramAccent),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
