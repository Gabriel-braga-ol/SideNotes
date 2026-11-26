using System;
using System.Globalization;
using System.Windows.Data;

namespace DearNotes.Views.Converters
{
    /// <summary>
    /// Mostra o título na aba vertical do Desk em maiúsculas e em uma única
    /// linha, como etiqueta de pasta.
    /// </summary>
    public sealed class TabLabelConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is not string title)
            {
                return string.Empty;
            }

            string[] words = title.Split(
                default(char[]),
                StringSplitOptions.RemoveEmptyEntries);

            return string.Join(' ', words).ToUpper(culture);
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
