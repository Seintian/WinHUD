using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using WinHUD.Models;

namespace WinHUD.Views.Converters
{
    public static class FormatHelper
    {
        public static string FormatSpeed(float bytes)
        {
            if (bytes < 1024) return $"{bytes:F0} B/s";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB/s";
            return $"{bytes / 1024 / 1024:F1} MB/s";
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

    public class DiskListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Dictionary<string, float> dict ? string.Join("\n", dict.Select(kv => $"Disk {kv.Key} - {kv.Value:F0}%")) : "";

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
