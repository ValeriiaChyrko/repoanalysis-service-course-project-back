namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class DiagnosticMessage
{
    public string Message { get; set; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
}