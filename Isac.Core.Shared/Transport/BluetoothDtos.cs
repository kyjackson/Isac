namespace Isac.Core.Shared.Transport;

// Message sent from watch to phone containing recorded audio
public record WatchAudioMessage(
    byte[] AudioData,
    int SampleRate,
    string AudioFormat // e.g., "PCM16"
);

// Message sent from phone to watch containing response audio
public record PhoneAudioResponse(
    byte[] AudioData,
    string AudioFormat // e.g., "WAV" or "PCM16"
);

// Updated for Cartesia voice enrollment
public record CartesiaVoiceEnrollRequest(
    string UserId,
    IReadOnlyList<VoiceSampleDescriptor> Samples
);

public record CartesiaVoiceEnrollResponse(
    string VoiceId,
    string Status
);
