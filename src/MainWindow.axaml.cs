using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaDummyProject.ViewModels;

namespace AvaloniaDummyProject
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.TemplateApplied += OnTemplateApplied;
                Console.WriteLine("MainWindow created");
            }
            catch (System.Exception ex)
            {
                // Handle any exceptions that occur during initialization
                System.Console.WriteLine($"Error initializing MainWindow: {ex.Message}");
            }
        }

        private void OnTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            InitBitmaps();
        }

        private void InitBitmaps()
        {
            // Ensure that the ViewModel is set and subscribed to the BitmapUpdated event
            if (_vm != null) return;

            _vm = this.DataContext as MainWindowViewModel;
            _vm.BitmapUpdated += OnBitmapUpdated;
        }

        private void OnBitmapUpdated()
        {
            if (SpectrumImageControl != null)
            {
                SpectrumImageControl.Source = null;
                SpectrumImageControl.Source = _vm.SpectrumImage;
            }

            if (WaterfallImageControl != null)
            {
                WaterfallImageControl.Source = null;
                WaterfallImageControl.Source = _vm.WaterfallImage;
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            // Update the mouse pointer position in the ViewModel
            var point = e.GetPosition(sender as Control);
            int x = (int)point.X;
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.MousePointer = point;
        }

        private void OnPointerExited(object sender, PointerEventArgs e)
        {
            // Cleanup the mouse pointer when it exits the control
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.MousePointer = null;
          }
    }
}