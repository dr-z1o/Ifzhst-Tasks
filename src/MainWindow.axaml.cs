using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaDummyProject.ViewModels;

namespace AvaloniaDummyProject
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                // this.FindControl<Image>("SpectrumImageControl")?.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);

                Console.WriteLine("MainWindow created");
            }
            catch (System.Exception ex)
            {
                // Handle any exceptions that occur during initialization
                System.Console.WriteLine($"Error initializing MainWindow: {ex.Message}");
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var point = e.GetPosition(sender as Control);
            int x = (int)point.X;
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.MousePointer = point;

            // индекс по X → частота
            double frequency = MainWindowViewModel.FrequencyForIndex(x, 1024);
            FrequencyText.Text = $"Frequency: {frequency:F2} MHz";
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            // Очистка курсора и текста частоты при выходе курсора
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.MousePointer = null;
            FrequencyText.Text = "Frequency: N/A"; // Очистка текста частоты при выходе курсора
        }
    }
}