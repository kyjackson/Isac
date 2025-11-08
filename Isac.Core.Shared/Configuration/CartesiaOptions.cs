namespace Isac.Core.Shared.Configuration;

public class CartesiaOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string VoiceId { get; set; } = string.Empty; // User's custom cloned voice ID
    public string Model { get; set; } = "sonic-english"; // Cartesia's default streaming model
}
