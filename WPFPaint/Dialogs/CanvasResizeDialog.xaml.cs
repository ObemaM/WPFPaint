using System;
using System.Windows;

namespace WPFPaint.Dialogs
{
    public partial class CanvasResizeDialog : Window
    {
        // Ширина холста
        public int CanvasWidth { get; private set; }
        // Высота холста
        public int CanvasHeight { get; private set; }

        // Конструктор диалога изменения размера
        public CanvasResizeDialog(int currentWidth, int currentHeight)
        {
            InitializeComponent();
            
            // Устанавливаем текущие размеры
            CanvasWidth = currentWidth;
            CanvasHeight = currentHeight;
            
            // Заполняем поля ввода текущими значениями
            WidthBox.Text = currentWidth.ToString();
            HeightBox.Text = currentHeight.ToString();
            
            // Устанавливаем фокус и выделяем текст
            WidthBox.Focus();
            WidthBox.SelectAll();
        }

        // Нажатие кнопки ОК
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Парсим введенные значения
                int w = int.Parse(WidthBox.Text);
                int h = int.Parse(HeightBox.Text);

                // Проверяем ширину
                if (w < 1 || w > 4000)
                {
                    MessageBox.Show("Ширина должна быть от 1 до 4000 пикселей.", "Ошибка ввода",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Проверяем высоту
                if (h < 1 || h > 4000)
                {
                    MessageBox.Show("Высота должна быть от 1 до 4000 пикселей.", "Ошибка ввода",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем новые размеры
                CanvasWidth = w;
                CanvasHeight = h;
                DialogResult = true;
            }
            catch (FormatException)
            {
                MessageBox.Show("Введите корректные числовые значения.", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (OverflowException)
            {
                MessageBox.Show("Введённое значение слишком большое.", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
