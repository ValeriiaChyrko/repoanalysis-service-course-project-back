using RepoAnalysis.Domain.Abstractions.Contracts;

namespace RepoAnalysis.Domain.Abstractions.TestsSection;

public interface ITestsRunner
{
    Task<IEnumerable<TestResult>> RunTestsAsync(string repoPath, CancellationToken cancellationToken);
}