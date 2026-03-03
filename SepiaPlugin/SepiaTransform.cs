using System;
using System.Threading;
using PluginInterface;

namespace SepiaPlugin
{
    // Плагин «Оттенки сепии»
    // Преобразует цветное изображение в тёплый коричневатый оттенок
    [Version(1, 0)]
    public class SepiaTransform : IPlugin
    {
        public string Name => "Оттенки сепии";
        public string Author => "WPFPaint";

        public void Transform(byte[] pixelData, int width, int height,
                               IProgress<int> progress, CancellationToken ct)
        {
            int totalPixels = width * height;

            for (int i = 0; i < totalPixels; i++)
            {
                // Проверка отмены операции
                ct.ThrowIfCancellationRequested();

                int offset = i * 4; // BGRA32: каждый пиксель = 4 байта

                // Исходные компоненты (B, G, R)
                byte b = pixelData[offset]; // Синий
                byte g = pixelData[offset + 1]; // Зеленый
                byte r = pixelData[offset + 2]; // Красный

                // Формула сепии (стандартная матрица Microsoft)
                int newR = (int)(r * 0.393 + g * 0.769 + b * 0.189);
                int newG = (int)(r * 0.349 + g * 0.686 + b * 0.168);
                int newB = (int)(r * 0.272 + g * 0.534 + b * 0.131);

                // Ограничиваем значения до 255
                pixelData[offset] = (byte)Math.Min(newB, 255);
                pixelData[offset + 1] = (byte)Math.Min(newG, 255);
                pixelData[offset + 2] = (byte)Math.Min(newR, 255);

                // Отчёт о прогрессе каждые 1% пикселей (для прогресс-бара)
                if (i % (totalPixels / 100 + 1) == 0)
                    progress?.Report(i * 100 / totalPixels);
            }

            progress?.Report(100);
        }
    }
}
