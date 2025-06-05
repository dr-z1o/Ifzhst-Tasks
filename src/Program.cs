// filepath: /Users/dr_zlo/Documents/test-app/infozahyst-test/avalonia-dummy-project/src/Program.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Rendering;
using System;

namespace AvaloniaDummyProject
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Avalonia App...");
            //Console.WriteLine($"Rendering mode: {AvaloniaLocator.Current.GetService<IRenderRoot>()?.GetType().FullName}");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            Console.WriteLine("BuildAvaloniaApp Avalonia App...");
           return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
        }

    }
}