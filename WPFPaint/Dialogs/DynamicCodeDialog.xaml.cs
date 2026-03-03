using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WPFPaint.Dialogs
{
    // Диалоговое окно для ввода пользовательского C#-кода фильтра и его применения
    public partial class DynamicCodeDialog : Window
    {
        // Скомпилированный массив пикселей (результат)
        public byte[] ResultPixels { get; private set; }

        // Ширина исходного изображения
        public int ImageWidth { get; set; }

        // Высота исходного изображения
        public int ImageHeight { get; set; }

        // Исходные данные пикселей
        public byte[] SourcePixels { get; set; }

        public DynamicCodeDialog()
        {
            InitializeComponent();
        }

        // Применяет пользовательский код через динамическую компиляцию
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            string userCode = CodeTextBox.Text;

            // Оборачиваем пользовательский код в класс с методом
            string fullCode = @"
using System;

public class DynamicFilter
{
    public static void Apply(byte[] pixelData, int width, int height)
    {
" + userCode + @"
    }
}";

            try
            {
                // Компиляция с помощью Roslyn
                var syntaxTree = CSharpSyntaxTree.ParseText(fullCode);

                // Ссылки на необходимые сборки
                var references = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Math).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(
                        Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll"))
                };

                var compilation = CSharpCompilation.Create(
                    "DynamicFilterAssembly",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    var result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        // Показываем ошибки компиляции
                        var errors = result.Diagnostics
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Select(d => d.GetMessage());
                        ErrorText.Text = "Ошибки компиляции:\n" + string.Join("\n", errors);
                        return;
                    }

                    // Загружаем скомпилированную сборку
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    var type = assembly.GetType("DynamicFilter");
                    var method = type.GetMethod("Apply");

                    // Копируем пиксели для обработки
                    byte[] pixels = new byte[SourcePixels.Length];
                    Array.Copy(SourcePixels, pixels, SourcePixels.Length);

                    // Вызываем скомпилированный метод
                    method.Invoke(null, new object[] { pixels, ImageWidth, ImageHeight });

                    ResultPixels = pixels;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Ошибка выполнения: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
