using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using AvaloniaDummyProject.Extensions;
using AvaloniaDummyProject.Renderers;

namespace AvaloniaDummyProject.ViewModels
{

    public partial class MainWindowViewModel : ObservableObject
    {
        private DispatcherTimer _timer;

        private readonly SignalGenerator _generator = new();
        private readonly SpectrumRenderer _spectrum = new();
        private readonly WaterfallRenderer _waterfall = new();

        [ObservableProperty]
        private WriteableBitmap spectrumImage;

        [ObservableProperty]
        private WriteableBitmap waterfallImage;

        [RelayCommand]
        private void Start()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (s, e) => Update();
            _timer.Start();
        }

        [RelayCommand]
        private void Stop() => _timer?.Stop();

        private void Update()
        {
            var data = _generator.Generate();
            WaterfallImage = _waterfall.AddAndRender(data);

            var tmpSectrum = _spectrum.Render(data);
            if (MousePointer.HasValue)
                DrawVerticalLine(tmpSectrum, (int)MousePointer.Value.X);
            SpectrumImage = tmpSectrum;
        }

        private void DrawVerticalLine(WriteableBitmap wb,int x, int y = 0)
        {
            using var fb = wb.Lock();
            fb.DrawLine(x, 0, x, fb.Size.Height, _spectrum.PointerColor);
        }

        public Avalonia.Point? MousePointer;

        public static double FrequencyForIndex(int index, int total = 1024) => 90.0 + index * (20.0 / total);
    }
}
