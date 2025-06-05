using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaDummyProject.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaDummyProject.Renderers;

public class WaterfallRenderer
{
    private readonly Queue<double[]> _history = new();
    private const int MaxLines = 200;

    public Color BackgroundColor { get; set; } = Colors.Black;

    public WriteableBitmap AddAndRender(double[] data)
    {
        if (_history.Count >= MaxLines)
            _history.Dequeue();
        _history.Enqueue(data);

        var bmp = new WriteableBitmap(new PixelSize(1024, MaxLines), new Vector(96, 96), PixelFormat.Bgra8888);
        using var fb = bmp.Lock();
        fb.Clear(BackgroundColor);

        int y = 0;
        foreach (var row in _history.Reverse())
        {
            for (int x = 0; x < row.Length; x++)
            {
                var color = GradientMapper.Map(row[x]);
                fb.SetPixel(x, y, color);
            }
            y++;
        }

        return bmp;
    }
}