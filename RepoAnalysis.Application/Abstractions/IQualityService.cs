using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Abstractions;

public interface IQualityService
{
    Task<int> VerifyProjectQuality(RepositoryWithBranchQueryDto repoWithBranchQuery,
        CancellationToken cancellationToken = default);
}