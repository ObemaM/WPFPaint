using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFPaint.Tools
{
    // Инструмент пера
    public class PenTool : ITool
    {
        // Название инструмента
        public string ToolName => "Перо";
        // Последняя позиция мыши для рисования линий
        private Point? _lastPoint;

        // Курсор инструмента (ручка)
        public Cursor GetCursor() => Cursors.Pen;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _lastPoint = position; // Сохраняем начальную позицию
            canvas.IsModified = true; // Помечаем документ как измененный
        }

        // Движение мыши
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e)
        {
            // Проверяем что кнопка нажата и есть предыдущая позиция
            if (e.LeftButton != MouseButtonState.Pressed || _lastPoint == null) 
            {
                return;
            }

            try
            {
                DrawLine(canvas.Bitmap, _lastPoint.Value, position, canvas.StrokeColor, canvas.StrokeThickness); // Рисуем линию
                canvas.InvalidateBitmap(); // Обновляем отображение
                _lastPoint = position; // Обновляем последнюю позицию
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка рисования пером: {ex.Message}");
            }
        }

        // Отпускание кнопки мыши
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _lastPoint = null; // Сбрасываем последнюю позицию
        }

        // Рисование линии
        public static void DrawLine(WriteableBitmap bmp, Point p0, Point p1, Color color, int thickness)
        {
            int x0 = (int)p0.X, y0 = (int)p0.Y; // Начальная точка
            int x1 = (int)p1.X, y1 = (int)p1.Y; // Конечная точка
            int w = bmp.PixelWidth, h = bmp.PixelHeight; // Размеры битмапа

            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0); // Разница координат
            int sx = x0 < x1 ? 1 : -1; // Направление по X
            int sy = y0 < y1 ? 1 : -1; // Направление по Y
            int err = dx - dy; // Ошибка

            bmp.Lock(); // Блокируем битмап для прямого доступа к пикселям
            try
            {
                unsafe
                {
                    IntPtr buffer = bmp.BackBuffer; // Указатель на буфер битмапа
                    int stride = bmp.BackBufferStride; // Шаг (количество байт на строку)
                    int pixel = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B; // Цвет пикселя в формате BGRA

                    while (true)
                    {
                        FillCircle(buffer, stride, w, h, x0, y0, thickness / 2, pixel); // Рисуем круг в текущей точке

                        if (x0 == x1 && y0 == y1) break; // Если достигли конечной точки, выходим
                        int e2 = 2 * err; // Удвоенная ошибка
                        if (e2 > -dy) { err -= dy; x0 += sx; } // Двигаемся по X
                        if (e2 < dx) { err += dx; y0 += sy; } // Двигаемся по Y
                    }
                }
                bmp.AddDirtyRect(new Int32Rect(0, 0, w, h)); // Обновляем весь битмап
            }
            finally
            {
                bmp.Unlock(); // Разблокируем битмап
            }
        }

        // Заполнение круга (для рисования толстых линий)
        private static unsafe void FillCircle(IntPtr buffer, int stride, int bmpW, int bmpH, int cx, int cy, int radius, int pixel)
        {
            if (radius < 1) radius = 1; // Минимальный радиус
            int r2 = radius * radius; // Квадрат радиуса для проверки расстояния
            for (int dy = -radius; dy <= radius; dy++) // Проходим по строкам
            {
                for (int dx = -radius; dx <= radius; dx++) // Проходим по столбцам
                {
                    if (dx * dx + dy * dy <= r2) // Если точка внутри круга
                    {
                        int px = cx + dx, py = cy + dy; // Вычисляем позицию пикселя
                        if (px >= 0 && px < bmpW && py >= 0 && py < bmpH) // Проверяем границы
                        {
                            *((int*)(buffer + py * stride + px * 4)) = pixel; // Записываем пиксель
                        }
                    }
                }
            }
        }
    }
}
