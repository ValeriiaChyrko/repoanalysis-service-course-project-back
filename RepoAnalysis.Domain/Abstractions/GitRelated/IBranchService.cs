using Newtonsoft.Json.Linq;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Domain.Abstractions.GitRelated;

public interface IBranchService
{
    Task<JArray> GetBranchesInfoAsync(BranchQueryDto branchQuery, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>?> GetBranchesByAuthorAsync(BranchQueryDto branchQueryDto,
        IEnumerable<string> branchTitles, CancellationToken cancellationToken = default);

    Task<string> PostBranchByAuthorAsync(BranchQueryDto branchQueryDto, CancellationToken cancellationToken = default);
}