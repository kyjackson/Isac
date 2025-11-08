namespace Isac.Core.Shared.Configuration;

public class OpenAIRealtimeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini-realtime-preview"; // Default to mini for cost efficiency
    public string Voice { get; set; } = "alloy"; // Default OpenAI voice
}
