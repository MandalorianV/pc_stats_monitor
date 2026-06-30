import 'dart:math' show pi;
import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class LoadGauge extends StatelessWidget {
  final double loadValue;
  final double tempValue;
  final Color color;

  const LoadGauge({
    super.key,
    required this.loadValue,
    required this.tempValue,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    final fraction = (loadValue / 100).clamp(0.0, 1.0);

    return LayoutBuilder(
      builder: (context, constraints) {
        final maxH = constraints.maxHeight.isFinite
            ? constraints.maxHeight
            : constraints.maxWidth;
        final dim = constraints.maxWidth < maxH ? constraints.maxWidth : maxH;
        final size = dim.clamp(120.0, 200.0);

        return TweenAnimationBuilder<double>(
          tween: Tween<double>(begin: 0, end: fraction),
          duration: const Duration(milliseconds: 600),
          curve: Curves.easeOutCubic,
          builder: (context, animatedFraction, child) {
            return SizedBox(
              width: size,
              height: size,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  Container(
                    width: size,
                    height: size,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: color.withValues(alpha: 0.30),
                          blurRadius: 32,
                          spreadRadius: 1,
                        ),
                      ],
                    ),
                  ),
                  CustomPaint(
                    size: Size(size, size),
                    painter: _ArcGaugePainter(
                      fraction: animatedFraction,
                      color: color,
                      trackColor: AppColors.trackBackground,
                      strokeWidth: size * 0.10,
                      sweepDegrees: 270,
                    ),
                  ),
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        '${loadValue.toStringAsFixed(0)}%',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: size * 0.20,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        'LOAD',
                        style: TextStyle(
                          color: Colors.white38,
                          fontWeight: FontWeight.w600,
                          fontSize: size * 0.06,
                          letterSpacing: 1.5,
                        ),
                      ),
                      SizedBox(height: size * 0.045),
                      Text(
                        '${tempValue.toStringAsFixed(0)}°',
                        style: TextStyle(
                          color: color,
                          fontSize: size * 0.135,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _ArcGaugePainter extends CustomPainter {
  final double fraction;
  final Color color;
  final Color trackColor;
  final double strokeWidth;
  final double sweepDegrees;

  _ArcGaugePainter({
    required this.fraction,
    required this.color,
    required this.trackColor,
    required this.strokeWidth,
    this.sweepDegrees = 270,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final rect = Rect.fromLTWH(
      strokeWidth / 2,
      strokeWidth / 2,
      size.width - strokeWidth,
      size.height - strokeWidth,
    );

    final gapDegrees = 360 - sweepDegrees;
    final startDegrees = 90 + gapDegrees / 2;
    final startAngle = startDegrees * pi / 180;
    final totalSweepAngle = sweepDegrees * pi / 180;

    final trackPaint = Paint()
      ..color = trackColor
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(rect, startAngle, totalSweepAngle, false, trackPaint);

    final progressPaint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(
      rect,
      startAngle,
      totalSweepAngle * fraction.clamp(0.0, 1.0),
      false,
      progressPaint,
    );
  }

  @override
  bool shouldRepaint(covariant _ArcGaugePainter oldDelegate) {
    return oldDelegate.fraction != fraction || oldDelegate.color != color;
  }
}
