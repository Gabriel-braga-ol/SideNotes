using System;
using System.Globalization;
using System.Windows.Data;

namespace DearNotes.Views.Converters
{
    /// <summary>
    /// Liga os chips "Todas" e "No Desk" ao mesmo estado do ViewModel.
    /// </summary>
    public sealed class DeskFilterConverter : IValueConverter
    {
        private const string DeskParameter = "desk";

        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            bool showOnlyDesk = value is true;

            return IsDeskFilter(parameter)
                ? showOnlyDesk
                : !showOnlyDesk;
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is not true)
            {
                return Binding.DoNothing;
            }

            return IsDeskFilter(parameter);
        }

        private static bool IsDeskFilter(object? parameter)
        {
            return string.Equals(
                parameter as string,
                DeskParameter,
                StringComparison.OrdinalIgnoreCase);
        }

    }
}
