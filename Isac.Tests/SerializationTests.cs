using System.Text.Json;
using Isac.Core.Shared.Transport;
using Xunit;

namespace Isac.Tests;

public class SerializationTests
{
    private readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    [Fact]
    public void QueryResponse_Serializes()
    {
        var obj = new QueryResponse("audio/wav", new byte[]{1,2,3}, "text");
        var json = JsonSerializer.Serialize(obj, _opts);
        var clone = JsonSerializer.Deserialize<QueryResponse>(json, _opts);
        Assert.Equal(obj.AudioFormat, clone!.AudioFormat);
        Assert.Equal(obj.AudioBytes, clone.AudioBytes);
        Assert.Equal(obj.Text, clone.Text);
    }

    [Fact]
    public void VoiceEnrollRequest_Serializes()
    {
        var obj = new VoiceEnrollRequest("user", new [] { new VoiceSampleDescriptor("a.wav", "audio/wav", new byte[]{0}) });
        var json = JsonSerializer.Serialize(obj, _opts);
        var clone = JsonSerializer.Deserialize<VoiceEnrollRequest>(json, _opts);
        Assert.Equal(obj.UserId, clone!.UserId);
        Assert.Single(clone.Clips);
    }
}
