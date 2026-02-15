using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFPaint.Tools
{
    // Инструмент эллипса
    public class EllipseTool : ITool
    {
        // Название инструмента
        public string ToolName => "Эллипс";
        // Начальная точка рисования
        private Point? _startPoint;
        // Превью эллипса для отображения при рисовании
        private Ellipse _previewEllipse;

        // Курсор инструмента (крестик)
        public Cursor GetCursor() => Cursors.Cross;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _startPoint = position; // Сохраняем начальную точку
            // Создаем превью эллипса
            _previewEllipse = new Ellipse
            {
                Stroke = new SolidColorBrush(canvas.StrokeColor), // Цвет обводки
                StrokeThickness = canvas.StrokeThickness, // Толщина обводки
                Fill = canvas.IsFilled ? new SolidColorBrush(canvas.FillColor) : null, // Заливка если включена
                Width = 0, // Начальная ширина
                Height = 0 // Начальная высота
            };
            System.Windows.Controls.Canvas.SetLeft(_previewEllipse, position.X); // Устанавливаем позицию X
            System.Windows.Controls.Canvas.SetTop(_previewEllipse, position.Y); // Устанавливаем позицию Y
            canvas.OverlayCanvas.Children.Add(_previewEllipse); // Добавляем на OverlayCanvas для отображения
        }

        // Движение мыши
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e)
        {
            // Проверяем что кнопка нажата и есть начальная точка
            if (e.LeftButton != MouseButtonState.Pressed || _startPoint == null || _previewEllipse == null) return;

            // Вычисляем позицию и размеры эллипса
            double x = Math.Min(_startPoint.Value.X, position.X); // Левая граница
            double y = Math.Min(_startPoint.Value.Y, position.Y); // Верхняя граница
            double w = Math.Abs(position.X - _startPoint.Value.X); // Ширина
            double h = Math.Abs(position.Y - _startPoint.Value.Y); // Высота

            // Обновляем превью эллипса
            System.Windows.Controls.Canvas.SetLeft(_previewEllipse, x); // Обновляем позицию X
            System.Windows.Controls.Canvas.SetTop(_previewEllipse, y); // Обновляем позицию Y
            _previewEllipse.Width = w; // Обновляем ширину
            _previewEllipse.Height = h; // Обновляем высоту
        }

        // Отпускание кнопки мыши
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            if (_startPoint == null) return;

            try
            {
                canvas.OverlayCanvas.Children.Remove(_previewEllipse); // Удаляем превью

                // Вычисляем позицию и размеры эллипса
                double x = Math.Min(_startPoint.Value.X, position.X); // Левая граница
                double y = Math.Min(_startPoint.Value.Y, position.Y); // Верхняя граница
                double w = Math.Abs(position.X - _startPoint.Value.X); // Ширина
                double h = Math.Abs(position.Y - _startPoint.Value.Y); // Высота

                if (w < 1 || h < 1) return; // Если размер слишком маленький, выходим

                DrawEllipseOnBitmap(canvas.Bitmap, x, y, w, h, // Рисуем эллипс на битмапе
                    canvas.StrokeColor, canvas.StrokeThickness,
                    canvas.IsFilled ? (Color?)canvas.FillColor : null);
                canvas.InvalidateBitmap(); // Обновляем отображение
                canvas.IsModified = true; // Помечаем документ как измененный
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рисования эллипса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _startPoint = null; // Сбрасываем начальную точку
                _previewEllipse = null; // Сбрасываем превью
            }
        }

        // Рисование эллипса на битмапе
        private void DrawEllipseOnBitmap(WriteableBitmap bmp, double x, double y, double w, double h,
            Color strokeColor, int strokeThickness, Color? fillColor)
        {
            var dv = new DrawingVisual(); // Создаем визуальный элемент для рисования
            using (var dc = dv.RenderOpen()) // Открываем контекст рисования
            {
                Brush fill = fillColor.HasValue ? new SolidColorBrush(fillColor.Value) : null; // Заливка если есть
                Pen pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness); // Кисть для обводки
                dc.DrawEllipse(fill, pen, new Point(x + w / 2, y + h / 2), w / 2, h / 2); // Рисуем эллипс
            }
            var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32); // Создаем целевой битмап для рендеринга
            rtb.Render(dv); // Рендерим визуальный элемент в битмап

            // Композитинг (смешивание) с существующим битмапом
            bmp.Lock(); // Блокируем битмап для прямого доступа к пикселям
            try
            {
                int[] pixels = new int[bmp.PixelWidth * bmp.PixelHeight]; // Массив пикселей
                rtb.CopyPixels(pixels, bmp.PixelWidth * 4, 0); // Копируем пиксели из rtb

                unsafe
                {
                    IntPtr buffer = bmp.BackBuffer; // Указатель на буфер битмапа
                    int stride = bmp.BackBufferStride; // Шаг (количество байт на строку)
                    for (int py = 0; py < bmp.PixelHeight; py++) // Проходим по строкам
                    {
                        for (int px = 0; px < bmp.PixelWidth; px++) // Проходим по пикселям в строке
                        {
                            int src = pixels[py * bmp.PixelWidth + px]; // Исходный пиксель
                            int srcA = (src >> 24) & 0xFF; // Альфа-канал исходного пикселя
                            if (srcA == 0) continue; // Если пиксель прозрачный, пропускаем

                            int srcR = (src >> 16) & 0xFF; // Красный канал исходного
                            int srcG = (src >> 8) & 0xFF; // Зеленый канал исходного
                            int srcB = src & 0xFF; // Синий канал исходного

                            // Если альфа-канал полупрозрачный, делаем предмножение
                            if (srcA > 0 && srcA < 255)
                            {
                                srcR = srcR * 255 / srcA;
                                srcG = srcG * 255 / srcA;
                                srcB = srcB * 255 / srcA;
                            }

                            int* dst = (int*)(buffer + py * stride + px * 4); // Указатель на пиксель назначения
                            int dstPixel = *dst; // Пиксель назначения
                            int dstA = (dstPixel >> 24) & 0xFF; // Альфа-канал назначения
                            int dstR = (dstPixel >> 16) & 0xFF; // Красный канал назначения
                            int dstG = (dstPixel >> 8) & 0xFF; // Зеленый канал назначения
                            int dstB = dstPixel & 0xFF; // Синий канал назначения

                            // Вычисляем результирующие каналы с альфа-блендингом
                            int outA = srcA + dstA * (255 - srcA) / 255; // Результирующая альфа
                            int outR = (srcR * srcA + dstR * dstA * (255 - srcA) / 255) / (outA > 0 ? outA : 1); // Результирующий красный
                            int outG = (srcG * srcA + dstG * dstA * (255 - srcA) / 255) / (outA > 0 ? outA : 1); // Результирующий зеленый
                            int outB = (srcB * srcA + dstB * dstA * (255 - srcA) / 255) / (outA > 0 ? outA : 1); // Результирующий синий

                            *dst = (outA << 24) | (outR << 16) | (outG << 8) | outB; // Записываем результирующий пиксель
                        }
                    }
                }
                bmp.AddDirtyRect(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)); // Обновляем весь битмап
            }
            finally
            {
                bmp.Unlock(); // Разблокируем битмап
            }
        }
    }
}
