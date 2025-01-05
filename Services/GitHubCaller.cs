using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class GitHubCaller
{
    private readonly HttpClient _httpClient;

    public GitHubCaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires a User-Agent header
    }

    public async Task<Dictionary<string, int>> GetLanguageStatistics(string username)
    {
        // Fetch repos from GitHub API
        var reposUrl = $"https://api.github.com/users/{username}/repos";
        var repos = await _httpClient.GetFromJsonAsync<List<GitHubRepo>>(reposUrl);

        if (repos == null || repos.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        // Analyze languages
        var languageStats = new Dictionary<string, int>();
        foreach (var repo in repos)
        {
            if (!string.IsNullOrEmpty(repo.Language))
            {
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
    public string Name { get; set; }
    public string? Language { get; set; } // Language is nullable
}
