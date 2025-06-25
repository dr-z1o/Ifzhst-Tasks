using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaDummyProject.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvaloniaDummyProject.Renderers;

public class WaterfallRenderer
{
    private WriteableBitmap _bitmap;

    private const int BytesPerPixel = 4;
    public WaterfallRenderer()
    {
        _bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888);
        _history = new double[Height][];
        _headIndex = -1;
    }
    private readonly double[][] _history;
    private int _headIndex;
    private const int Height = 200;
    private const int Width = 1024;

    public Color BackgroundColor { get; set; } = Colors.Black;
    
    public WriteableBitmap GetBitmap() => _bitmap;

    public void AddAndRender(double[] data)
    {
        // push new data to the history, rewriting the oldest entry
        _headIndex = (_headIndex + 1) % Height;
        _history[_headIndex] = data;

        using var fb = _bitmap.Lock();
        //fb.Clear(BackgroundColor);

        // int y = 0;
        // foreach (var row in _history.Reverse())
        // {
        //     for (int x = 0; x < row.Length; x++)
        //     {
        //         var color = GradientMapper.Map(row[x]);
        //         fb.SetPixel(x, y, color);
        //     }
        //     y++;
        // }

        unsafe
        {
            byte* ptr = (byte*)fb.Address;
            int rowBytes = fb.RowBytes;

            // Shift all rows down by one
            // This effectively removes the oldest row (the one at the bottom)
            // and makes space for the new row at the top
            Buffer.MemoryCopy(
                source: ptr,
                destination: ptr + rowBytes,
                destinationSizeInBytes: rowBytes * (Height - 1),
                sourceBytesToCopy: rowBytes * (Height - 1)
            );

            // Draw the newly added row (_headIndex)
            if (_history[_headIndex] != null)
            {
                var currentRow = _history[_headIndex];

                for (int x = 0; x < currentRow.Length; x++)
                {
                    var color = GradientMapper.Map(currentRow[x]);

                    int index = x * 4;
                    ptr[index + 0] = color.B;
                    ptr[index + 1] = color.G;
                    ptr[index + 2] = color.R;
                    ptr[index + 3] = 255;
                }
            }
        }
    }
}