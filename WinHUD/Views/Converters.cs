using System.Globalization;
using System.Windows.Data;
using WinHUD.Models;
using WinHUD.Models.Nodes;

namespace WinHUD.Views.Converters
{
    public static class FormatHelper
    {
        public static string FormatSpeed(float bytes)
        {
            // PadLeft forces the string to always take up 10 character slots!
            if (bytes < 1024) return $"{bytes:F0} B/s".PadLeft(10);
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB/s".PadLeft(10);
            return $"{bytes / 1024 / 1024:F1} MB/s".PadLeft(10);
        }
    }

    public class DiskSpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is HardwareData d ? $"↓ {FormatHelper.FormatSpeed(d.DiskReadBytesPerSec)}  ↑ {FormatHelper.FormatSpeed(d.DiskWriteBytesPerSec)}" : "";

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class NetSpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is HardwareData d ? $"↓ {FormatHelper.FormatSpeed(d.NetDownloadBytesPerSec)}  ↑ {FormatHelper.FormatSpeed(d.NetUploadBytesPerSec)}" : "";

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class DiskListConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Dictionary<string, float> dict && values[1] is LayoutDirection dir)
            {
                string separator = dir == LayoutDirection.Horizontal ? "   |   " : "\n";
                return string.Join(separator, dict.Select(kv => $"Disk {kv.Key} - {kv.Value:F0}%"));
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
