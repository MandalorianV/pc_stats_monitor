import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class ConnectionStatusBar extends StatelessWidget {
  final bool connected;
  const ConnectionStatusBar({super.key, required this.connected});

  @override
  Widget build(BuildContext context) {
    final color = connected ? AppColors.activeGreen : AppColors.inactiveRed;
    final text = connected ? 'PC LINK ACTIVE' : 'WAITING FOR PC';

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(shape: BoxShape.circle, color: color),
        ),
        const SizedBox(width: 8),
        Text(
          text,
          style: TextStyle(
            color: color,
            fontSize: 12,
            fontWeight: FontWeight.w700,
            letterSpacing: 2,
          ),
        ),
      ],
    );
  }
}
