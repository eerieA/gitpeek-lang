using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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
    public static async Task<(TResult?, GitHubApiErrorCodes, HttpResponseHeaders?, string)> CallApiSimple<TResult>(
        Func<Task<HttpResponseMessage>> apiCall,
        Func<Task<TResult>> onSuccess,
        TResult defaultOnError)
    {
        string content = "";
        try
        {
            // Execute the API call
            var response = await apiCall();
            var headers = response.Headers;
            
            // Read content immediately after getting response
            content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Check for rate limit exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var rateLimitRemaining = headers.Contains("X-RateLimit-Remaining")
                        ? headers.GetValues("X-RateLimit-Remaining").FirstOrDefault()
                        : null;

                    if (rateLimitRemaining == "0" || content.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                    {
                        return (defaultOnError, GitHubApiErrorCodes.RateLimitExceeded, headers, content);
                    }
                    
                    return (defaultOnError, GitHubApiErrorCodes.OtherError, headers, content);
                }
                
                // If not rate limit, check if the response contains an error message
                if (content.Contains("\"message\""))
                {
                    Console.WriteLine($"[DEBUG] API Error Response: {content}");
                    return (defaultOnError, GitHubApiErrorCodes.OtherError, headers, content);
                }

                return (defaultOnError, GitHubApiErrorCodes.OtherError, headers, content);
            }

            // If successful, process the response
            var result = await onSuccess();
            return (result, GitHubApiErrorCodes.NoError, headers, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error: {ex.Message}. Response content: {content}");
            return (defaultOnError, GitHubApiErrorCodes.OtherError, null, content);
        }
    }
}

public class GitHubCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _AccessToken;
    private readonly string _UserReposUrlTemplate;
    private readonly string _RepoLanguagesUrlTemplate;
    private readonly string _UserAgentProduct = "GitPeek";
    private readonly string _UserAgentVersion = "1.0";
    private readonly string _UserAgentContactInfo = "(+https://github.com/eerieA)"; 

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
        var (repos, errorCode, rateLimitInfo, errorContent) = await GitHubApiHelper.CallApiSimple(
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

    public async Task<(Dictionary<string, long> stats, GitHubApiErrorCodes errorCode, Dictionary<string, string> rateLimitInfo)> GetDetailedLanguageStatistics(Dictionary<string, string> parameters)
    {
        var rateLimitInfo = new Dictionary<string, string>();
        var reposUrl = ReplacePlaceholders(_UserReposUrlTemplate, parameters);

        var (repos, errorCode, headers, errorContent) = await GitHubApiHelper.CallApiSimple(
            () => SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl),
            async () =>
            {
                var response = await SendRequestWithOptionalAuth(HttpMethod.Get, reposUrl);
                return await response.Content.ReadFromJsonAsync<List<GitHubRepo>>();
            },
            new List<GitHubRepo>()
        );

        // Extract rate limit info from headers
        ExtractRateLimitHeaders(headers, rateLimitInfo);
        Console.WriteLine($"X-RateLimit-Remaining: {rateLimitInfo["X-RateLimit-Remaining"]}.");

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            Console.WriteLine("Rate limit exceeded while fetching repositories. Aborting operation.");
            Console.WriteLine($"X-RateLimit-Reset: {rateLimitInfo["X-RateLimit-Reset"]}.");
            return (new Dictionary<string, long>(), GitHubApiErrorCodes.RateLimitExceeded, rateLimitInfo);
        }

        if (errorContent.Length > 0) {
            Console.WriteLine($"GitHub responded with error content: {errorContent}.");
        }

        if (repos == null || repos.Count == 0)
        {
            Console.WriteLine($"No repositories found for user {parameters["username"]}.");
            return (new Dictionary<string, long>(), GitHubApiErrorCodes.OtherError, rateLimitInfo);
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

            var (repoLanguages, repoError, repoHeaders, repoErrorContent) = await GitHubApiHelper.CallApiSimple(
                () => SendRequestWithOptionalAuth(HttpMethod.Get, repoLanguagesUrl),
                async () => 
                {
                    var response = await _httpClient.GetAsync(repoLanguagesUrl);
                    
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    if (jsonContent.Contains("\"message\""))  // Handle API error responses
                    {
                        Console.WriteLine($"[DEBUG] GitHub API Error: {jsonContent}");
                        return null;
                    }

                    return await response.Content.ReadFromJsonAsync<Dictionary<string, long>>();
                },
                null
            );

            // Extract rate limit info from repo headers
            ExtractRateLimitHeaders(repoHeaders, rateLimitInfo);

            if (repoError == GitHubApiErrorCodes.RateLimitExceeded)
            {
                Console.WriteLine("Rate limit exceeded while fetching repo languages. Stopping further API calls.");
                Console.WriteLine($"X-RateLimit-Reset: {rateLimitInfo["X-RateLimit-Reset"]}.");
                return (languageStats, GitHubApiErrorCodes.RateLimitExceeded, rateLimitInfo);
            }

            if (repoErrorContent.Length > 0) {
                Console.WriteLine($"GitHub responded with error content: {repoErrorContent}.");
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

        // After aggregating, sort the languages by total count in descending order
        var sortedLanguageStats = languageStats
            .OrderByDescending(kvp => kvp.Value)              // Sort by the count in descending order
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);  // Convert back to dictionary

        return (sortedLanguageStats, GitHubApiErrorCodes.NoError, rateLimitInfo);
    }

    private void ExtractRateLimitHeaders(HttpResponseHeaders? headers, Dictionary<string, string> rateLimitInfo)
    {
        if (headers == null) return;

        if (headers.TryGetValues("X-RateLimit-Remaining", out var remaining))
        {
            rateLimitInfo["X-RateLimit-Remaining"] = remaining.FirstOrDefault() ?? "0";
        }

        if (headers.TryGetValues("X-RateLimit-Reset", out var reset))
        {
            rateLimitInfo["X-RateLimit-Reset"] = reset.FirstOrDefault() ?? "0";
        }
    }

    private Task<HttpResponseMessage> SendRequestWithOptionalAuth(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        // Add required User-Agent header (GitHub requirement)
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(
            productName: _UserAgentProduct,
            productVersion: _UserAgentVersion));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue(
            _UserAgentContactInfo));

        // Add Authorization header
        if (!string.IsNullOrWhiteSpace(_AccessToken) && !string.Equals(_AccessToken, "your_personal_access_token_here"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _AccessToken);
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
