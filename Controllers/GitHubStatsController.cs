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
        var stats = await _gitHubCaller.GetDetailedLanguageStatistics(parameters);
        if (stats.Count == 0)
        {
            return NotFound(new { message = "No repositories found or invalid username." });
        }

        return Ok(stats);
    }
}
