using System;
using System.Globalization;
using System.Windows.Data;

namespace DearNotes.Views.Converters
{
    /// <summary>
    /// Reduz o conteúdo a uma linha na lista, sem quebras nem espaços repetidos.
    /// </summary>
    public sealed class PreviewTextConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is not string text)
            {
                return string.Empty;
            }

            string[] words = text.Split(
                default(char[]),
                StringSplitOptions.RemoveEmptyEntries);

            return string.Join(' ', words);
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
