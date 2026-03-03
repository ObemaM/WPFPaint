using System.Collections.Generic;
using System.Windows;

namespace WPFPaint.Dialogs
{
    /// <summary>
    /// Диалоговое окно управления плагинами.
    /// Позволяет просматривать список плагинов и включать/выключать их.
    /// </summary>
    public partial class PluginManagerDialog : Window
    {
        private readonly List<PluginInfo> _plugins;
        private bool _hasChanges = false;

        /// <summary>
        /// Были ли изменения в конфигурации.
        /// </summary>
        public bool HasChanges => _hasChanges;

        public PluginManagerDialog(List<PluginInfo> plugins)
        {
            InitializeComponent();
            _plugins = plugins;
            PluginListView.ItemsSource = _plugins;
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _hasChanges = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
