namespace RepoAnalysis.Domain.Abstractions.TestsSection;

public interface ICodeTestsService
{
    Task<int> CheckCodeTestsAsync(string repoDirectory, CancellationToken cancellationToken = default);
}