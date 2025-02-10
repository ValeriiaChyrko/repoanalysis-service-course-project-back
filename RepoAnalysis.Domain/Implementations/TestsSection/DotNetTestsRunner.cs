using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.TestsSection;

namespace RepoAnalysis.Domain.Implementations.TestsSection;

public partial class DotNetTestsRunner : ITestsRunner
{
    private const string DockerImage = "mcr.microsoft.com/dotnet/sdk:7.0";
    private const string Command = "dotnet";
    private static readonly Regex PassedPattern = GeneratePassedPatternRegex();
    private static readonly Regex FailedPattern = GenerateFailedPatternRegex();
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeTestsService> _logger;

    public DotNetTestsRunner(IDockerService dockerService, ILogger<CodeTestsService> logger)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TestResult>> RunTestsAsync(string repoPath, CancellationToken cancellationToken)
    {
        ValidateRepositoryPath(repoPath);

        var testFiles = Directory.GetFiles(Path.Combine(repoPath, "tests"), "*.csproj", SearchOption.AllDirectories);
        var testResults = new ConcurrentBag<TestResult>();

        var tasks = testFiles.Select(testFile =>
            RunTestWithLoggingAsync(testFile, repoPath, testResults, cancellationToken));
        await Task.WhenAll(tasks);

        return testResults.ToList();
    }

    private async Task RunTestWithLoggingAsync(string testFile, string repositoryPath,
        ConcurrentBag<TestResult> testResults, CancellationToken cancellationToken)
    {
        try
        {
            var resultSet = await RunTestsInDockerAsync(testFile, repositoryPath, cancellationToken);
            foreach (var result in resultSet) testResults.Add(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running tests for file: {TestFile}", testFile);
        }
    }

    private async Task<List<TestResult>> RunTestsInDockerAsync(string testFile, string repositoryPath,
        CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(repositoryPath, testFile);
        var arguments = $"test \"{Path.GetFileName(relativePath)}\" --verbosity normal";
        var workingDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;

        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            workingDirectory,
            DockerImage,
            Command,
            arguments
        );
        var result = await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
        return ParseTestResults(result.OutputDataReceived);
    }

    private static List<TestResult> ParseTestResults(string output)
    {
        var regex = CompileTestExecutionOutputRegex();
        var match = regex.Match(output);
        var filteredOutput = match.Success ? match.Groups[1].Value : string.Empty;

        var lines = filteredOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(ParseTestResult).OfType<TestResult>().ToList();
    }

    private static TestResult? ParseTestResult(string line)
    {
        var match = PassedPattern.Match(line);
        if (match.Success)
            return new TestResult
            {
                TestName = match.Groups["TestName"].Value,
                IsPassed = true,
                ExecutionTimeMs = ExtractExecutionTime(match.Groups["Time"].Value)
            };

        match = FailedPattern.Match(line);
        if (match.Success)
            return new TestResult
            {
                TestName = match.Groups["TestName"].Value,
                IsPassed = false,
                ExecutionTimeMs = ExtractExecutionTime(match.Groups["Time"].Value)
            };

        return null;
    }

    private static double ExtractExecutionTime(string timeOutput)
    {
        var cleanTime = timeOutput.Replace("<", "").Replace(" ms", "").Trim();
        return double.TryParse(cleanTime, out var time) ? time : 0;
    }

    private void ValidateRepositoryPath(string repoPath)
    {
        if (!string.IsNullOrWhiteSpace(repoPath)) return;
        _logger.LogError("Repository path is null or empty.");
        throw new ArgumentException("Repository path cannot be null or empty.", nameof(repoPath));
    }

    [GeneratedRegex(@"Passed\s+(?<TestName>[^\s]+)\s+\[\s*(?<Time>(?:<\s*)?\d+(\.\d+)?\s+ms)\]", RegexOptions.Compiled)]
    private static partial Regex GeneratePassedPatternRegex();

    [GeneratedRegex(@"Failed\s+(?<TestName>[^\s]+)\s+\[\s*(?<Time>(?:<\s*)?\d+(\.\d+)?\s+ms)\]", RegexOptions.Compiled)]
    private static partial Regex GenerateFailedPatternRegex();

    [GeneratedRegex(@"(?s).*?Starting test execution, please wait\.\.\.(.*)", RegexOptions.IgnoreCase, "uk-UA")]
    private static partial Regex CompileTestExecutionOutputRegex();
}