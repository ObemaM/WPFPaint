using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFPaint.Tools
{
    // Инструмент увеличения масштаба
    public class ZoomInTool : ITool
    {
        // Название инструмента
        public string ToolName => "Масштаб+";

        // Курсор инструмента (стрелка)
        public Cursor GetCursor() => Cursors.Arrow;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            try
            {
                double newZoom = Math.Min(canvas.ZoomLevel + 0.25, 5.0); // Увеличиваем масштаб на 0.25, но не более 5.0
                canvas.ZoomLevel = newZoom; // Применяем новый масштаб
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка масштабирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e) { }
        
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e) { }
    }

    // Инструмент уменьшения масштаба
    public class ZoomOutTool : ITool
    {
        // Название инструмента
        public string ToolName => "Масштаб-";

        // Курсор инструмента (стрелка)
        public Cursor GetCursor() => Cursors.Arrow;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            try
            {
                double newZoom = Math.Max(canvas.ZoomLevel - 0.25, 0.25); // Уменьшаем масштаб на 0.25, но не менее 0.25
                canvas.ZoomLevel = newZoom; // Применяем новый масштаб
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка масштабирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e) { }
        
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e) { }
    }
}
