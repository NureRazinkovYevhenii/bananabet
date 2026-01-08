using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/football")]
public class FootballController : ControllerBase
{
    private readonly FootballDataClient _client;

    public FootballController(FootballDataClient client)
    {
        _client = client;
    }

    [HttpGet("upcoming/{competition}")]
    public async Task<IActionResult> Upcoming(string competition)
    {
        return Ok(await _client.GetUpcomingMatchesAsync(competition, 5));
    }

}