using System;
using System.Windows;
using System.Windows.Media;

namespace WPFPaint.Dialogs
{
    public partial class TextInputDialog : Window
    {
        // Введенный текст
        public string EnteredText { get; private set; }
        // Выбранный шрифт
        public string SelectedFontFamily { get; private set; }
        // Выбранный размер шрифта
        public double SelectedFontSize { get; private set; }

        // Конструктор диалога ввода текста
        public TextInputDialog()
        {
            InitializeComponent();

            // Заполняем список шрифтов
            foreach (var font in Fonts.SystemFontFamilies)
            {
                FontComboBox.Items.Add(font.Source);
            }
            if (FontComboBox.Items.Count > 0)
                FontComboBox.SelectedIndex = 0;

            // Заполняем размеры шрифтов
            int[] sizes = { 8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72 };
            foreach (var s in sizes)
                SizeComboBox.Items.Add(s.ToString());
            SizeComboBox.SelectedIndex = 3; // 14

            // Подписываемся на события для обновления предпросмотра
            FontComboBox.SelectionChanged += (s, e) => UpdatePreview();
            SizeComboBox.SelectionChanged += (s, e) => UpdatePreview();
            TextBox.TextChanged += (s, e) => UpdatePreview();

            // Устанавливаем фокус на текстовое поле
            TextBox.Focus();
        }

        // Обновление предпросмотра текста
        private void UpdatePreview()
        {
            try
            {
                // Получаем выбранный шрифт и размер
                string font = FontComboBox.SelectedItem as string ?? "Segoe UI";
                double size = 14;
                if (SizeComboBox.Text != null)
                    double.TryParse(SizeComboBox.Text, out size);

                // Применяем настройки к предпросмотру
                PreviewBlock.FontFamily = new FontFamily(font);
                PreviewBlock.FontSize = Math.Max(size, 6);
                PreviewBlock.Text = string.IsNullOrEmpty(TextBox.Text) ? "Предпросмотр текста" : TextBox.Text;
            }
            catch { }
        }

        // Нажатие кнопки ОК
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем введенные данные
                EnteredText = TextBox.Text;
                SelectedFontFamily = FontComboBox.SelectedItem as string ?? "Segoe UI";
                if (!double.TryParse(SizeComboBox.Text, out double size) || size < 1)
                    size = 14;
                SelectedFontSize = size;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
