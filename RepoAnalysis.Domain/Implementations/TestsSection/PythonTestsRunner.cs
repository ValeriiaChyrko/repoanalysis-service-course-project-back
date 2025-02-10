using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.TestsSection;

namespace RepoAnalysis.Domain.Implementations.TestsSection;

public partial class PythonTestsRunner : ITestsRunner
{
    private const string DockerImage = "python:3.9-slim";
    private const string Command = "python3";
    private static readonly Regex PassedPattern = GeneratePassedPatternRegex();
    private static readonly Regex FailedPattern = GenerateFailedPatternRegex();
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeTestsService> _logger;

    public PythonTestsRunner(IDockerService dockerService, ILogger<CodeTestsService> logger)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<TestResult>> RunTestsAsync(string repoPath, CancellationToken cancellationToken)
    {
        ValidateRepositoryPath(repoPath);

        var testResults = new ConcurrentBag<TestResult>();

        try
        {
            var resultSet = await RunTestsInDockerAsync(repoPath, cancellationToken);
            foreach (var result in resultSet) testResults.Add(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running tests in repository: {RepoPath}", repoPath);
            return Enumerable.Empty<TestResult>();
        }

        return testResults.ToList();
    }

    private async Task<List<TestResult>> RunTestsInDockerAsync(string repositoryPath,
        CancellationToken cancellationToken)
    {
        const string arguments = "-m unittest discover -v";

        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            string.Empty,
            DockerImage,
            Command,
            arguments
        );
        var result = await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
        return ParseTestResults(result.OutputDataReceived);
    }

    private static List<TestResult> ParseTestResults(string output)
    {
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(ParseTestLine).OfType<TestResult>().ToList();
    }

    private static TestResult? ParseTestLine(string line)
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
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            _logger.LogError("Repository path is null or empty.");
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repoPath));
        }
    }

    [GeneratedRegex(@"(?<TestName>[\w_]+)\s+\((?<FullTestPath>[\w\.]+)\)\s+\.\.\.\s+(FAIL|FAILED)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GenerateFailedPatternRegex();

    [GeneratedRegex(@"(?<TestName>[\w_]+)\s+\((?<FullTestPath>[\w\.]+)\)\s+\.\.\.\s+(OK|PASSED)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GeneratePassedPatternRegex();
}