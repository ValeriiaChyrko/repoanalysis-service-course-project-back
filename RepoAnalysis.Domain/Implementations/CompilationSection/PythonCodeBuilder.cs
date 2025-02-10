using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;

namespace RepoAnalysis.Domain.Implementations.CompilationSection;

public class PythonCodeBuilder : ICodeBuilder
{
    private const string DockerImage = "python:3.9-slim";
    private const string Command = "python3";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeBuildService> _logger;

    public PythonCodeBuilder(IDockerService dockerService, ILogger<CodeBuildService> logger)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BuildResult> BuildProjectAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            _logger.LogError("Repository path is null or empty.");
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));
        }

        var pythonFiles = GetPythonFiles(repositoryPath);
        if (!pythonFiles.Any())
        {
            _logger.LogWarning("No Python files found in repository: {RepositoryPath}", repositoryPath);
            return new BuildResult(false, new List<string> { "No Python files found." });
        }

        _logger.LogInformation("Starting Python project build in repository: {RepositoryPath}", repositoryPath);
        var buildResults = new List<BuildResult>();

        foreach (var pythonFile in pythonFiles)
            try
            {
                var result = await BuildPythonFileInDockerAsync(pythonFile, repositoryPath, cancellationToken);
                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully compiled Python file: {PythonFile}", pythonFile);
                    buildResults.Add(new BuildResult(true, new List<string> { pythonFile }));
                }
                else
                {
                    _logger.LogError("Failed to build project: {ProjectFile}. Exit Code: {ExitCode}. Output: {Output}",
                        pythonFile, result.ExitCode, result.OutputDataReceived);
                    buildResults.Add(new BuildResult(false, new List<string> { pythonFile }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling Python file: {PythonFile}", pythonFile);
                buildResults.Add(new BuildResult(false, new List<string> { pythonFile }));
            }

        var overallSuccess = buildResults.All(result => result.Success);
        var failedProjects = buildResults.Where(result => !result.Success).SelectMany(result => result.FailedProjects)
            .ToList();

        _logger.LogInformation("Build process completed. Overall success: {Success}. Failed files: {FailedFilesCount}",
            overallSuccess, failedProjects.Count);
        return new BuildResult(overallSuccess, failedProjects);
    }

    private async Task<ProcessResult> BuildPythonFileInDockerAsync(string pythonFile, string repositoryPath,
        CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(repositoryPath, pythonFile);
        var arguments = $"-m py_compile {Path.GetFileName(relativePath)}";
        var workingDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;

        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            workingDirectory,
            DockerImage,
            Command,
            arguments
        );

        _logger.LogDebug("Running Docker build for Python file: {PythonFile} with arguments: {Arguments}",
            pythonFile, arguments);
        return await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
    }

    private static string[] GetPythonFiles(string repositoryPath)
    {
        return Directory.GetFiles(repositoryPath, "*.py", SearchOption.AllDirectories);
    }
}