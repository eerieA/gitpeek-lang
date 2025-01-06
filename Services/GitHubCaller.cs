using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace gitpeek_lang.Services;

public static partial class RegexHelper
{
    [GeneratedRegex(@"\{(\w+)\}")]
    public static partial Regex PlaceholderRegex();
}

public enum GitHubApiErrorCodes
{
    RateLimitExceeded = 1001,
    OtherError = 1002
}

public class GitHubApiHelper
{
    public static async Task<T> CallApiWithErrorHandling<T>(
        Func<Task<HttpResponseMessage>> apiCall,
        Func<HttpResponseMessage, Task<T>> onError,
        Func<HttpResponseMessage, Task<T>> onSuccess)
    {
        try
        {
            // Execute the API call
            var response = await apiCall();

            // Handle errors if response is not successful
            if (!response.IsSuccessStatusCode)
            {
                return await onError(response);
            }

            // Process the successful response
            return await onSuccess(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] An unexpected error occurred: {ex.Message}");
            throw;
        }
    }
}

// TODO: Refactor the onError functions for both versions of get stats to be synomymous
public class GitHubCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _UserReposUrlTemplate;
    private readonly string _RepoLanguagesUrlTemplate;

    public GitHubCaller(HttpClient httpClient, IConfiguration configuration)
    {
        // Configure http call
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires a User-Agent header
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", configuration["GitHubApi:Version"]);


        _UserReposUrlTemplate = configuration["GitHubApi:UserReposUrl"] ?? "";

        if (string.IsNullOrWhiteSpace(_UserReposUrlTemplate))
        {
            throw new InvalidOperationException("The UserReposUrl configuration is missing or empty.");
        }

        // Console.WriteLine($"GitHub User Repo URL Template: {_UserReposUrlTemplate}");
        _RepoLanguagesUrlTemplate = configuration["GitHubApi:RepoLanguagesUrl"] ?? "";
        if (string.IsNullOrWhiteSpace(_RepoLanguagesUrlTemplate))
        {
            throw new InvalidOperationException("The RepoLanguagesUrl configuration is missing or empty.");
        }

    }

    public async Task<Dictionary<string, int>> GetLanguageStatistics(Dictionary<string, string> parameters)
    {
        return await GitHubApiHelper.CallApiWithErrorHandling(
        // First arg function: Make the API call
        async () =>
        {
            var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);
            return await _httpClient.GetAsync(reposUrl);
        },
        // Second arg function: Handle errors
        async (response) =>
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine($"Rate limit exceeded: {content}");
                return new Dictionary<string, int> { { "Error", (int)GitHubApiErrorCodes.RateLimitExceeded } };
            }

            Console.WriteLine($"API call failed with status code {response.StatusCode}: {content}");
            return new Dictionary<string, int> { { "Error", (int)GitHubApiErrorCodes.OtherError } };
        },
        // Third arg function: Process successful response
        async (response) =>
        {
            var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepo>>();

            if (repos == null || repos.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            var languageStats = new Dictionary<string, int>();
            foreach (var repo in repos)
            {
                if (!string.IsNullOrEmpty(repo.Language))
                {
                    Console.WriteLine($"repo.Name: {repo.Name}, repo.Language: {repo.Language}");
                    if (languageStats.ContainsKey(repo.Language))
                    {
                        languageStats[repo.Language]++;
                    }
                    else
                    {
                        languageStats[repo.Language] = 1;
                    }
                }
            }

            return languageStats;
        }
        );
    }

    public async Task<Dictionary<string, long>> GetDetailedLanguageStatistics(Dictionary<string, string> parameters)
    {
        return await GitHubApiHelper.CallApiWithErrorHandling (
        // First arg function: Make the API call
        async () =>
        {
            var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);
            return await _httpClient.GetAsync(reposUrl);
        },
        // Second arg function: Handle errors
        async (response) =>
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine($"Rate limit exceeded: {content}");
                return new Dictionary<string, long> { { "Error", (long)GitHubApiErrorCodes.RateLimitExceeded } };
            }

            Console.WriteLine($"API call failed with status code {response.StatusCode}: {content}");
            return new Dictionary<string, long> { { "Error", (long)GitHubApiErrorCodes.OtherError } };
        },
        // Third arg function: Process successful response
        async (response) => {
            // Fetch repos from GitHub API
            var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepo>>();

            if (repos == null || repos.Count == 0)
            {
                return new Dictionary<string, long>();
            }

            // Initialize a dictionary to hold aggregated language statistics
            var languageStats = new Dictionary<string, long>();

            Console.WriteLine($"User {parameters["username"]} has {repos.Count} repos.");
            foreach (var repo in repos)
            {
                if (string.IsNullOrEmpty(repo.Name))
                {
                    continue;
                }

                // Prepare the language API URL for the current repository
                var repoLanguagesUrl = ReplacePlaceholders(_RepoLanguagesUrlTemplate, new Dictionary<string, string>
                {
                    { "username", parameters["username"] },
                    { "reponame", repo.Name }
                });

                // Fetch language details for the repository
                var repoLanguages = await _httpClient.GetFromJsonAsync<Dictionary<string, long>>(repoLanguagesUrl);

                Console.WriteLine($"Getting lang stats from user {parameters["username"]}'s repo {repo.Name}...");
                if (repoLanguages == null || repoLanguages.Count == 0)
                {
                    continue;
                }

                // Aggregate language statistics
                foreach (var (language, count) in repoLanguages)
                {
                    if (languageStats.ContainsKey(language))
                    {
                        languageStats[language] += count;
                    }
                    else
                    {
                        languageStats[language] = count;
                    }
                }
            }

            return languageStats;
        }
        );
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> parameters)
    {
        return RegexHelper.PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value; // Extract the placeholder name
            if (parameters.TryGetValue(key, out var value))
            {
                return value; // Replace with the corresponding value
            }

            throw new InvalidOperationException($"Missing value for placeholder '{key}' in the template.");
        });
    }
}

public class GitHubRepo
{
    public required string Name { get; set; }
    public string? Language { get; set; } // Language is nullable
}
