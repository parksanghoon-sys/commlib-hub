using System.Text.Json;
using System.Text.Json.Serialization;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public sealed class JsonDeviceLabSettingsStore : IDeviceLabSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public JsonDeviceLabSettingsStore()
    {
        FilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public string FilePath { get; }

    public async Task<DeviceLabAppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return new DeviceLabAppSettings();
        }

        await using var stream = File.OpenRead(FilePath);
        var settings = await JsonSerializer.DeserializeAsync<DeviceLabAppSettings>(
            stream,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);

        return settings ?? new DeviceLabAppSettings();
    }

    public async Task SaveAsync(DeviceLabAppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
