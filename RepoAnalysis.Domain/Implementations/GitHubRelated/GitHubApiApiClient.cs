﻿using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;

namespace RepoAnalysis.Domain.Implementations.GitHubRelated;

public class GitHubApiApiClient : IGitHubApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubApiApiClient> _logger;

    public GitHubApiApiClient(HttpClient httpClient, ILogger<GitHubApiApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    public async Task<JArray> GetJsonArrayAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending GET request to GitHub API: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Received response from GitHub API: {StatusCode} in {ElapsedMilliseconds}ms",
                response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GitHub API returned error. Status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);
            }

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            return JArray.Parse(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making request to GitHub API: {Url}", url);
            throw;
        }
    }

    public async Task<JObject> GetJsonObjectAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending GET request to GitHub API (expecting JSON object): {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Received response from GitHub API: {StatusCode} in {ElapsedMilliseconds}ms",
                response.StatusCode, stopwatch.ElapsedMilliseconds);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned error. Status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, responseContent);
            }

            response.EnsureSuccessStatusCode();

            return JObject.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making GET request to GitHub API: {Url}", url);
            throw;
        }
    }

    public async Task<JObject> PostJsonAsync(string url, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending POST request to GitHub API: {Url}", url);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Received response from GitHub API: {StatusCode} in {ElapsedMilliseconds}ms",
                response.StatusCode, stopwatch.ElapsedMilliseconds);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned error. Status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, responseContent);
            }

            response.EnsureSuccessStatusCode();

            return JObject.Parse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making POST request to GitHub API: {Url}", url);
            throw;
        }
    }
}