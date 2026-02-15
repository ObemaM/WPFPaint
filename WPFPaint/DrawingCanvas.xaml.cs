using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFPaint.Tools;

namespace WPFPaint
{
    // Рисование на растровом изображении
    public partial class DrawingCanvas : UserControl
    {
        private WriteableBitmap _bitmap;
        private ITool _currentTool;
        private bool _isModified;
        private string _filePath;
        private double _zoomLevel = 1.0;

        //Текущий растровый битмап - хранит изображение, на котором рисуем
        public WriteableBitmap Bitmap => _bitmap;

        // Цвет обводки (границы), по дефолту черный
        public Color StrokeColor { get; set; } = Colors.Black;

        // Цвет заливки, по дефолту белый
        public Color FillColor { get; set; } = Colors.Yellow;

        // Толщина пера, по дефолту 3 пикселя
        public int StrokeThickness { get; set; } = 3;

        // Рисование закрашенных фигур
        public bool IsFilled { get; set; } = false;

        // Флаг изменений (звездочка)
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                ModifiedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Путь к файлу
        public string FilePath
        {
            get => _filePath;
            set => _filePath = value;
        }

        // Масштаб
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                // Увеличение масштаба
                _zoomLevel = value;
                ZoomTransform.ScaleX = value;
                ZoomTransform.ScaleY = value;
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Управление активным инструментом
        public ITool CurrentTool
        {
            get => _currentTool;
            set
            {
                _currentTool = value;
                if (_currentTool != null)
                {
                    // OverlayCanvas ловит все события мыши, поэтому меняем курсор там
                    var overlayCanvas = OverlayCanvas;
                    if (overlayCanvas != null)
                        overlayCanvas.Cursor = _currentTool.GetCursor();
                }
            }
        }

        // Событие изменения флага модификации
        public event EventHandler ModifiedChanged;

        // Событие изменения масштаба
        public event EventHandler ZoomChanged;

        // Событие движения мыши (для статусбара)
        public event EventHandler<Point> MousePositionChanged;

        // Конструктор холста
        public DrawingCanvas()
        {
            InitializeComponent();
        }

        // Создаем новый пустой холст
        public void CreateNew(int width, int height)
        {
            try
            {
                // Создаем новый WriteableBitmap с заданными размерами и форматом пикселей
                _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

                // Предотвращает доступ к битмапу из других потоков и позволяет изменять пиксели
                _bitmap.Lock();
                try
                {
                    unsafe
                    {
                        // Указатель на начало буфера пикселей
                        IntPtr buffer = _bitmap.BackBuffer;

                        // Получаем шаг строки (кол-во байт на строку)
                        int stride = _bitmap.BackBufferStride;

                        // Создаем белый цвет в формате BGRA (255, 255, 255, 255
                        int white = (255 << 24) | (255 << 16) | (255 << 8) | 255;
                        
                        // Заполняем весь буфер белым цветом
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                *((int*)(buffer + y * stride + x * 4)) = white;
                            }
                        }
                    }
                    _bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                }
                finally
                {
                    // Разблокировка битмапа
                    _bitmap.Unlock();
                }

                // Заполняем значения для холста
                DisplayImage.Source = _bitmap;
                DisplayImage.Width = width;
                DisplayImage.Height = height;
                _filePath = null;
                
                // Новый длкумент не сохранен,
                IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания холста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузить изображение из файла
        public void LoadFromFile(string path)
        {
            try
            {
                // Создаем URI и загружаем изображение
                var uri = new Uri(path, UriKind.Absolute);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = uri;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                // Получаем размеры изображения
                int w = bitmapImage.PixelWidth;
                int h = bitmapImage.PixelHeight;

                // Создаем WriteableBitmap для рисования
                _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);

                // Копируем пиксели из загруженного изображения
                var converted = new FormatConvertedBitmap(bitmapImage, PixelFormats.Bgra32, null, 0);
                int[] pixels = new int[w * h];
                converted.CopyPixels(pixels, w * 4, 0);
                _bitmap.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);

                // Отображаем изображение в UI
                DisplayImage.Source = _bitmap;
                DisplayImage.Width = w;
                DisplayImage.Height = h;
                _filePath = path;
                IsModified = false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось загрузить файл: {ex.Message}", ex);
            }
        }

        // Сохранение изображения в файл
        public void SaveToFile(string path)
        {
            try
            {
                // Определяем формат файла по расширению
                string ext = System.IO.Path.GetExtension(path).ToLower();
                BitmapEncoder encoder;

                // Выбираем энкодер для формата
                switch (ext)
                {
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                        break;
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    default:
                        throw new NotSupportedException($"Формат '{ext}' не поддерживается.");
                }

                // Добавляем битмап в энкодер
                encoder.Frames.Add(BitmapFrame.Create(_bitmap));

                // Сохраняем в файл
                using (var stream = System.IO.File.Create(path))
                {
                    encoder.Save(stream);
                }

                // Обновляем состояние документа
                _filePath = path;
                IsModified = false;
            }
            catch (NotSupportedException) // Для неподдерживаемых форматов
            {
                throw;
            }
            catch (Exception ex) // Для всех остальных ошибок
            {
                throw new InvalidOperationException($"Не удалось сохранить файл: {ex.Message}", ex);
            }
        }

        // Изменение размера холста
        public void ResizeCanvas(int newWidth, int newHeight)
        {
            try
            {
                // Создаем новый битмап с новыми размерами
                var newBitmap = new WriteableBitmap(newWidth, newHeight, 96, 96, PixelFormats.Bgra32, null);
                
                // Заливаем новый холст белым цветом
                newBitmap.Lock();
                try
                {
                    unsafe
                    {
                        IntPtr buffer = newBitmap.BackBuffer;
                        int stride = newBitmap.BackBufferStride;
                        int white = (255 << 24) | (255 << 16) | (255 << 8) | 255;
                        for (int y = 0; y < newHeight; y++)
                        {
                            for (int x = 0; x < newWidth; x++)
                            {
                                *((int*)(buffer + y * stride + x * 4)) = white;
                            }
                        }
                    }
                    newBitmap.AddDirtyRect(new Int32Rect(0, 0, newWidth, newHeight));
                }
                finally
                {
                    newBitmap.Unlock();
                }

                // Определяем область копирования (минимальные размеры)
                int copyW = Math.Min(_bitmap.PixelWidth, newWidth);
                int copyH = Math.Min(_bitmap.PixelHeight, newHeight);
                int[] pixels = new int[copyW * copyH];

                // Копируем старое содержимое в новый холст
                _bitmap.CopyPixels(new Int32Rect(0, 0, copyW, copyH), pixels, copyW * 4, 0);
                newBitmap.WritePixels(new Int32Rect(0, 0, copyW, copyH), pixels, copyW * 4, 0);

                // Заменяем старый битмап новым
                _bitmap = newBitmap;
                DisplayImage.Source = _bitmap;
                DisplayImage.Width = newWidth;
                DisplayImage.Height = newHeight;
                IsModified = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения размера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обновление отображения битмапа
        public void InvalidateBitmap()
        {
            // Сбрасываем и заново устанавливаем источник для перерисовки
            DisplayImage.Source = null;
            DisplayImage.Source = _bitmap;
        }

        // Обработчики событий мыши

        // Нажатие кнопки мыши
        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверка на наличие инструмента и битмапа
            if (_currentTool == null || _bitmap == null) return;

            try
            {
                // Получаем позицию курсора и передаем инструменту
                var pos = e.GetPosition(OverlayCanvas);
                _currentTool.OnMouseDown(this, pos, e);
                // Захватываем мышь для отслеживания движения
                OverlayCanvas.CaptureMouse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse Down Error: {ex.Message}");
            }
        }

        // Движение мыши
        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Обновляем позицию курсора в статусной строке
            var pos = e.GetPosition(OverlayCanvas);
            MousePositionChanged?.Invoke(this, pos);

            // Проверка на наличие инструмента и битмапа
            if (_currentTool == null || _bitmap == null) return;

            try
            {
                // Передаем движение мыши инструменту
                _currentTool.OnMouseMove(this, pos, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse Move Error: {ex.Message}");
            }
        }

        // Отпускание кнопки мыши
        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Проверка на наличие инструмента и битмапа
            if (_currentTool == null || _bitmap == null) return;

            try
            {
                // Получаем позицию и передаем инструменту
                var pos = e.GetPosition(OverlayCanvas);
                _currentTool.OnMouseUp(this, pos, e);
                // Освобождаем захват мыши
                OverlayCanvas.ReleaseMouseCapture();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse Up Error: {ex.Message}");
            }
        }
    }
}
