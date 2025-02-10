namespace RepoAnalysis.Domain.Abstractions.QualitySection;

public interface ICodeQualityService
{
    Task<int> CheckCodeQualityAsync(string repoPath, CancellationToken cancellationToken = default);
}