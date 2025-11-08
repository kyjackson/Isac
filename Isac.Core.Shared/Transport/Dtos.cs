namespace Isac.Core.Shared.Transport;

public record QueryRequest(
    string UserId,
    string DeviceId,
    string AudioFormat,
    byte[] Payload
);

public record QueryResponse(
    string AudioFormat,
    byte[] AudioBytes,
    string? Text = null
);

public record VoiceSampleDescriptor(
    string FileName,
    string MimeType,
    byte[] AudioBytes
);

public record VoiceEnrollRequest(
    string UserId,
    IReadOnlyList<VoiceSampleDescriptor> Clips
);

public record VoiceEnrollResponse(
    string VoiceProfileId
);

public record VoiceDeleteRequest(
    string UserId
);
