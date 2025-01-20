using Microsoft.AspNetCore.Mvc;
using gitpeek_lang.Services;

namespace gitpeek_lang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubStatsController : ControllerBase
{
    private readonly GitHubCaller _gitHubCaller;
    private readonly GraphMaker _graphMaker;

    public GitHubStatsController(GitHubCaller gitHubCaller, GraphMaker graphMaker)
    {
        _gitHubCaller = gitHubCaller;
        _graphMaker = graphMaker;
    }

    // Endpoint to get language stats as JSON
    [HttpGet("{username}")]
    public async Task<IActionResult> GetLanguageStats(string username)
    {
        var parameters = new Dictionary<string, string> {
            {"username", username}
        };

        var (stats, errorCode) = await _gitHubCaller.GetDetailedLanguageStatistics(parameters);

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            return StatusCode(429, new { message = "Rate limit exceeded. If you are not using your GitHub access token then that may be the cause.", errorCode = errorCode });
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
        [FromQuery] int? width = null,
        [FromQuery] int? barHeight = null,
        [FromQuery] int? lgItemWidth = null)
    {
        var parameters = new Dictionary<string, string> {
            {"username", username}
        };

        // var (stats, errorCode) = await _gitHubCaller.GetDetailedLanguageStatistics(parameters);
        // Console.WriteLine($"token: {configuration["GitHubApi:AccessToken"]}");  //DEBUG

        GitHubApiErrorCodes errorCode = GitHubApiErrorCodes.RateLimitExceeded;  //DEBUG
        Dictionary<string, long> stats = [];  //DEBUG
        // Handle rate limit error
        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            var errorMessage = "Rate limit exceeded. Please check GitHub's rate reset time and wait.";
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

        // Generate SVG graph with legend
        var svg = _graphMaker.GenerateSvgWithLegend(
            stats,
            width ?? 600,           // Default width if not specified
            barHeight ?? 50,        // Default bar height if not specified
            lgItemWidth ?? 120      // Default legend item width if not specified
        );

        return Content(svg, "image/svg+xml");
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