namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class TestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public double ExecutionTimeMs { get; set; }
}