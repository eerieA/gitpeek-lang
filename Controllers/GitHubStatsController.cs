using Microsoft.AspNetCore.Mvc;
using gitpeek_lang.Services;

namespace gitpeek_lang.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubStatsController : ControllerBase
{
    private readonly GitHubCaller _gitHubCaller;

    public GitHubStatsController(GitHubCaller gitHubCaller)
    {
        _gitHubCaller = gitHubCaller;
    }

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
}
