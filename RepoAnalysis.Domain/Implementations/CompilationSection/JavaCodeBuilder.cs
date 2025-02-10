using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;

namespace RepoAnalysis.Domain.Implementations.CompilationSection;

public class JavaCodeBuilder : ICodeBuilder
{
    private const string DockerImage = "maven:3.8.6-openjdk-11-slim";
    private const string Command = "mvn";
    private const string Arguments = "compile -q";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeBuildService> _logger;

    public JavaCodeBuilder(IDockerService dockerService, ILogger<CodeBuildService> logger)
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

        var pomFilePath = Path.Combine(repositoryPath, "pom.xml");
        if (!File.Exists(pomFilePath))
        {
            _logger.LogWarning("No pom.xml file found in repository: {RepositoryPath}", repositoryPath);
            return new BuildResult(false, new List<string> { "No pom.xml file found." });
        }

        _logger.LogInformation("Starting Java project build in repository: {RepositoryPath}", repositoryPath);

        try
        {
            var result = await BuildProjectInDockerAsync(repositoryPath, cancellationToken);
            if (result.ExitCode == 0)
            {
                _logger.LogInformation("Successfully built Java project in repository: {RepositoryPath}",
                    repositoryPath);
                return new BuildResult(true, new List<string>());
            }

            _logger.LogError("Failed to build Java project. Exit Code: {ExitCode}. Output: {Output}",
                result.ExitCode, result.OutputDataReceived);
            return new BuildResult(false, new List<string> { "Build failed with exit code: " + result.ExitCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building Java project in repository: {RepositoryPath}", repositoryPath);
            return new BuildResult(false, new List<string> { "An error occurred during the build." });
        }
    }

    private async Task<ProcessResult> BuildProjectInDockerAsync(string repositoryPath,
        CancellationToken cancellationToken)
    {
        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            string.Empty,
            DockerImage,
            Command,
            Arguments
        );

        _logger.LogDebug("Running Docker build for Java project with arguments: {Arguments}", Arguments);
        return await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
    }
}