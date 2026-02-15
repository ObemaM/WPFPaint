using System.Windows;

namespace WPFPaint.Dialogs
{
    public partial class AboutDialog : Window
    {
        // Конструктор диалога о программе
        public AboutDialog()
        {
            InitializeComponent();
        }

        // Нажатие кнопки ОК
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем диалог
            Close();
        }
    }
}
