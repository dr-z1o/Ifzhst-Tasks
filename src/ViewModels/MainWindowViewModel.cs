using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using AvaloniaDummyProject.Extensions;
using AvaloniaDummyProject.Renderers;
using Avalonia;

namespace AvaloniaDummyProject.ViewModels
{

    public partial class MainWindowViewModel : ObservableObject
    {
        private DispatcherTimer _timer;
        private Point? _mousePointer;

        private readonly SignalGenerator _generator;
        private readonly SpectrumRenderer _spectrum;
        private readonly WaterfallRenderer _waterfall;

        [ObservableProperty]
        private WriteableBitmap spectrumImage;

        [ObservableProperty]
        private WriteableBitmap waterfallImage;

        [ObservableProperty]
        private double cursorFrequency;
        
        public MainWindowViewModel()
        {
            _generator = new SignalGenerator();
            _spectrum = new SpectrumRenderer();
            _waterfall = new WaterfallRenderer();
        }

        [RelayCommand]
        private void Start()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += (s, e) => Update();
            _timer.Start();

            SpectrumImage = _spectrum.GetBitmap();
            WaterfallImage = _waterfall.GetBitmap();
        }

        [RelayCommand]
        private void Stop() => _timer?.Stop();

        public event Action BitmapUpdated;

        private void Update()
        {
            var data = _generator.Generate();
            _waterfall.AddAndRender(data);

            _spectrum.Render(data, MousePointer.HasValue ? (int)MousePointer.Value.X : -1);

            BitmapUpdated?.Invoke();
        }

        public Point? MousePointer
        {
            get => _mousePointer;
            set
            {
                _mousePointer = value;
                CursorFrequency = value.HasValue ? FrequencyForIndex((int)value.Value.X, 1024) : 0;
            }
        }

        public static double FrequencyForIndex(int index, int total = 1024) => 90.0 + index * (20.0 / total);
    }
}
