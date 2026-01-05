using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TestArmMonobrick.Converters;

/// <summary>
/// Converts boolean to Brush color based on parameter format "TrueColor;FalseColor"
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            var parts = paramStr.Split(';');
            if (parts.Length == 2)
            {
                var colorName = boolValue ? parts[0] : parts[1];
                return colorName.ToLower() switch
                {
                    "green" => Brushes.Green,
                    "gray" or "grey" => Brushes.Gray,
                    "orange" => Brushes.Orange,
                    "red" => Brushes.Red,
                    _ => Brushes.Black
                };
            }
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to text based on parameter format "TrueText;FalseText"
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramStr)
        {
            var parts = paramStr.Split(';');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
