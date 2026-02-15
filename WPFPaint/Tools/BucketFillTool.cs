using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFPaint.Tools
{
    // Инструмент заливки
    public class BucketFillTool : ITool
    {
        // Название инструмента
        public string ToolName => "Заливка";

        // Курсор инструмента (рука)
        public Cursor GetCursor() => Cursors.Hand;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            int x = (int)position.X; // Координата X
            int y = (int)position.Y; // Координата Y

            // Проверяем что позиция в пределах битмапа
            if (x < 0 || x >= canvas.Bitmap.PixelWidth || y < 0 || y >= canvas.Bitmap.PixelHeight)
                return;

            try
            {
                FloodFill(canvas.Bitmap, x, y, canvas.FillColor); // Заливаем область
                canvas.InvalidateBitmap(); // Обновляем отображение
                canvas.IsModified = true; // Помечаем документ как измененный
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка заливки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Движение мыши (не используется)
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e) { }
        // Отпускание кнопки мыши (не используется)
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e) { }

        // Заливка области (алгоритм заливки Flood Fill)
        private void FloodFill(WriteableBitmap bmp, int startX, int startY, Color fillColor)
        {
            int w = bmp.PixelWidth; // Ширина битмапа
            int h = bmp.PixelHeight; // Высота битмапа
            int[] pixels = new int[w * h]; // Массив пикселей
            bmp.CopyPixels(pixels, w * 4, 0); // Копируем пиксели из битмапа

            int targetColor = pixels[startY * w + startX]; // Цвет целевой точки (откуда начинаем)
            int replacement = (fillColor.A << 24) | (fillColor.R << 16) | (fillColor.G << 8) | fillColor.B; // Цвет замены в формате BGRA

            if (targetColor == replacement) return; // Если цвета совпадают, выходим

            var stack = new Stack<Tuple<int, int>>(); // Стек для алгоритма заливки
            stack.Push(Tuple.Create(startX, startY)); // Добавляем начальную точку в стек

            while (stack.Count > 0) // Пока стек не пуст
            {
                var point = stack.Pop(); // Извлекаем точку из стека
                int px = point.Item1; // Координата X точки
                int py = point.Item2; // Координата Y точки

                if (px < 0 || px >= w || py < 0 || py >= h) continue; // Если точка за пределами, пропускаем

                int idx = py * w + px; // Индекс пикселя в массиве
                if (pixels[idx] != targetColor) continue; // Если цвет не совпадает с целевым, пропускаем

                pixels[idx] = replacement; // Заменяем цвет пикселя

                // Добавляем соседние пиксели в стек
                stack.Push(Tuple.Create(px + 1, py)); // Справа
                stack.Push(Tuple.Create(px - 1, py)); // Слева
                stack.Push(Tuple.Create(px, py + 1)); // Снизу
                stack.Push(Tuple.Create(px, py - 1)); // Сверху
            }

            bmp.Lock(); // Блокируем битмап для записи
            try
            {
                bmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0); // Записываем пиксели в битмап
            }
            finally
            {
                bmp.Unlock(); // Разблокируем битмап
            }
        }
    }
}
