using Newtonsoft.Json.Linq;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Domain.Abstractions.GitRelated;

public interface ICommitService
{
    Task<JArray> GetCommitsForBranchAsync(RepositoryWithBranchQueryDto query, CancellationToken cancellationToken = default);

    Task<IEnumerable<string?>> FilterCommitsByAuthorAsync(CommitQueryDto query,
        CancellationToken cancellationToken = default);

    Task<string?> GetLastCommitByAuthorAsync(RepositoryWithBranchQueryDto query,
        CancellationToken cancellationToken = default);
}