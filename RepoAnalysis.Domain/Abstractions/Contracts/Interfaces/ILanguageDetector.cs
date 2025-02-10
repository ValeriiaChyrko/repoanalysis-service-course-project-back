namespace RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;

public interface ILanguageDetector
{
    string DetectMainLanguage(string repositoryPath);
}