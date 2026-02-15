using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using AvalonDock.Layout;
using WPFPaint.Tools;

namespace WPFPaint
{
    public partial class MainWindow
    {
        // Горячие клавиши для основных команд
        public static readonly RoutedCommand NewCommand = new RoutedCommand();
        public static readonly RoutedCommand OpenCommand = new RoutedCommand();
        public static readonly RoutedCommand SaveCommand = new RoutedCommand();
        public static readonly RoutedCommand SaveAsCommand = new RoutedCommand();
        public static readonly RoutedCommand ResizeCanvasCommand = new RoutedCommand();

        // Инструменты

        // ITool - интерфейс для всех инструментов
        private ITool _penTool = new PenTool();
        private ITool _lineTool = new LineTool();
        private ITool _ellipseTool = new EllipseTool();
        private ITool _eraserTool = new EraserTool();
        private ITool _textTool = new TextTool();
        private ITool _bucketFillTool = new BucketFillTool();
        private ITool _calloutTool = new CalloutTool();

        // Текущий активный инструмент
        private ITool _currentTool;

        // Цвета
        private Color _strokeColor = Colors.Black;
        private Color _fillColor = Colors.Yellow;

        // Свойства для рисования
        private int _strokeThickness = 3;
        private bool _isFilled = false;
        private int _newDocCounter = 0;

        public MainWindow()
        {
            InitializeComponent();

            // CommandBindings для горячих клавиш
            CommandBindings.Add(new CommandBinding(NewCommand, (s, e) => NewFile_Click(s, null)));
            CommandBindings.Add(new CommandBinding(OpenCommand, (s, e) => OpenFile_Click(s, null)));
            CommandBindings.Add(new CommandBinding(SaveCommand, (s, e) => SaveFile_Click(s, null)));
            CommandBindings.Add(new CommandBinding(SaveAsCommand, (s, e) => SaveFileAs_Click(s, null)));
            CommandBindings.Add(new CommandBinding(ResizeCanvasCommand, (s, e) => ResizeCanvas_Click(s, null)));

            // Подписка на события клавиатуры для инструментов
            this.KeyDown += MainWindow_KeyDown;

            // Изначально активный инструмент — перо
            _currentTool = _penTool;

            // Обновляет текст статусной строки для текущего инструмента
            UpdateStatusTool();
        }

        // Текущий активный документ (получаем из AvalonDock)
        private DrawingCanvas ActiveCanvas
        {
            get
            {
                // Проверяет, что DockManager и Layout уже есть
                if (DockManager?.Layout == null) return null;

                // Ищем активный документ в AvalonDock
                var activeDoc = DockManager.Layout.Descendents()
                    .OfType<LayoutDocument>()
                    .FirstOrDefault(d => d.IsActive || d.IsSelected);

                // Безопасная проверка на наличие контента на документе
                return activeDoc?.Content as DrawingCanvas;
            }
        }

        // (object sender, KeyEventArgs e) - стандартные параметры обработчиков событий 

        // Горячие клавиши инструментов
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Если нет открытого документа, не обрабатываем горячие клавиши инструментов
            if (ActiveCanvas == null) return;

            // Обработка клавиш для инструментов
            switch (e.Key)
            {
                // Назначение клавиши для действия
                case Key.P:
                    ToolPen_Click(null, null);
                    BtnPen.IsChecked = true;
                    break;
                case Key.L:
                    ToolLine_Click(null, null);
                    BtnLine.IsChecked = true;
                    break;
                case Key.E:
                    ToolEllipse_Click(null, null);
                    BtnEllipse.IsChecked = true;
                    break;
                case Key.R:
                    ToolEraser_Click(null, null);
                    BtnEraser.IsChecked = true;
                    break;
                case Key.T:
                    ToolText_Click(null, null);
                    BtnText.IsChecked = true;
                    break;
                case Key.B:
                    ToolBucket_Click(null, null);
                    BtnBucket.IsChecked = true;
                    break;
                case Key.C:
                    ToolCallout_Click(null, null);
                    BtnCallout.IsChecked = true;
                    break;
                case Key.OemPlus:
                    ToolZoomIn_Click(null, null);
                    break;
                case Key.OemMinus:
                    ToolZoomOut_Click(null, null);
                    break;
            }
        }

        // Управление состояния UI
        private void UpdateUIState()
        {
            // Безопасная проверка на наличие элементов UI
            if (DockManager == null) return;

            // Проверяем, есть ли открытый документ
            bool hasDoc = ActiveCanvas != null;

            // Кнопка сохранить
            BtnSave.IsEnabled = hasDoc;

            // Создаем инструменты, которые зависят от наличия открытого документа
            BtnPen.IsEnabled = hasDoc;
            BtnLine.IsEnabled = hasDoc;
            BtnEllipse.IsEnabled = hasDoc;
            BtnEraser.IsEnabled = hasDoc;
            BtnText.IsEnabled = hasDoc;
            BtnBucket.IsEnabled = hasDoc;
            BtnCallout.IsEnabled = hasDoc;
            BtnZoomIn.IsEnabled = hasDoc;
            BtnZoomOut.IsEnabled = hasDoc;
            BtnResize.IsEnabled = hasDoc;

            // Если есть открытый документ, то добавляем строку состояния
            if (hasDoc)
            {
                var canvas = ActiveCanvas;
                StatusSize.Text = $"Размер: {canvas.Bitmap.PixelWidth} × {canvas.Bitmap.PixelHeight}";
                StatusZoom.Text = $"Масштаб: {(int)(canvas.ZoomLevel * 100)}%";
            }
            else // Просто держим статусную строку с дефолтными значениями
            {
                StatusPosition.Text = "Позиция: —";
                StatusSize.Text = "Размер: —";
                StatusZoom.Text = "Масштаб: 100%";
            }

            // Обновляем статус текущего инструмента
            UpdateStatusTool();
        }

        // Обновление статуса текущего инструмента в статусной строке
        private void UpdateStatusTool()
        {
            StatusTool.Text = _currentTool != null ? $"Инструмент: {_currentTool.ToolName}" : "Инструмент: —";
        }

        // Применяет текущие настройки к новому холсту и добавляет его
        private void AddDocumentToPane(DrawingCanvas canvas, string title)
        {
            // Применение текущих настроек к новому холсту
            canvas.CurrentTool = _currentTool;
            canvas.StrokeColor = _strokeColor;
            canvas.FillColor = _fillColor;
            canvas.StrokeThickness = _strokeThickness;
            canvas.IsFilled = _isFilled;

            // Подписка на события холста для обновления UI
            canvas.MousePositionChanged += Canvas_MousePositionChanged;
            canvas.ModifiedChanged += Canvas_ModifiedChanged;
            canvas.ZoomChanged += Canvas_ZoomChanged;

            // Создание LayoutDocument для AvalonDock
            var layoutDoc = new LayoutDocument
            {
                Title = title,
                Content = canvas
            };

            // Добавление документа в панель и установка его как активного
            DocumentPane.Children.Add(layoutDoc);
            layoutDoc.IsActive = true;
            UpdateUIState();
        }

        // Создание нового документа
        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Счетчик для именования новых документов (Новый 1, Новый 2 и т.д.)
                _newDocCounter++;

                // Создание холста
                var canvas = new DrawingCanvas();
                canvas.CreateNew(1400, 1200); // Размер холста

                // Добавление в AvalonDock
                AddDocumentToPane(canvas, $"Новый {_newDocCounter}");
            }
            catch (Exception ex) // Обработка ошибок при создании нового документа
            {
                MessageBox.Show($"Ошибка создания документа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Открытие файла
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Диалог выбора окна
                var dlg = new OpenFileDialog
                {
                    Title = "Открыть изображение",
                    Filter = "Все изображения|*.bmp;*.jpg;*.jpeg;*.png|BMP файлы (*.bmp)|*.bmp|JPEG файлы (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG файлы (*.png)|*.png",
                    FilterIndex = 1 // По умолчанию все изображения
                };

                // Если пользователь открывает какой-либо файл
                if (dlg.ShowDialog() == true)
                {
                    var canvas = new DrawingCanvas();
                    canvas.LoadFromFile(dlg.FileName); // Загружает изображение из файла

                    // Берем имя файла
                    string fileName = System.IO.Path.GetFileName(dlg.FileName);

                    // Добавление в AvalonDock
                    AddDocumentToPane(canvas, fileName);
                }
            }
            catch (Exception ex) // Обработка ошибок при открытии файла
            {
                MessageBox.Show($"Ошибка открытия файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранение текущего документа
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var canvas = ActiveCanvas;
                if (canvas == null) return; // Без открытого документа сохранять нечего

                // Если файл уже был сохранён ранее, сохраняем по тому же пути
                if (string.IsNullOrEmpty(canvas.FilePath))
                {
                    // Первый раз — запрашиваем имя файла
                    SaveFileAs_Click(sender, e);
                }
                else
                {
                    canvas.SaveToFile(canvas.FilePath);
                    UpdateTabHeader(); // Обновляем заголовок вкладки, чтобы убрать звёздочку
                }
            }
            catch (Exception ex) // Обработка ошибок при сохранении файла
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Сохранить как
        private void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var canvas = ActiveCanvas;
                if (canvas == null) return; // Без открытого документа сохранять нечего

                // Диалог сохранения файла
                var dlg = new SaveFileDialog
                {
                    Title = "Сохранить изображение как",
                    Filter = "BMP файл (*.bmp)|*.bmp|JPEG файл (*.jpg)|*.jpg|PNG файл (*.png)|*.png",
                    FilterIndex = 1,
                    FileName = string.IsNullOrEmpty(canvas.FilePath)
                        ? "Изображение" // Дефолтное название для сохранения нового документа
                        : System.IO.Path.GetFileNameWithoutExtension(canvas.FilePath)
                };

                // Показывает диалог и сохраняет файл
                if (dlg.ShowDialog() == true)
                {
                    canvas.SaveToFile(dlg.FileName);
                    UpdateTabHeader();
                }
            }
            catch (Exception ex) // Обработка ошибок при сохранении файла
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Выход из приложения
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Проверяем все открытые документы перед закрытием приложения
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Получаем все документы из AvalonDock
            var docs = DockManager?.Layout?.Descendents()
                .OfType<LayoutDocument>()
                .ToList();

            if (docs == null) return;

            // Проверяем все открытые документы
            foreach (var doc in docs)
            {
                var canvas = doc.Content as DrawingCanvas;

                // Проверка модифицированности документа
                if (canvas != null && canvas.IsModified)
                {
                    // Делаем документ активным для показа пользователю
                    doc.IsActive = true;
                    string name = doc.Title.TrimEnd('*');
                    var result = MessageBox.Show(
                        $"Сохранить изменения в \"{name}\"?",
                        "Сохранение",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveFile_Click(null, null);
                        if (canvas.IsModified) // Если после сохранения всё ещё модифицирована (пользователь отменил save dialog)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    // Если пользователь выбрал "Отмена", просто закрываем без сохранения
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        // Обработка закрытия отдельного документа через AvalonDock
        private void DockManager_DocumentClosing(object sender, AvalonDock.DocumentClosingEventArgs e)
        {
            var canvas = e.Document.Content as DrawingCanvas;
            if (canvas != null && canvas.IsModified)
            {
                string name = e.Document.Title.TrimEnd('*');
                var result = MessageBox.Show(
                    $"Сохранить изменения в \"{name}\"?",
                    "Сохранение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Временно устанавливаем как активный для сохранения
                    var prevDoc = ActiveCanvas;
                    e.Document.IsActive = true;
                    SaveFile_Click(null, null);
                    if (canvas.IsModified) // Пользователь отменил SaveAs
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        // Применяет текущие настройки к новому активному документу
        private void DockManager_ActiveContentChanged(object sender, EventArgs e)
        {
            var canvas = ActiveCanvas;
            if (canvas != null)
            {
                canvas.CurrentTool = _currentTool;
                canvas.StrokeColor = _strokeColor;
                canvas.FillColor = _fillColor;
                canvas.StrokeThickness = _strokeThickness;
                canvas.IsFilled = _isFilled;
            }
            UpdateUIState();
        }

        // Установка инструментов
        private void SetTool(ITool tool)
        {
            // Смена текущего инструмента
            _currentTool = tool;
            var canvas = ActiveCanvas;

            // Безопасная проверка на наличие открытого документа
            if (canvas != null)
                canvas.CurrentTool = tool;
            UpdateStatusTool();
        }


        // Обработчики кликов по кнопкам инструментов
        // => для однострочных методов, которые просто вызывают SetTool с нужным инструментом
        private void ToolPen_Click(object sender, RoutedEventArgs e) => SetTool(_penTool);
        private void ToolLine_Click(object sender, RoutedEventArgs e) => SetTool(_lineTool);
        private void ToolEllipse_Click(object sender, RoutedEventArgs e) => SetTool(_ellipseTool);
        private void ToolEraser_Click(object sender, RoutedEventArgs e) => SetTool(_eraserTool);
        private void ToolText_Click(object sender, RoutedEventArgs e) => SetTool(_textTool);
        private void ToolBucket_Click(object sender, RoutedEventArgs e) => SetTool(_bucketFillTool);
        private void ToolCallout_Click(object sender, RoutedEventArgs e) => SetTool(_calloutTool);

        // Обработка нажатий для увеличения масштаба
        private void ToolZoomIn_Click(object sender, RoutedEventArgs e)
        {
            var canvas = ActiveCanvas;
            if (canvas != null)
            {
                double newZoom = Math.Min(canvas.ZoomLevel + 0.25, 5);
                canvas.ZoomLevel = newZoom;
            }
        }

        // Обработка горячей клавиши для уменьшения масштаба
        private void ToolZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var canvas = ActiveCanvas;
            if (canvas != null)
            {
                double newZoom = Math.Max(canvas.ZoomLevel - 0.25, 0.25);
                canvas.ZoomLevel = newZoom;
            }
        }

        // Цвет пера
        private void StrokeColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Создание диалога выбора цвета и установка текущего цвета как начального
                var dlg = new System.Windows.Forms.ColorDialog();
                dlg.Color = System.Drawing.Color.FromArgb(_strokeColor.A, _strokeColor.R, _strokeColor.G, _strokeColor.B);

                // Если пользователь выбрал цвет, сохраняем его и применяем к текущему документу
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Конвертируем обратно в WPF Color
                    _strokeColor = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);

                    // Обновляем цвет на UI
                    StrokeColorBorder.Background = new SolidColorBrush(_strokeColor);

                    // Применяем к текущему холсту, если он есть
                    if (ActiveCanvas != null)
                        ActiveCanvas.StrokeColor = _strokeColor;
                }
            }
            catch (Exception ex) // Обработка ошибок при выборе цвета
            {
                MessageBox.Show($"Ошибка выбора цвета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Цвет заливки
        private void FillColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Создание диалога выбора цвета и установка текущего цвета как начального
                var dlg = new System.Windows.Forms.ColorDialog();
                dlg.Color = System.Drawing.Color.FromArgb(_fillColor.A, _fillColor.R, _fillColor.G, _fillColor.B);

                // Если пользователь выбрал цвет, сохраняем его и применяем к текущему документу
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Конвертируем обратно в WPF Color
                    _fillColor = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);

                    // Обновляем цвет на UI
                    FillColorBorder.Background = new SolidColorBrush(_fillColor);

                    // Применяем к текущему холсту, если он есть
                    if (ActiveCanvas != null)
                        ActiveCanvas.FillColor = _fillColor;
                }
            }
            catch (Exception ex) // Обработка ошибок при выборе цвета
            {
                MessageBox.Show($"Ошибка выбора цвета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Толщина пера
        private void ThicknessCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Проверка, что объект выбран и его можно преобразовать в ComboBoxItem
            if (ThicknessCombo.SelectedItem is ComboBoxItem item)
            {
                // Парсинг значения для отображения в блоке
                if (int.TryParse(item.Content.ToString(), out int t))
                {
                    // Смена глобального значения
                    _strokeThickness = t;

                    // Смена толщины на текущем холсте
                    if (ActiveCanvas != null)
                        ActiveCanvas.StrokeThickness = t;
                }
            }
        }

        // Заливка фигур
        private void FilledCheck_Changed(object sender, RoutedEventArgs e)
        {
            // Обработка переключателя заливки
            _isFilled = FilledCheckBox.IsChecked == true;
            if (ActiveCanvas != null)
                ActiveCanvas.IsFilled = _isFilled;
        }

        // Изменение размера холста
        private void ResizeCanvas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка наличия открытого документа
                var canvas = ActiveCanvas;
                if (canvas == null) return;

                // Если документ есть, открываем диалог изменения размера холста, передавая текущие размеры
                var dlg = new Dialogs.CanvasResizeDialog(canvas.Bitmap.PixelWidth, canvas.Bitmap.PixelHeight);
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                {
                    // Смена размера холста на новый, указанный пользователем
                    canvas.ResizeCanvas(dlg.CanvasWidth, dlg.CanvasHeight);
                    UpdateUIState();
                }
            }
            catch (Exception ex) // Обработка ошибок при изменении размера холста
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // О программе
        private void About_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Dialogs.AboutDialog();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        // Обновляет позицию курсора в статусной строке
        private void Canvas_MousePositionChanged(object sender, Point pos)
        {
            StatusPosition.Text = $"Позиция: {(int)pos.X}, {(int)pos.Y}";
        }

        // Обновляет заголовок вкладки при изменении документа
        private void Canvas_ModifiedChanged(object sender, EventArgs e)
        {
            UpdateTabHeader();
        }

        // Изменяет информацию о масштабе в статусной строке при изменении зума
        private void Canvas_ZoomChanged(object sender, EventArgs e)
        {
            var canvas = ActiveCanvas;
            if (canvas != null)
                StatusZoom.Text = $"Масштаб: {(int)(canvas.ZoomLevel * 100)}%";
        }

        // Обновляет заголовок LayoutDocument в зависимости от состояния IsModified
        private void UpdateTabHeader()
        {
            // Безопасная проверка на наличие DockManager
            if (DockManager?.Layout == null) return;

            // Находим активный документ в иерархии AvalonDock
            var activeDoc = DockManager.Layout.Descendents()
                .OfType<LayoutDocument>()
                .FirstOrDefault(d => d.IsActive || d.IsSelected);

            if (activeDoc == null) return;
            var canvas = activeDoc.Content as DrawingCanvas;
            if (canvas == null) return;

            // Определяем базовое имя файла
            string name;
            if (!string.IsNullOrEmpty(canvas.FilePath))
                name = System.IO.Path.GetFileName(canvas.FilePath);
            else
                name = activeDoc.Title.TrimEnd('*');

            // Добавляем звездочку если документ изменен
            activeDoc.Title = canvas.IsModified ? name + "*" : name;
        }
    }
}
