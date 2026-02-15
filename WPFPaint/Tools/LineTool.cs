using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFPaint.Tools
{
    public class LineTool : ITool
    {
        // Название инструмента
        public string ToolName => "Линия";
        // Начальная точка линии
        private Point? _startPoint;
        // Превью линии для отображения при рисовании
        private Line _previewLine;

        // Курсор инструмента (крестик)
        public Cursor GetCursor() => Cursors.Cross;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _startPoint = position; // Сохраняем начальную точку
            // Создаем превью линии
            _previewLine = new Line
            {
                Stroke = new SolidColorBrush(canvas.StrokeColor), // Цвет линии
                StrokeThickness = canvas.StrokeThickness, // Толщина линии
                X1 = position.X, // Начальная точка X
                Y1 = position.Y, // Начальная точка Y
                X2 = position.X, // Конечная точка X (совпадает с начальной)
                Y2 = position.Y // Конечная точка Y (совпадает с начальной)
            };
            canvas.OverlayCanvas.Children.Add(_previewLine); // Добавляем на OverlayCanvas для отображения
        }

        // Движение мыши
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e)
        {
            // Проверяем что кнопка нажата и есть начальная точка
            if (e.LeftButton != MouseButtonState.Pressed || _startPoint == null || _previewLine == null) return;

            // Обновляем конечную точку линии
            _previewLine.X2 = position.X;
            _previewLine.Y2 = position.Y;
        }

        // Отпускание кнопки мыши
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            if (_startPoint == null) return;

            try
            {
                canvas.OverlayCanvas.Children.Remove(_previewLine); // Удаляем превью
                PenTool.DrawLine(canvas.Bitmap, _startPoint.Value, position, canvas.StrokeColor, canvas.StrokeThickness); // Рисуем линию на битмапе
                canvas.InvalidateBitmap(); // Обновляем отображение
                canvas.IsModified = true; // Помечаем документ как измененный
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рисования линии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _startPoint = null; // Сбрасываем начальную точку
                _previewLine = null; // Сбрасываем превью
            }
        }
    }
}
