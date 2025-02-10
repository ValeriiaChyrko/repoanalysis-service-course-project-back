using Newtonsoft.Json.Linq;

namespace RepoAnalysis.Domain.Abstractions.GitHubRelated;

public interface IGitHubApiClient
{
    Task<JArray> GetJsonArrayAsync(string url, CancellationToken cancellationToken = default);
}