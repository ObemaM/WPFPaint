using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFPaint.Tools
{
    // Инструмент выноски — рисует прямоугольник с указательным хвостиком
    public class CalloutTool : ITool
    {
        // Название инструмента
        public string ToolName => "Выноска";
        // Начальная точка рисования
        private Point? _startPoint;
        // Превью пути для отображения при рисовании
        private System.Windows.Shapes.Path _previewPath;

        // Курсор инструмента (крестик)
        public Cursor GetCursor() => Cursors.Cross;

        // Нажатие кнопки мыши
        public void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            _startPoint = position; // Сохраняем начальную точку
            // Создаем превью пути
            _previewPath = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(canvas.StrokeColor), // Цвет обводки
                StrokeThickness = canvas.StrokeThickness, // Толщина обводки
                Fill = canvas.IsFilled ? new SolidColorBrush(canvas.FillColor) : null // Заливка если включена
            };
            canvas.OverlayCanvas.Children.Add(_previewPath); // Добавляем на OverlayCanvas для отображения
        }

        // Движение мыши
        public void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e)
        {
            // Проверяем что кнопка нажата и есть начальная точка
            if (e.LeftButton != MouseButtonState.Pressed || _startPoint == null || _previewPath == null) return;

            _previewPath.Data = CreateCalloutGeometry(_startPoint.Value, position); // Создаем и устанавливаем геометрию выноски
        }

        // Отпускание кнопки мыши
        public void OnMouseUp(DrawingCanvas canvas, Point position, MouseButtonEventArgs e)
        {
            if (_startPoint == null) return;

            try
            {
                canvas.OverlayCanvas.Children.Remove(_previewPath); // Удаляем превью

                // Вычисляем размеры выноски
                double w = Math.Abs(position.X - _startPoint.Value.X); // Ширина
                double h = Math.Abs(position.Y - _startPoint.Value.Y); // Высота
                if (w < 5 || h < 5) return; // Если размер слишком маленький, выходим

                var geometry = CreateCalloutGeometry(_startPoint.Value, position); // Создаем геометрию выноски

                DrawShapeOnBitmap(canvas.Bitmap, geometry, canvas.StrokeColor, canvas.StrokeThickness, // Рисуем выноску на битмапе
                    canvas.IsFilled ? (Color?)canvas.FillColor : null);
                canvas.InvalidateBitmap(); // Обновляем отображение
                canvas.IsModified = true; // Помечаем документ как измененный
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рисования выноски: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _startPoint = null; // Сбрасываем начальную точку
                _previewPath = null; // Сбрасываем превью
            }
        }

        // Создание геометрии выноски (прямоугольник с хвостиком)
        private Geometry CreateCalloutGeometry(Point start, Point end)
        {
            double x = Math.Min(start.X, end.X); // Левая граница
            double y = Math.Min(start.Y, end.Y); // Верхняя граница
            double w = Math.Abs(end.X - start.X); // Ширина
            double h = Math.Abs(end.Y - start.Y); // Высота

            if (w < 2 || h < 2) return Geometry.Empty; // Если размер слишком маленький, возвращаем пустую геометрию

            // Выноска: прямоугольник с треугольным хвостиком внизу слева
            double tailWidth = w * 0.15; // Ширина хвостика
            double tailHeight = h * 0.3; // Высота хвостика
            double rectHeight = h - tailHeight; // Высота прямоугольника

            if (rectHeight < 2) rectHeight = h * 0.7; // Минимальная высота прямоугольника

            var fig = new PathFigure { StartPoint = new Point(x, y), IsClosed = true, IsFilled = true }; // Создаем фигуру пути

            // Верхний правый угол
            fig.Segments.Add(new LineSegment(new Point(x + w, y), true));
            // Нижний правый угол
            fig.Segments.Add(new LineSegment(new Point(x + w, y + rectHeight), true));
            // К началу хвостика
            fig.Segments.Add(new LineSegment(new Point(x + w * 0.35, y + rectHeight), true));
            // Кончик хвостика
            fig.Segments.Add(new LineSegment(new Point(x + w * 0.15, y + h), true));
            // Конец хвостика
            fig.Segments.Add(new LineSegment(new Point(x + w * 0.2, y + rectHeight), true));
            // Нижний левый угол
            fig.Segments.Add(new LineSegment(new Point(x, y + rectHeight), true));
            // Замыкаем к началу

            var pg = new PathGeometry(); // Создаем геометрию пути
            pg.Figures.Add(fig); // Добавляем фигуру
            return pg; // Возвращаем геометрию
        }

        // Рисование формы на битмапе
        private void DrawShapeOnBitmap(WriteableBitmap bmp, Geometry geometry, Color strokeColor, int strokeThickness, Color? fillColor)
        {
            var dv = new DrawingVisual(); // Создаем визуальный элемент для рисования
            using (var dc = dv.RenderOpen()) // Открываем контекст рисования
            {
                Brush fill = fillColor.HasValue ? new SolidColorBrush(fillColor.Value) : null; // Заливка если есть
                Pen pen = new Pen(new SolidColorBrush(strokeColor), strokeThickness); // Кисть для обводки
                dc.DrawGeometry(fill, pen, geometry); // Рисуем геометрию
            }
            var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32); // Создаем целевой битмап для рендеринга
            rtb.Render(dv); // Рендерим визуальный элемент в битмап
            TextTool.CompositeOnBitmap(bmp, rtb); // Композитим (смешиваем) с исходным битмапом
        }
    }
}
