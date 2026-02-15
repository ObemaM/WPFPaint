using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFPaint.Tools
{
    // Инструмент текста
    public class TextTool : ITool
    {
        // Название инструмента
        public string ToolName => "Текст";

        // Курсор инструмента (текстовый курсор)
        public Cursor GetCursor() => Cursors.IBeam;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            // Проверяем что позиция в пределах битмапа
            if (position.X < 0 || position.Y < 0 ||
                position.X > canvas.Bitmap.PixelWidth ||
                position.Y > canvas.Bitmap.PixelHeight)
                return;

            try
            {
                var dialog = new Dialogs.TextInputDialog(); // Создаем диалог ввода текста
                dialog.Owner = Window.GetWindow(canvas); // Устанавливаем владельца диалога
                if (dialog.ShowDialog() == true) // Если диалог закрыт с ОК
                {
                    string text = dialog.EnteredText; // Получаем введенный текст
                    string fontFamily = dialog.SelectedFontFamily; // Получаем выбранный шрифт
                    double fontSize = dialog.SelectedFontSize; // Получаем выбранный размер

                    if (string.IsNullOrEmpty(text)) return; // Если текст пустой, выходим

                    DrawTextOnBitmap(canvas.Bitmap, position, text, canvas.StrokeColor, fontFamily, fontSize); // Рисуем текст на битмапе
                    canvas.InvalidateBitmap(); // Обновляем отображение
                    canvas.IsModified = true; // Помечаем документ как измененный
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инструмента Текст: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Движение мыши (не используется)
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e) { }
        // Отпускание кнопки мыши (не используется)
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e) { }

        // Рисование текста на битмапе
        private void DrawTextOnBitmap(WriteableBitmap bmp, Point position, string text, Color color, string fontFamily, double fontSize)
        {
            var dv = new DrawingVisual(); // Создаем визуальный элемент для рисования
            using (var dc = dv.RenderOpen()) // Открываем контекст рисования
            {
                // Создаем форматированный текст
                var formattedText = new FormattedText(
                    text, // Текст
                    System.Globalization.CultureInfo.CurrentCulture, // Культура
                    FlowDirection.LeftToRight, // Направление текста
                    new Typeface(fontFamily), // Шрифт
                    fontSize, // Размер шрифта
                    new SolidColorBrush(color), // Цвет текста
                    VisualTreeHelper.GetDpi(dv).PixelsPerDip); // DPI для корректного отображения

                dc.DrawText(formattedText, position); // Рисуем текст
            }

            var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32); // Создаем целевой битмап для рендеринга
            rtb.Render(dv); // Рендерим визуальный элемент в битмап

            CompositeOnBitmap(bmp, rtb); // Композитим (смешиваем) с исходным битмапом
        }

        // Композитинг (смешивание) битмапов
        // Нужно, так как текст рендеристся в отдельный элемент с прозрачным фоном
        internal static void CompositeOnBitmap(WriteableBitmap bmp, RenderTargetBitmap rtb)
        {
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
                            if (srcA == 0) 
                                continue; // Если пиксель прозрачный, пропускаем

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
                            int outR = outA > 0 ? (srcR * srcA + dstR * dstA * (255 - srcA) / 255) / outA : 0; // Результирующий красный
                            int outG = outA > 0 ? (srcG * srcA + dstG * dstA * (255 - srcA) / 255) / outA : 0; // Результирующий зеленый
                            int outB = outA > 0 ? (srcB * srcA + dstB * dstA * (255 - srcA) / 255) / outA : 0; // Результирующий синий

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
