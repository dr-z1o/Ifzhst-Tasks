using System;
using Avalonia.Media;

namespace AvaloniaDummyProject.Extensions;

public static class GradientMapper
{
    private static readonly Color[] Stops =
    [
        Color.FromArgb(255, 0, 0, 255),     // 0.0
        Color.FromArgb(255, 0, 255, 255),   // 0.25
        Color.FromArgb(255, 0, 255, 0),     // 0.5
        Color.FromArgb(255, 255, 255, 0),   // 0.75
        Color.FromArgb(255, 255, 0, 0)      // 1.0
    ];

    public static Color Map(double value)
    {
        double norm = Math.Clamp((value + 120) / 100.0, 0, 1);
        double scaled = norm * (Stops.Length - 1);
        int index = (int)scaled;
        double t = scaled - index;
        if (index >= Stops.Length - 1) return Stops[^1];

        Color c1 = Stops[index];
        Color c2 = Stops[index + 1];

        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);

        return Color.FromRgb(r, g, b);
    }
}