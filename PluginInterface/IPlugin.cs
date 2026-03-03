using System;
using System.Threading;

namespace PluginInterface
{
    // Интерфейс плагина для фильтров изображения
    // Плагины реализуют этот интерфейс для подключения к MDI Paint
    public interface IPlugin
    {
        // Название плагина (отображается в меню «Фильтры»)
        string Name { get; }

        // Автор плагина
        string Author { get; }

        // Применяет фильтр к массиву пикселей изображения (формат BGRA32). Каждый пиксель занимает 4 байта: [B, G, R, A].
        // Метод вызывается в отдельном потоке
        
        // Параметры:
        // pixelData — массив пикселей в формате BGRA32 (4 байта на пиксель)
        // width — ширина изображения в пикселях
        // height — высота изображения в пикселях
        // progress — объект для отчётов о прогрессе (0–100%)
        // cancellationToken — токен для отмены операции
        void Transform(byte[] pixelData, int width, int height,
                       IProgress<int> progress, CancellationToken cancellationToken);
    }
}
