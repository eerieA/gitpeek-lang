using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class GitHubCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _userRepoUrl;

    public GitHubCaller(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires a User-Agent header
        _userRepoUrl = configuration["GitHubApi:UserRepoUrl"] ?? "";

        // Console.WriteLine($"Got UserRepoUrl template: {_userRepoUrl}");
        if (string.IsNullOrWhiteSpace(_userRepoUrl))
        {
            throw new InvalidOperationException("The UserRepoUrl configuration is missing or empty.");
        }

        if (!_userRepoUrl.Contains("{username}"))
        {
            throw new InvalidOperationException("The UserRepoUrl does not contain the '{username}' placeholder.");
        }
    }

    public async Task<Dictionary<string, int>> GetLanguageStatistics(string username)
    {
        // Fetch repos from GitHub API
        var reposUrl = _userRepoUrl.Replace("{username}", username);
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
}

public class GitHubRepo
{
    public required string Name { get; set; }
    public string? Language { get; set; } // Language is nullable
}
