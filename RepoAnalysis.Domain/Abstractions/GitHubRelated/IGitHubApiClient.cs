using Newtonsoft.Json.Linq;

namespace RepoAnalysis.Domain.Abstractions.GitHubRelated;

public interface IGitHubApiClient
{
    Task<JArray> GetJsonArrayAsync(string url, CancellationToken cancellationToken = default);
    Task<JObject> GetJsonObjectAsync(string url, CancellationToken cancellationToken = default);
    Task<JObject> PostJsonAsync(string url, object payload, CancellationToken cancellationToken = default);
}