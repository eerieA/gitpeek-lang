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

        var (stats, errorCode) = await _gitHubCaller.GetDetailedLanguageStatistics(parameters);

        if (errorCode == GitHubApiErrorCodes.RateLimitExceeded)
        {
            var errorMessage = "Rate limit exceeded. If you are not using your GitHub access token, that may be the cause.";
            var htmlContent = $"<html><body><h1>Error 429</h1><p>{errorMessage}</p></body></html>";
            return Content(htmlContent, "text/html");
        }

        if (stats.Count == 0)
        {
            var errorMessage = "No repositories found or response error occurred.";
            var htmlContent = $"<html><body><h1>Error 404</h1><p>{errorMessage}</p></body></html>";
            return Content(htmlContent, "text/html");
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
}