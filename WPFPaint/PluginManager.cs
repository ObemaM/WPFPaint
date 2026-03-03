using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using PluginInterface;

namespace WPFPaint
{
    // Информация о плагине для конфигурационного файла и UI
    public class PluginInfo
    {
        // Путь к DLL-файлу плагина (относительный)
        public string DllPath { get; set; }

        // Полное имя типа плагина (namespace.class)
        public string TypeName { get; set; }

        // Название плагина
        public string Name { get; set; }

        // Автор плагина
        public string Author { get; set; }

        // Версия плагина (из атрибута VersionAttribute)
        public string Version { get; set; }

        // Загружать ли плагин
        public bool IsEnabled { get; set; } = true;

        // Экземпляр плагина (не сериализуется)
        [System.Text.Json.Serialization.JsonIgnore]
        public IPlugin Instance { get; set; }
    }

    // Конфигурация плагинов (сериализуется в JSON)
    public class PluginConfig
    {
        // Автоматический режим: загружать все плагины из директории
        public bool AutoMode { get; set; } = true;

        // Список плагинов
        public List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();
    }

    // Менеджер плагинов — загрузка через рефлексию, работа с конфигурационным файлом
    public class PluginManager
    {
        private readonly string _pluginsFolder;
        private readonly string _configPath;
        private PluginConfig _config;

        // Все обнаруженные плагины (включая отключенные)
        public List<PluginInfo> AllPlugins { get; private set; } = new List<PluginInfo>();

        // Только включённые плагины (для меню)
        public List<PluginInfo> EnabledPlugins =>
            AllPlugins.Where(p => p.IsEnabled && p.Instance != null).ToList();

        // Создаёт менеджер плагинов
        public PluginManager()
        {
            // Папка Plugins рядом с exe
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _pluginsFolder = Path.Combine(baseDir, "Plugins");
            _configPath = Path.Combine(baseDir, "plugins.json");
        }

        // Инициализация: загрузка конфига и поиск плагинов
        public void Initialize()
        {
            // Создаём папку Plugins если нет
            if (!Directory.Exists(_pluginsFolder))
                Directory.CreateDirectory(_pluginsFolder);

            // Загружаем конфигурацию
            _config = LoadConfig();

            // Сканируем все DLL и находим плагины
            var foundPlugins = ScanForPlugins();

            if (!File.Exists(_configPath) || _config.AutoMode)
            {
                // Автоматический режим — все найденные плагины включены
                AllPlugins = foundPlugins;
                foreach (var p in AllPlugins)
                    p.IsEnabled = true;
            }
            else
            {
                // Ручной режим — объединяем конфиг с найденными плагинами
                AllPlugins = MergeWithConfig(foundPlugins, _config);
            }

            // Инстанцируем включённые плагины
            foreach (var p in AllPlugins)
            {
                if (p.IsEnabled && p.Instance == null)
                    p.Instance = InstantiatePlugin(p);
            }

            // Сохраняем конфиг (создаём если нет)
            SaveConfig();
        }

        // Сканирует папку Plugins на наличие DLL с плагинами
        private List<PluginInfo> ScanForPlugins()
        {
            var result = new List<PluginInfo>();

            if (!Directory.Exists(_pluginsFolder))
                return result;

            string[] files = Directory.GetFiles(_pluginsFolder, "*.dll");

            foreach (string file in files)
            {
                try
                {
                    // Загружаем сборку (DLL) в память
                    Assembly assembly = Assembly.LoadFrom(file);

                    foreach (Type type in assembly.GetTypes())
                    {
                        // Проверяем, реализует ли тип интерфейс IPlugin
                        Type iface = type.GetInterface("PluginInterface.IPlugin");

                        if (iface != null)
                        {
                            // Создаём экземпляр плагина через рефлексию
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

                            // Получаем атрибут версии через рефлексию
                            var versionAttr = type.GetCustomAttribute<VersionAttribute>();
                            string version = versionAttr != null ? versionAttr.ToString() : "—";

                            result.Add(new PluginInfo
                            {
                                DllPath = Path.GetFileName(file),
                                TypeName = type.FullName,
                                Name = plugin.Name,
                                Author = plugin.Author,
                                Version = version,
                                IsEnabled = true,
                                Instance = plugin
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки плагина из {file}: {ex.Message}");
                }
            }

            return result;
        }

        // Создаёт экземпляр плагина по его информации (используется при Reload)
        private IPlugin InstantiatePlugin(PluginInfo info)
        {
            try
            {
                string dllPath = Path.Combine(_pluginsFolder, info.DllPath);
                if (!File.Exists(dllPath)) return null;

                // Загружаем DLL и создаём экземпляр типа
                Assembly assembly = Assembly.LoadFrom(dllPath);
                Type type = assembly.GetType(info.TypeName);
                if (type == null) return null;

                return (IPlugin)Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        // Объединяет найденные плагины с сохранённой конфигурацией
        private List<PluginInfo> MergeWithConfig(List<PluginInfo> found, PluginConfig config)
        {
            var result = new List<PluginInfo>();

            foreach (var fp in found)
            {
                // Ищем плагин в конфиге по DLL и типу
                var cfgEntry = config.Plugins.FirstOrDefault(
                    c => c.DllPath == fp.DllPath && c.TypeName == fp.TypeName);

                if (cfgEntry != null)
                {
                    fp.IsEnabled = cfgEntry.IsEnabled; // Берём настройку из конфига
                }
                // Если плагина нет в конфиге — он новый, включён по умолчанию

                result.Add(fp);
            }

            return result;
        }

        // Загружает конфигурационный файл plugins.json
        private PluginConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения конфига: {ex.Message}");
            }

            // Если файла нет или ошибка — возвращаем режим Auto
            return new PluginConfig { AutoMode = true };
        }

        // Сохраняет текущую конфигурацию в файл plugins.json
        public void SaveConfig()
        {
            try
            {
                _config = new PluginConfig
                {
                    AutoMode = false, // После первого запуска — ручной режим
                    Plugins = AllPlugins.Select(p => new PluginInfo
                    {
                        DllPath = p.DllPath,
                        TypeName = p.TypeName,
                        Name = p.Name,
                        Author = p.Author,
                        Version = p.Version,
                        IsEnabled = p.IsEnabled
                    }).ToList()
                };

                // Сериализация в JSON с отступами для читаемости
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения конфига: {ex.Message}");
            }
        }

        // Перезагружает плагины после изменения конфигурации в диалоге
        public void Reload()
        {
            // Включаем/выключаем плагины по флагу IsEnabled
            foreach (var p in AllPlugins)
            {
                if (p.IsEnabled && p.Instance == null)
                    p.Instance = InstantiatePlugin(p); // Загружаем включённые
                else if (!p.IsEnabled)
                    p.Instance = null; // Выгружаем отключённые
            }

            SaveConfig(); // Сохраняем изменения в файл
        }
    }
}
