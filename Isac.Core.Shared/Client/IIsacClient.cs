using Isac.Core.Shared.Transport;
using System.Threading;
using System.Threading.Tasks;

namespace Isac.Core.Shared.Client;

public interface IIsacClient
{
    Task<QueryResponse> SendQueryAsync(QueryRequest request, CancellationToken ct = default);
    Task<VoiceEnrollResponse> EnrollVoiceAsync(VoiceEnrollRequest request, CancellationToken ct = default);
    Task DeleteVoiceAsync(string userId, CancellationToken ct = default);
    Task<bool> PingAsync(CancellationToken ct = default);
}
