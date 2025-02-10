namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string OutputDataReceived { get; set; } = string.Empty;
    public string ErrorDataReceived { get; set; } = string.Empty;
}