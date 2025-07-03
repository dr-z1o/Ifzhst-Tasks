using System;

namespace AvaloniaDummyProject;

public class SignalGenerator
{
    // private readonly Random _rnd = new();

    // public double[] Generate()
    // {
    //     var result = new double[1024];
    //     for (int i = 0; i < result.Length; i++)
    //     {
    //         //result[i] = -70 + (_rnd.NextDouble() - 0.5) * 10; // -70 +-5 dBm з шумом

    //         result[i] = -120 + (_rnd.NextDouble() * 100) + (_rnd.NextDouble() - 0.5) * 10; // Random value in range [-120, -20] +-5 dBm з шумом
    //     }
    //     return result;
    // }
    private readonly Random _rnd = new();
    private double _phase;

    public double[] Generate()
    {
        var result = new double[1024];
        double center1 = 0.3 + 0.2 * Math.Sin(_phase); // 30% от ширины (примерно 96-й индекс)
        double center2 = 0.7 + 0.1 * Math.Cos(_phase * 1.5); // второй сигнал

        for (int i = 0; i < result.Length; i++)
        {
            double xNorm = (double)i / result.Length;
            double noise = -100 + (_rnd.NextDouble() * 50) + (_rnd.NextDouble() - 0.5) * 10; // случайный шум

            // два псевдосигнала (гиперболические пики)
            double signal1 = Math.Exp(-Math.Pow((xNorm - center1) * 50, 2)) * 40;
            double signal2 = Math.Exp(-Math.Pow((xNorm - center2) * 80, 2)) * 25;

            result[i] = noise + signal1 + signal2;
        }

        _phase += 0.1;
        return result;
    }
}