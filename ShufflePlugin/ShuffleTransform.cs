using System;
using System.Threading;
using PluginInterface;

namespace ShufflePlugin
{
    // Плагин «Перемешивание частей»
    // Разбивает изображение на 9 равных частей (сетка 3×3) и перемешивает их в случайном порядке
    [Version(1, 0)]
    public class ShuffleTransform : IPlugin
    {
        public string Name => "Перемешивание частей (3×3)";
        public string Author => "WPFPaint";

        public void Transform(byte[] pixelData, int width, int height,
                               IProgress<int> progress, CancellationToken ct)
        {
            // Размеры каждой из 9 частей
            int partW = width / 3;
            int partH = height / 3;

            // Создаём массив индексов частей [0..8] и перемешиваем
            int[] order = new int[9];
            for (int i = 0; i < 9; i++) order[i] = i;

            var rng = new Random();
            for (int i = 8; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // Копируем исходные данные для чтения
            byte[] source = new byte[pixelData.Length];
            Array.Copy(pixelData, source, pixelData.Length);

            int bytesPerPixel = 4; // BGRA32

            // Перемещаем каждую часть на новое место
            for (int idx = 0; idx < 9; idx++)
            {
                // Механизм корректной отмены
                ct.ThrowIfCancellationRequested();

                // Позиция источника (исходная позиция части order[idx])
                int srcCol = order[idx] % 3;
                int srcRow = order[idx] / 3;
                int srcX = srcCol * partW;
                int srcY = srcRow * partH;

                // Позиция назначения (позиция idx)
                int dstCol = idx % 3;
                int dstRow = idx / 3;
                int dstX = dstCol * partW;
                int dstY = dstRow * partH;

                // Определяем фактические размеры части (последняя может быть чуть больше)
                int actualW = (dstCol == 2) ? (width - dstX) : partW;
                int actualH = (dstRow == 2) ? (height - dstY) : partH;
                int srcActualW = (srcCol == 2) ? (width - srcX) : partW;
                int srcActualH = (srcRow == 2) ? (height - srcY) : partH;
                // Берём минимальные размеры для безопасности
                int copyW = Math.Min(actualW, srcActualW);
                int copyH = Math.Min(actualH, srcActualH);

                // Копируем пиксели построчно
                for (int y = 0; y < copyH; y++)
                {
                    // Вычисляем смещения в массивах
                    int srcOffset = ((srcY + y) * width + srcX) * bytesPerPixel;
                    int dstOffset = ((dstY + y) * width + dstX) * bytesPerPixel;
                    int rowBytes = copyW * bytesPerPixel;

                    // Копируем строку
                    Array.Copy(source, srcOffset, pixelData, dstOffset, rowBytes);
                }

                // Отчёт о прогрессе
                progress?.Report((idx + 1) * 100 / 9);
            }

            progress?.Report(100);
        }
    }
}
