using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Platform;

namespace AvaloniaDummyProject.Extensions;

public static class FramebufferExtensions
{
    public static void SetPixel(this ILockedFramebuffer fb, int x, int y, Color color)
    {
        if (x < 0 || x >= fb.Size.Width || y < 0 || y >= fb.Size.Height)
            return;

        IntPtr address = fb.Address + y * fb.RowBytes + x * 4;
        byte[] pixel = { color.B, color.G, color.R, color.A }; // BGRA format
        Marshal.Copy(pixel, 0, address, 4);
    }

    public static void Clear(this ILockedFramebuffer fb, Color color)
    {
        for (int y = 0; y < fb.Size.Height; y++)
        {
            for (int x = 0; x < fb.Size.Width; x++)
            {
                fb.SetPixel(x, y, color);
            }
        }
    }

    public static void DrawLine(this ILockedFramebuffer fb, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            fb.SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}