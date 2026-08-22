using Microsoft.Win32;
using PulseMon.Models;

namespace PulseMon.Services;

public sealed class DeviceInfoService
{
    private const string BiosRegistryPath = @"HARDWARE\DESCRIPTION\System\BIOS";
    private const string CpuRegistryPath = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";
    private const string VideoRegistryPath = @"SYSTEM\CurrentControlSet\Control\Video";

    public DeviceInfo GetDeviceInfo()
    {
        return new DeviceInfo
        {
            ModelName = ReadModelName(),
            CpuName = ReadCpuName(),
            GpuName = ReadGpuName()
        };
    }

    private static string ReadModelName()
    {
        using var biosKey = Registry.LocalMachine.OpenSubKey(BiosRegistryPath);

        var manufacturer = ReadString(biosKey, "SystemManufacturer");
        var productName = ReadString(biosKey, "SystemProductName");

        return CombineOrUnknown(manufacturer, productName, "Unknown device");
    }

    private static string ReadCpuName()
    {
        using var cpuKey = Registry.LocalMachine.OpenSubKey(CpuRegistryPath);
        return ReadString(cpuKey, "ProcessorNameString") ?? "Unknown CPU";
    }

    private static string ReadGpuName()
    {
        using var videoRootKey = Registry.LocalMachine.OpenSubKey(VideoRegistryPath);

        if (videoRootKey is null)
        {
            return "Unknown GPU";
        }

        foreach (var adapterKeyName in videoRootKey.GetSubKeyNames())
        {
            using var adapterKey = videoRootKey.OpenSubKey($@"{adapterKeyName}\0000");
            var adapterName = ReadString(adapterKey, "HardwareInformation.AdapterString")
                ?? ReadString(adapterKey, "DriverDesc");

            if (!string.IsNullOrWhiteSpace(adapterName))
            {
                return adapterName;
            }
        }

        return "Unknown GPU";
    }

    private static string? ReadString(RegistryKey? registryKey, string valueName)
    {
        var value = registryKey?.GetValue(valueName);

        return value switch
        {
            string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
            byte[] bytes => DecodeRegistryString(bytes),
            _ => null
        };
    }

    private static string? DecodeRegistryString(byte[] bytes)
    {
        var text = System.Text.Encoding.Unicode.GetString(bytes)
            .TrimEnd('\0')
            .Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string CombineOrUnknown(string? first, string? second, string fallback)
    {
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(second))
        {
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(first))
        {
            return second!;
        }

        if (string.IsNullOrWhiteSpace(second))
        {
            return first;
        }

        return $"{first} {second}";
    }
}
