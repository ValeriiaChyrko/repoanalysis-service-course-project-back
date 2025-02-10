namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class DockerCommandOptions
{
    public DockerCommandOptions(string repositoryPath, string workingDirectory, string dockerImage, string command,
        string? arguments)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));

        if (string.IsNullOrWhiteSpace(dockerImage))
            throw new ArgumentException("Docker image cannot be null or empty.", nameof(dockerImage));

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        RepositoryPath = repositoryPath;
        WorkingDirectory = workingDirectory;
        DockerImage = dockerImage;
        Command = command;
        Arguments = arguments;
    }

    public string RepositoryPath { get; }
    public string WorkingDirectory { get; }
    public string DockerImage { get; }
    public string Command { get; }
    public string? Arguments { get; }
}