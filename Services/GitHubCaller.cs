using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

public static partial class RegexHelper
{
    [GeneratedRegex(@"\{(\w+)\}")]
    public static partial Regex PlaceholderRegex();
}

public class GitHubCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _userRepoUrlTemplate;

    public GitHubCaller(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires a User-Agent header
        _userRepoUrlTemplate = configuration["GitHubApi:UserRepoUrl"] ?? "";

        if (string.IsNullOrWhiteSpace(_userRepoUrlTemplate))
        {
            throw new InvalidOperationException("The UserRepoUrl configuration is missing or empty.");
        }

        if (!Regex.IsMatch(_userRepoUrlTemplate, @"\{(\w+)\}"))
        {
            throw new InvalidOperationException("The UserRepoUrl does not contain valid placeholders (e.g., {username}).");
        }

        // Console.WriteLine($"GitHub User Repo URL Template: {_userRepoUrlTemplate}");
    }

    public async Task<Dictionary<string, int>> GetLanguageStatistics(Dictionary<string, string> parameters)
    {
        // Fetch repos from GitHub API
        var reposUrl = ReplacePlaceholders(_userRepoUrlTemplate, parameters);
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
                Console.WriteLine($"repo: {repo}, repo.Language: {repo.Language}");
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
