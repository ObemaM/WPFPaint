using System;

namespace PluginInterface
{
    // Пользовательский атрибут для хранения версии плагина
    // Используется для получения информации о версии через рефлексию
    [AttributeUsage(AttributeTargets.Class)]
    public class VersionAttribute : Attribute
    {
        // Основная версия
        public int Major { get; private set; }

        // Дополнительная версия
        public int Minor { get; private set; }

        // Создаёт атрибут версии
        // major — основная версия
        // minor — дополнительная версия
        public VersionAttribute(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        public override string ToString() => $"{Major}.{Minor}";
    }
}
