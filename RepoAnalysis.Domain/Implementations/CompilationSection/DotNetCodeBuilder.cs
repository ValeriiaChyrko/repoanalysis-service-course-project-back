using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;

namespace RepoAnalysis.Domain.Implementations.CompilationSection;

public class DotNetCodeBuilder : ICodeBuilder
{
    private const string DockerImage = "mcr.microsoft.com/dotnet/sdk:7.0";
    private const string Command = "dotnet";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeBuildService> _logger;
    private readonly int _maxDegreeOfParallelism;

    public DotNetCodeBuilder(IDockerService dockerService, ILogger<CodeBuildService> logger,
        int maxDegreeOfParallelism = 4)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }


    public async Task<BuildResult> BuildProjectAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            _logger.LogError("Repository path is null or empty.");
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));
        }

        _logger.LogInformation("Starting project build in repository: {RepositoryPath}", repositoryPath);

        var projectFiles = GetProjectFiles(repositoryPath);
        if (projectFiles.Length == 0)
        {
            _logger.LogWarning("No .csproj files found in repository: {RepositoryPath}", repositoryPath);
            return new BuildResult(false, new List<string> { "No project files found." });
        }

        _logger.LogInformation("Found {ProjectCount} projects to build.", projectFiles.Length);

        var buildResults = new ConcurrentBag<BuildResult>();

        await Parallel.ForEachAsync(projectFiles,
            new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, async (projectFile, ct) =>
            {
                try
                {
                    if (IsBuildCacheValid(projectFile))
                    {
                        _logger.LogInformation("Skipping already built project: {ProjectFile}", projectFile);
                        buildResults.Add(new BuildResult(true, new List<string> { projectFile }));
                        return;
                    }

                    var result = await BuildProjectFileInDockerAsync(projectFile, repositoryPath, ct);
                    if (result.ExitCode == 0)
                    {
                        _logger.LogInformation("Successfully built project: {ProjectFile}", projectFile);
                        buildResults.Add(new BuildResult(true, new List<string> { projectFile }));
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to build project: {ProjectFile}. Exit Code: {ExitCode}. Output: {Output}",
                            projectFile, result.ExitCode, result.OutputDataReceived);
                        buildResults.Add(new BuildResult(false, new List<string> { projectFile }));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building project: {ProjectFile}", projectFile);
                    buildResults.Add(new BuildResult(false, new List<string> { projectFile }));
                }
            });

        var overallSuccess = buildResults.All(result => result.Success);
        var failedProjects = buildResults.Where(result => !result.Success).SelectMany(result => result.FailedProjects)
            .ToList();

        _logger.LogInformation(
            "Build process completed. Overall success: {Success}. Failed projects: {FailedProjectsCount}",
            overallSuccess, failedProjects.Count);
        return new BuildResult(overallSuccess, failedProjects);
    }

    private async Task<ProcessResult> BuildProjectFileInDockerAsync(
        string projectFile,
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(repositoryPath, projectFile);
        var arguments = $"build {Path.GetFileName(relativePath)} --configuration Release /nologo /v:q";
        var workingDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;

        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            workingDirectory,
            DockerImage,
            Command,
            arguments
        );

        _logger.LogDebug("Running Docker build for project: {ProjectFile} with arguments: {Arguments}",
            projectFile, arguments);

        return await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
    }

    private static string[] GetProjectFiles(string repositoryPath)
    {
        return Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories);
    }

    private static bool IsBuildCacheValid(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile);
        if (projectDirectory == null) return false;

        var binDir = Path.Combine(projectDirectory, "bin");
        var objDir = Path.Combine(projectDirectory, "obj");

        if (!Directory.Exists(binDir) || !Directory.Exists(objDir))
            return false; // If `bin/` or `obj/` does not exist, a build is required

        var lastBuildTime = Directory.GetLastWriteTime(binDir);
        var lastSourceEdit = Directory.GetLastWriteTime(projectDirectory);

        return lastBuildTime >= lastSourceEdit;
    }
}