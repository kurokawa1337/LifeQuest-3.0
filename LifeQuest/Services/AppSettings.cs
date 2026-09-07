using System.IO;
using System.Text.Json;

namespace LifeQuest.Services;

public sealed class AppSettings
{
    public string ConnectionString { get; set; } =
        @"Server=.\SQLEXPRESS;Database=LifeQuest;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

    private static readonly string Path =
        System.IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null && !string.IsNullOrWhiteSpace(s.ConnectionString)) return s;
            }
        }
        catch { }

        var def = new AppSettings();
        def.Save();
        return def;
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { }
    }
}
