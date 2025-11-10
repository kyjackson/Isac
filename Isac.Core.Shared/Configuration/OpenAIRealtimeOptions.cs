namespace Isac.Core.Shared.Configuration;

public class OpenAIRealtimeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-realtime"; // Correct OpenAI Realtime API model
    public string Voice { get; set; } = "alloy"; // Default OpenAI voice
}
