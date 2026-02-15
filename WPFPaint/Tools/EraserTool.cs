using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFPaint.Tools
{
    public class EraserTool : ITool
    {
        // Название инструмента
        public string ToolName => "Ластик";
        // Последняя позиция мыши для рисования линий
        private Point? _lastPoint;

        // Курсор ластика (запрещающий знак)
        public Cursor GetCursor() => Cursors.No;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _lastPoint = position; // Сохраняем начальную позицию
            EraseAt(canvas, position); // Стираем в точке клика
            canvas.IsModified = true; // Помечаем документ как измененный
        }

        // Движение мыши
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e)
        {
            // Проверяем что кнопка нажата и есть предыдущая позиция
            if (e.LeftButton != MouseButtonState.Pressed || _lastPoint == null) return;

            try
            {
                // Рисуем белую линию между точками (стирание)
                PenTool.DrawLine(canvas.Bitmap, _lastPoint.Value, position, Colors.White, canvas.StrokeThickness * 2);
                canvas.InvalidateBitmap(); // Обновляем отображение
                _lastPoint = position; // Обновляем последнюю позицию
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка ластика: {ex.Message}");
            }
        }

        // Отпускание кнопки мыши
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _lastPoint = null; // Сбрасываем последнюю позицию
        }

        // Стирание в точке
        private void EraseAt(DrawingCanvas canvas, Point position)
        {
            try
            {
                // Рисуем точку белым цветом (стирание)
                PenTool.DrawLine(canvas.Bitmap, position, position, Colors.White, canvas.StrokeThickness * 2);
                canvas.InvalidateBitmap(); // Обновляем отображение
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка ластика: {ex.Message}");
            }
        }
    }
}
