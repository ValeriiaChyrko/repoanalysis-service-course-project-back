using System.Text.Json.Serialization;

namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class PylintMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("module")] public string Module { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
}