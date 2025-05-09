using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using gitpeek_lang.Services;
using gitpeek_lang.Models;

namespace gitpeek_lang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubStatsController : ControllerBase
{
    private readonly GitHubCaller _gitHubCaller;
    private readonly GraphMaker _graphMaker;
    private readonly int _cacheTimer;

    public GitHubStatsController(GitHubCaller gitHubCaller, GraphMaker graphMaker, IConfiguration configuration)
    {
        _gitHubCaller = gitHubCaller;
        _graphMaker = graphMaker;
        
        // Try reading cache expiration timer value from env var
        _cacheTimer = int.TryParse(configuration["CACHE_TIMER"], out int result) ? result : 24;
    }

    // Endpoint to get language stats as JSON
    [HttpGet("{username}")]
    public async Task<IActionResult> GetLanguageStats(
        string username,
        [FromQuery] bool? noCache = false)
    {
        var parameters = new Dictionary<string, string> {
            {"username", username}
        };
        var forceRefresh = noCache ?? false;

        var (stats, errorCode, rateLimitInfo) = await CacheAndRetrieveLangStats(parameters, forceRefresh);

        // GitHubApiErrorCodes errorCode = GitHubApiErrorCodes.RateLimitExceeded;  //DEBUG
        // Dictionary<string, long> stats = [];  //DEBUG
        // var rateLimitInfo = new Dictionary<string, string> {
        //     {"X-RateLimit-Remaining", "0"}, {"X-RateLimit-Reset", "12345678"}
        // };  //DEBUG

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            var errorMessage = $"Rate limit exceeded. Reset time: {rateLimitInfo["X-RateLimit-Reset"]}.";
            return StatusCode(429, new { message = errorMessage, errorCode = errorCode });
        }

        // Log the rate limit info too on success
        if (rateLimitInfo != null)
        {
            Console.WriteLine($"Rate Limit Remaining: {rateLimitInfo["X-RateLimit-Remaining"]}");
            Console.WriteLine($"Rate Limit Reset: {rateLimitInfo["X-RateLimit-Reset"]}");
        }

        if (stats.Count == 0)
        {
            return NotFound(new { message = "No repositories found or response error occurred.", errorCode = errorCode });
        }

        return Ok(new { stats, errorCode });
    }

    // Endpoint to get the language stats as an SVG graph
    [HttpGet("{username}/graph")]
    [Produces("image/svg+xml")]
    public async Task<IActionResult> GetLanguageGraph(
        string username,
        IConfiguration configuration,
        [FromQuery] int? width = null,
        [FromQuery] int? barHeight = null,
        [FromQuery] int? lgItemWidth = null,
        [FromQuery] int? lgItemMaxCnt = null,
        [FromQuery] int? fontSize = null,
        [FromQuery] bool? noCache = false)
    {
        var parameters = new Dictionary<string, string> {
            {"username", username}
        };
        var forceRefresh = noCache ?? false;
        var (stats, errorCode, rateLimitInfo) = await CacheAndRetrieveLangStats(parameters, forceRefresh);
        // Console.WriteLine($"GH_AC_TOKEN: {configuration["GH_AC_TOKEN"]}");  //DEBUG

        // GitHubApiErrorCodes errorCode = GitHubApiErrorCodes.RateLimitExceeded;  //DEBUG
        // Dictionary<string, long> stats = [];  //DEBUG
        // var rateLimitInfo = new Dictionary<string, string> {
        //     {"X-RateLimit-Remaining", "0"}, {"X-RateLimit-Reset", "12345678"}
        // };  //DEBUG

        // Handle rate limit error
        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            var errorMessage = $"Rate limit exceeded. Reset time: {rateLimitInfo["X-RateLimit-Reset"]}.";
            var svgError = GenerateErrorSvg(errorMessage, width ?? 600, barHeight ?? 50);
            return Content(svgError, "image/svg+xml");
        }

        // Handle no repositories or other errors
        if (stats.Count == 0)
        {
            var errorMessage = "No repositories found or response error occurred.";
            var svgError = GenerateErrorSvg(errorMessage, width ?? 600, barHeight ?? 50);
            return Content(svgError, "image/svg+xml");
        }

        // Log the rate limit info too on success
        if (rateLimitInfo != null)
        {
            Console.WriteLine($"Rate Limit Remaining: {rateLimitInfo["X-RateLimit-Remaining"]}");
            Console.WriteLine($"Rate Limit Reset: {rateLimitInfo["X-RateLimit-Reset"]}");
        }

        // Generate SVG graph with legend
        var svg = _graphMaker.GenerateSvgWithLegend(
            stats,
            width ?? 600,           // Default width if not specified
            barHeight ?? 50,        // Default bar height if not specified
            lgItemWidth ?? 120,     // Default legend item width if not specified
            lgItemMaxCnt ?? 8,      // Default legend item max number if not specified
            fontSize ?? 14          // Default legend font size if not specified
        );

        return Content(svg, "image/svg+xml");
    }

    public async Task<(Dictionary<string, long>, GitHubApiErrorCodes, Dictionary<string, string>)> CacheAndRetrieveLangStats(
        Dictionary<string, string> parameters,
        bool forceRefresh)
    {
        var cacheDirectory = "Cache";
        var cacheFilePath = Path.Combine(cacheDirectory, $"{parameters["username"]}_language_stats.json");
        var cacheDuration = TimeSpan.FromHours(_cacheTimer); // Cache expires after 24 hours
        Console.WriteLine($"Cache expiration age: {cacheDuration}");

        var now = DateTime.UtcNow;

        // Ensure the cache directory exists
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        // Check if cache exists and is valid
        if (System.IO.File.Exists(cacheFilePath) && !forceRefresh)
        {
            var cacheInfo = new FileInfo(cacheFilePath);
            if (now - cacheInfo.LastWriteTimeUtc < cacheDuration)
            {
                var cacheContent = await System.IO.File.ReadAllTextAsync(cacheFilePath);
                var cachedData = JsonSerializer.Deserialize<CachedLangStats>(cacheContent);
                
                if (cachedData?.Stats != null && cachedData.RateLimitInfo != null)
                {
                    Console.WriteLine("Returning cached data...");
                    return (cachedData.Stats, GitHubApiErrorCodes.NoError, cachedData.RateLimitInfo);
                }
            }
        }

        // Fetch and cache fresh data
        var (stats, errorCode, rateLimitInfo) = await _gitHubCaller.GetDetailedLanguageStatistics(parameters);
        if (errorCode == GitHubApiErrorCodes.NoError)
        {
            var cacheContent = JsonSerializer.Serialize(new CachedLangStats
            {
                Stats = stats,
                RateLimitInfo = rateLimitInfo
            });
            await System.IO.File.WriteAllTextAsync(cacheFilePath, cacheContent);
        }

        // Return empty stats and rate limit info if cache or API call fails
        return (stats ?? new Dictionary<string, long>(), errorCode, rateLimitInfo ?? new Dictionary<string, string>());
    }

    // Helper method to generate an SVG for error messages
    private string GenerateErrorSvg(string message, int width, int barHeight)
    {
        var height = barHeight + 20; // Adjust height based on barHeight or other factors
        return $@"
            <svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}' style='font-family: Arial, sans-serif;'>
                <rect width='{width}' height='{height}' fill='#ff0000' fill-opacity='10%' />
                <text x='50%' y='50%' fill='#721c24' text-anchor='middle' alignment-baseline='middle' font-size='14'>
                    {System.Net.WebUtility.HtmlEncode(message)}
                </text>
            </svg>";
    }
}
