using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace OpenCursor.Host
{
    public class AppSettings
    {
        public string GoogleApiKey { get; set; } = string.Empty;
        public string OpenRouterApiKey { get; set; } = string.Empty;
        public string ClaudeApiKey { get; set; } = string.Empty;
        public string ChatClient { get; set; } = "OpenRouter"; // "OpenRouter", "Gemini", or "Claude"
        public string WorkspaceDirectory { get; set; } = string.Empty;
        public string ClaudeModel { get; set; } = "claude-3-5-sonnet-20241022";
    }

    public class SettingsService
    {
        private const string SettingsFile = "settings.json";

        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(SettingsFile))
            {
                return new AppSettings();
            }
            var json = await File.ReadAllTextAsync(SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public async Task SaveAsync(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsFile, json);
        }
    }
}
