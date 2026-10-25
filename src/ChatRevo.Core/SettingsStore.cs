using System.Text.Json;

namespace ChatRevo.Core;

public sealed class SettingsStore(string directory)
{
    public string DirectoryPath { get; } = directory;
    public string FilePath => Path.Combine(DirectoryPath, "settings.json");
    public string? LoadWarning { get; private set; }
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
            if (settings is null) throw new JsonException();
            if (settings.SchemaVersion != 1) throw new JsonException("Unknown schema");
            return settings.Normalize();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LoadWarning = "저장된 설정을 읽지 못했습니다. 원본을 보관하고 기본 설정으로 시작합니다.";
            try { File.Copy(FilePath, FilePath + $".recovery-{DateTime.UtcNow:yyyyMMddHHmmssfff}", false); } catch (IOException) { }
            return new();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings.Normalize(), JsonOptions));
        File.Move(temp, FilePath, true);
    }
}
