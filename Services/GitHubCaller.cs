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

// TODO: Deal with GitHub's specific error message 403 (rate limit exceeded), for both get stats functions
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
        // Fetch repos from GitHub API
        var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);
        var repos = await _httpClient.GetFromJsonAsync<List<GitHubRepo>>(reposUrl);

        if (repos == null || repos.Count == 0)
        {
            return [];
        }

        // Analyze languages
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

public async Task<Dictionary<string, long>> GetDetailedLanguageStatistics(Dictionary<string, string> parameters)
{
    // Fetch repos from GitHub API
    var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);
    var repos = await _httpClient.GetFromJsonAsync<List<GitHubRepo>>(reposUrl);

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
