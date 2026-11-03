using System.Globalization;
using System.Text;
using PulseMon.Models;

namespace PulseMon.Services;

public sealed class CsvLogService
{
    public void Export(string filePath, IReadOnlyList<SystemStatus> samples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var builder = new StringBuilder();
        builder.AppendLine("UpdatedAt,CpuUsagePercent,MemoryUsedGb,MemoryTotalGb,GpuUsagePercent,GpuTemperatureCelsius,DownloadMbps,UploadMbps,BatteryPercent");

        foreach (var sample in samples)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Escape(sample.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Format(sample.CpuUsagePercent),
                Format(sample.MemoryUsedGb),
                Format(sample.MemoryTotalGb),
                Format(sample.GpuUsagePercent),
                Format(sample.GpuTemperatureCelsius),
                Format(sample.DownloadMbps),
                Format(sample.UploadMbps),
                Format(sample.BatteryPercent)
            }));
        }

        System.IO.File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Format(double? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
