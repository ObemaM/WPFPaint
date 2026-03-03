using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using PluginInterface;

namespace WPFPaint.Dialogs
{
    /// <summary>
    /// Диалоговое окно прогресса применения фильтра.
    /// Выполняет фильтр в отдельном потоке с отображением прогресса и возможностью отмены.
    /// </summary>
    public partial class FilterProgressDialog : Window
    {
        private readonly IPlugin _plugin;
        private readonly WriteableBitmap _bitmap;
        private CancellationTokenSource _cts;
        private bool _isCompleted = false;
        private bool _isCancelled = false;

        /// <summary>
        /// Обработанные данные пикселей (результат фильтра).
        /// </summary>
        public byte[] ResultPixels { get; private set; }

        /// <summary>
        /// Была ли операция отменена.
        /// </summary>
        public bool WasCancelled => _isCancelled;

        public FilterProgressDialog(IPlugin plugin, WriteableBitmap bitmap)
        {
            InitializeComponent();
            _plugin = plugin;
            _bitmap = bitmap;
            StatusText.Text = $"Выполнение: {plugin.Name}...";

            // Запускаем фильтр сразу после загрузки окна
            Loaded += async (s, e) => await RunFilterAsync();
        }

        /// <summary>
        /// Асинхронное выполнение фильтра в отдельном потоке.
        /// </summary>
        private async Task RunFilterAsync()
        {
            _cts = new CancellationTokenSource();

            // Копируем пиксели из WriteableBitmap
            int width = _bitmap.PixelWidth;
            int height = _bitmap.PixelHeight;
            int stride = width * 4; // BGRA32
            byte[] pixels = new byte[height * stride];
            _bitmap.CopyPixels(pixels, stride, 0);

            // Прогресс на UI-потоке
            var progress = new Progress<int>(percent =>
            {
                ProgressBar.Value = percent;
                StatusText.Text = $"Выполнение: {_plugin.Name}... {percent}%";
            });

            try
            {
                // Выполняем фильтр в фоновом потоке
                await Task.Run(() =>
                {
                    _plugin.Transform(pixels, width, height, progress, _cts.Token);
                }, _cts.Token);

                ResultPixels = pixels;
                _isCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (OperationCanceledException)
            {
                _isCancelled = true;
                _isCompleted = true;
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                _isCompleted = true;
                MessageBox.Show($"Ошибка выполнения фильтра: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            CancelButton.IsEnabled = false;
            StatusText.Text = "Отмена...";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Запрещаем закрытие пока фильтр работает (только через кнопку Отмена)
            if (!_isCompleted)
            {
                _cts?.Cancel();
                e.Cancel = true;
            }
        }
    }
}
