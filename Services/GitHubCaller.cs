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
    NoError = 200,
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

    public static async Task<(TResult?, GitHubApiErrorCodes)> CallApiSimple<TResult>(
        Func<Task<HttpResponseMessage>> apiCall,
        Func<Task<TResult>> onSuccess,
        TResult defaultOnError)
    {
        try
        {
            // Execute the API call
            var response = await apiCall();

            // Inspect the response for errors
            if (!response.IsSuccessStatusCode)
            {
                // Check for rate limit exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // Check the header value for rate limit message
                    var rateLimitRemaining = response.Headers.Contains("X-RateLimit-Remaining")
                        ? response.Headers.GetValues("X-RateLimit-Remaining").FirstOrDefault()
                        : null;

                    if (rateLimitRemaining == "0")
                    {
                        return (defaultOnError, GitHubApiErrorCodes.RateLimitExceeded);
                    }

                    // Check the response body for rate limit message
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    {
                        return (defaultOnError, GitHubApiErrorCodes.RateLimitExceeded);
                    }

                    Console.WriteLine("Forbidden: Access denied or another issue.");
                    return (defaultOnError, GitHubApiErrorCodes.OtherError);
                }

                // Handle other HTTP errors
                Console.WriteLine($"API call failed with status code {response.StatusCode}.");
                return (defaultOnError, GitHubApiErrorCodes.OtherError);
            }

            // If successful, process the response
            var result = await onSuccess();
            return (result, GitHubApiErrorCodes.OtherError); // Success; no error code needed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] An unexpected error occurred: {ex.Message}");
            return (defaultOnError, GitHubApiErrorCodes.OtherError);
        }
    }
}

public class GitHubCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _AccessToken;
    private readonly string _UserReposUrlTemplate;
    private readonly string _RepoLanguagesUrlTemplate;

    public GitHubCaller(HttpClient httpClient, IConfiguration configuration)
    {
        // Configure http call
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires a User-Agent header
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", configuration["GitHubApi:Version"]);

        _AccessToken = configuration["GH_AC_TOKEN"] ?? "";

        if (string.IsNullOrWhiteSpace(_AccessToken))
        {
            Console.WriteLine("User is not using an access token. Rate limit will be low.");
        }
        Console.WriteLine("Yay we are using GH_AC_TOKEN now!");  // DEBUG

        _UserReposUrlTemplate = configuration["GitHubApi:UserReposUrl"] ?? "";
        if (string.IsNullOrWhiteSpace(_UserReposUrlTemplate))
        {
            throw new InvalidOperationException("The UserReposUrl configuration is missing or empty.");
        }

        _RepoLanguagesUrlTemplate = configuration["GitHubApi:RepoLanguagesUrl"] ?? "";
        if (string.IsNullOrWhiteSpace(_RepoLanguagesUrlTemplate))
        {
            throw new InvalidOperationException("The RepoLanguagesUrl configuration is missing or empty.");
        }
    }

    public async Task<(Dictionary<string, int> stats, GitHubApiErrorCodes errorCode)> GetLanguageStatistics(Dictionary<string, string> parameters)
    {
        var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);

        // Use CallApiSimple with SendRequestWithOptionalAuth
        var (repos, errorCode) = await GitHubApiHelper.CallApiSimple(
            () => SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl),
            async () => 
            {
                var response = await SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl);
                return await response.Content.ReadFromJsonAsync<List<GitHubRepo>>();
            },
            new List<GitHubRepo>()
        );

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            Console.WriteLine("Rate limit exceeded while fetching repositories. Aborting operation.");
            return (new Dictionary<string, int>(), GitHubApiErrorCodes.RateLimitExceeded);
        }

        if (repos == null || repos.Count == 0)
        {
            Console.WriteLine($"No repositories found for user {parameters["username"]}.");
            return (new Dictionary<string, int>(), GitHubApiErrorCodes.OtherError);
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

        return (languageStats, GitHubApiErrorCodes.NoError); // No error, return stats
    }

    public async Task<(Dictionary<string, long> stats, GitHubApiErrorCodes errorCode)> GetDetailedLanguageStatistics(Dictionary<string, string> parameters)
    {
        var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);

        // Use CallApiSimple with SendRequestWithOptionalAuth
        var (repos, errorCode) = await GitHubApiHelper.CallApiSimple(
            () => SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl),
            async () => 
            {
                var response = await SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl);
                return await response.Content.ReadFromJsonAsync<List<GitHubRepo>>();
            },
            new List<GitHubRepo>()
        );

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            Console.WriteLine("Rate limit exceeded while fetching repositories. Aborting operation.");
            return (new Dictionary<string, long>(), GitHubApiErrorCodes.RateLimitExceeded);
        }

        if (repos == null || repos.Count == 0)
        {
            Console.WriteLine($"No repositories found for user {parameters["username"]}.");
            return (new Dictionary<string, long>(), GitHubApiErrorCodes.OtherError);
        }

        Console.WriteLine($"Got {repos.Count} repos from user {parameters["username"]}.");

        var languageStats = new Dictionary<string, long>();

        foreach (var repo in repos)
        {
            if (string.IsNullOrEmpty(repo.Name))
            {
                continue;
            }

            var repoLanguagesUrl = ReplacePlaceholders(_RepoLanguagesUrlTemplate, new Dictionary<string, string>
            {
                { "username", parameters["username"] },
                { "reponame", repo.Name }
            });

            Console.WriteLine($"Getting lang stats from user {parameters["username"]}'s repo {repo.Name}...");

            var (repoLanguages, repoError) = await GitHubApiHelper.CallApiSimple(
                () => SendRequestWithOptionalAuth(HttpMethod.Get, repoLanguagesUrl),
                async () => await _httpClient.GetFromJsonAsync<Dictionary<string, long>>(repoLanguagesUrl),
                null
            );

            if (repoError == GitHubApiErrorCodes.RateLimitExceeded)
            {
                Console.WriteLine("Rate limit exceeded while fetching repo languages. Stopping further API calls.");
                return (languageStats, GitHubApiErrorCodes.RateLimitExceeded); // Return stats so far and rate limit error
            }

            if (repoLanguages != null)
            {
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
        }

        return (languageStats, GitHubApiErrorCodes.NoError); // No error, return stats
    }

    private Task<HttpResponseMessage> SendRequestWithOptionalAuth(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        // Add Authorization header only if AccessToken is not empty and not equal to placeholder string
        if (!string.IsNullOrWhiteSpace(_AccessToken) && !string.Equals(_AccessToken, "your_personal_access_token_here"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _AccessToken);
        }

        // Debugging: Log request details
        Console.WriteLine($"[DEBUG] Requesting URL: {url}");
        if (request.Headers.Authorization != null)
        {
            // Console.WriteLine($"[DEBUG] Authorization: {request.Headers.Authorization}");
            Console.WriteLine($"[DEBUG] Authorization _AccessToken is not null (likely valid).");
        }

        return _httpClient.SendAsync(request);
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
