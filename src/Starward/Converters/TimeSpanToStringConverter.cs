using Microsoft.UI.Xaml.Data;
using Starward.Language;
using System;

namespace Starward.Converters;

internal partial class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan time)
        {
            return LocalizedTimeFormatter.FormatHoursMinutes(time);
        }
        else
        {
            return null!;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
