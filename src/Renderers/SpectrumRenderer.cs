using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaDummyProject.Extensions;

namespace AvaloniaDummyProject.Renderers;

public class SpectrumRenderer
{
    public Color BackgroundColor { get; set; } = Colors.Black;
    public Color LineColor { get; set; } = Colors.Lime;

    public Color PointerColor { get; set; } = Colors.White;
    public WriteableBitmap Render(double[] data)
    {
        var bmp = new WriteableBitmap(new PixelSize(1024, 200), new Vector(96, 96), PixelFormat.Bgra8888);
        using var fb = bmp.Lock();
        fb.Clear(BackgroundColor);
        for (int x = 1; x < data.Length; x++)
        {
            int y1 = (int)((data[x - 1] + 120) / 100.0 * 200);
            int y2 = (int)((data[x] + 120) / 100.0 * 200);
            y1 = 200 - y1;
            y2 = 200 - y2;
            fb.DrawLine(x - 1, y1, x, y2, LineColor);
        }
        return bmp;
    }
}