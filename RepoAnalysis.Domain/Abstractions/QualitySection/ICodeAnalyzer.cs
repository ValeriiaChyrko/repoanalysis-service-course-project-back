using RepoAnalysis.Domain.Abstractions.Contracts;

namespace RepoAnalysis.Domain.Abstractions.QualitySection;

public interface ICodeAnalyzer
{
    Task<IEnumerable<DiagnosticMessage>>
        AnalyzeAsync(string repoPath, CancellationToken cancellationToken = default);
}