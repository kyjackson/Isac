using System.Diagnostics;
using System.Reflection;

namespace Isac.Mobile.Configuration;

/// <summary>
/// Loads configuration from embedded .env file at build time
/// </summary>
public static class EmbeddedConfiguration
{
    private static readonly Dictionary<string, string> _configuration = new();

    /// <summary>
    /// Loads configuration from embedded .env resource
    /// </summary>
    public static async Task LoadAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = ".env";
            
            // Try to find the .env resource
            var resources = assembly.GetManifestResourceNames();
            var envResource = resources.FirstOrDefault(r => r.EndsWith(".env"));
            
            if (string.IsNullOrEmpty(envResource))
            {
                Debug.WriteLine($"[EmbeddedConfiguration] No .env resource found. Available resources: {string.Join(", ", resources)}");
                return;
            }

            using var stream = assembly.GetManifestResourceStream(envResource);
            if (stream == null)
            {
                Debug.WriteLine($"[EmbeddedConfiguration] Could not open .env resource stream");
                return;
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            foreach (var line in content.Split('\n'))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    _configuration[key] = value;
                    Debug.WriteLine($"[EmbeddedConfiguration] Loaded: {key}");
                }
            }

            Debug.WriteLine($"[EmbeddedConfiguration] Successfully loaded {_configuration.Count} configuration values from embedded .env");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EmbeddedConfiguration] Error loading .env: {ex.Message}");
            Debug.WriteLine($"[EmbeddedConfiguration] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Gets a configuration value by key
    /// </summary>
    public static string? Get(string key)
    {
        return _configuration.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets OpenAI API key from .env
    /// </summary>
    public static string? OpenAIApiKey => Get("openai_api_key");

    /// <summary>
    /// Gets Cartesia API key from .env
    /// </summary>
    public static string? CartesiaApiKey => Get("cartesia_api_key");

    /// <summary>
    /// Gets Cartesia voice ID from .env
    /// </summary>
    public static string? CartesiaVoiceId => Get("cartesia_voice_id");
}
