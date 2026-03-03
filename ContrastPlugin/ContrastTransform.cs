using System;
using System.Threading;
using PluginInterface;

namespace ContrastPlugin
{
    // Плагин «Повышение контрастности»
    // Усиливает контраст изображения с коэффициентом 1.5
    [Version(1, 0)]
    public class ContrastTransform : IPlugin
    {
        public string Name => "Повышение контрастности";
        public string Author => "WPFPaint";

        // Коэффициент контрастности (>1 = усиление, <1 = ослабление)
        private const double ContrastFactor = 1.5;

        public void Transform(byte[] pixelData, int width, int height,
                               IProgress<int> progress, CancellationToken ct)
        {
            int totalPixels = width * height;

            // Предварительный расчёт таблицы контрастности для ускорения (Look Up Table)
            // Формула: newValue = clamp(factor * (value - 128) + 128, 0, 255)
            byte[] contrastLUT = new byte[256];
            for (int v = 0; v < 256; v++)
            {
                // Пиксели светлее 128 становятся ещё светлее, темнее — ещё темнее
                int newVal = (int)(ContrastFactor * (v - 128) + 128);
                contrastLUT[v] = (byte)Math.Clamp(newVal, 0, 255);
            }

            for (int i = 0; i < totalPixels; i++)
            {
                // Механизм корректной отмены
                ct.ThrowIfCancellationRequested();

                // Вычисление позиции пикселя в массиве
                int offset = i * 4;

                // Применяем таблицу контрастности к B, G, R
                pixelData[offset] = contrastLUT[pixelData[offset]];
                pixelData[offset + 1] = contrastLUT[pixelData[offset + 1]];
                pixelData[offset + 2] = contrastLUT[pixelData[offset + 2]];

                // Обновляем прогресс-бар каждые 1% от общего числа пикселей
                if (i % (totalPixels / 100 + 1) == 0)
                    progress?.Report(i * 100 / totalPixels);
            }

            progress?.Report(100);
        }
    }
}
