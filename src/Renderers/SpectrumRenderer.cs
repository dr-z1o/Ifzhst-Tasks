using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaDummyProject.Extensions;

namespace AvaloniaDummyProject.Renderers;

public class SpectrumRenderer
{
    WriteableBitmap _bitmap;
    private const int Width = 1024;
    private const int Height = 200;
    public SpectrumRenderer()
    {
        _bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888);
    }

    public Color BackgroundColor { get; set; } = Colors.Black;
    public Color LineColor { get; set; } = Colors.Lime;

    public Color PointerColor { get; set; } = Colors.White;

    public WriteableBitmap GetBitmap() => _bitmap;
    
    public WriteableBitmap Render(double[] data, int pointerX = -1)
    {
        using var fb = _bitmap.Lock();

        // for (int x = 1; x < data.Length; x++)
        // {
        //     int y1 = (int)((data[x - 1] + 120) / 100.0 * 200);
        //     int y2 = (int)((data[x] + 120) / 100.0 * 200);
        //     y1 = 200 - y1;
        //     y2 = 200 - y2;
        //     fb.DrawLine(x - 1, y1, x, y2, LineColor);
        // }

        unsafe
        {
            // Line Color is used for the spectrum lines
            byte r = LineColor.R, g = LineColor.G, b = LineColor.B, a = LineColor.A;

            Span<byte> buffer = new((void*)fb.Address, fb.RowBytes * fb.Size.Height);
            buffer.Clear(); // Clear the buffer

            for (int x = 1; x < data.Length; x++)
            {
                int y1 = Height - (int)((data[x - 1] + 120) / 100.0 * Height);
                int y2 = Height - (int)((data[x] + 120) / 100.0 * Height);
                DrawLine(buffer, fb.RowBytes, x - 1, y1, x, y2, r, g, b, a);
            }

            //Draw the pointer line if pointerX is provided
            if (pointerX >= 0 && pointerX < Width)
            {
                int y0 = 0, y1 = Height - 1;
                DrawLine(buffer, fb.RowBytes, pointerX, y0, pointerX, y1, PointerColor);
            }
        }
        
        return _bitmap;
    }

    private void DrawLine(Span<byte> buffer, int stride, int x0, int y0, int x1, int y1, Color? color = null)
    {
        color ??= LineColor; // Default color if none provided
        byte r = color.Value.R, g = color.Value.G, b = color.Value.B, a = color.Value.A;
        DrawLine(buffer, stride, x0, y0, x1, y1, r, g, b, a);
    }
    private static void DrawLine(Span<byte> buffer, int stride, int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte a)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            if (x0 >= 0 && x0 < Width && y0 >= 0 && y0 < Height)
            {
                int index = y0 * stride + x0 * 4;
                buffer[index + 0] = b;//0;
                buffer[index + 1] = g;//255;
                buffer[index + 2] = r;//0;
                buffer[index + 3] = a;//255;
            }

            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}