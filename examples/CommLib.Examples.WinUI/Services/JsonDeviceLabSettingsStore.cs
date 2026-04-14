using System.Text.Json;
using System.Text.Json.Serialization;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// JsonDeviceLabSettingsStore 타입입니다.
/// </summary>
public sealed class JsonDeviceLabSettingsStore : IDeviceLabSettingsStore
{
    /// <summary>
    /// SerializerOptions 값을 나타냅니다.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>
    /// <see cref="JsonDeviceLabSettingsStore"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public JsonDeviceLabSettingsStore()
    {
        FilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// FilePath 값을 가져옵니다.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// LoadAsync 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// SaveAsync 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateSerializerOptions 작업을 수행합니다.
    /// </summary>
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