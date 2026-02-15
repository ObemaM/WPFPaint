using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFPaint.Tools
{
    // Интерфейс инструментов рисования
    public interface ITool
    {
        // Название инструмента (для статусбара)
        string ToolName { get; }

        // Курсор для данного инструмента
        Cursor GetCursor();

        // Нажатие кнопки мыши
        void OnMouseDown(DrawingCanvas canvas, Point position, MouseButtonEventArgs e);

        // Движение мыши
        void OnMouseMove(DrawingCanvas canvas, Point position, MouseEventArgs e);

        // Отпускание кнопки мыши
        void OnMouseUp(DrawingCanvas canvas, Point posiion, MouseButtonEventArgs e);
    }
}
