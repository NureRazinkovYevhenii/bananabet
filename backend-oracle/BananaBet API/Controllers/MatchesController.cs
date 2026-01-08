using BananaBet_API.DTO;
using BananaBet_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Controllers
{
    [ApiController]
    [Route("api/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly BettingDbContext _db;

        public MatchesController(BettingDbContext db)
        {
            _db = db;
        }

        // GET /api/matches
        [HttpGet]
        public async Task<ActionResult<List<MatchDto>>> GetOpen(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var data = await _db.Matches
                .AsNoTracking()
                .Where(m => m.Status == MatchPipelineStatus.Open)
                .OrderBy(m => m.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    ExternalId = m.ExternalId,
                    HomeTeam = m.HomeTeam,
                    AwayTeam = m.AwayTeam,
                    StartTime = m.StartTime,
                    OddsHome = m.OddsHome,
                    OddsAway = m.OddsAway,
                    Status = m.Status
                })
                .ToListAsync(ct);

            return Ok(data);
        }

        // GET /api/matches/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MatchDetailsDto>> GetById(int id, CancellationToken ct)
        {
            var match = await _db.Matches
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new MatchDetailsDto
                {
                    Id = m.Id,
                    ExternalId = m.ExternalId,
                    HomeTeam = m.HomeTeam,
                    AwayTeam = m.AwayTeam,
                    StartTime = m.StartTime,
                    OddsHome = m.OddsHome,
                    OddsAway = m.OddsAway,
                    Status = m.Status,
                    Result = m.Result
                })
                .FirstOrDefaultAsync(ct);

            if (match == null)
                return NotFound();

            return Ok(match);
        }

        // GET /api/matches/history
        [HttpGet("history")]
        public async Task<ActionResult<List<MatchDto>>> GetHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var data = await _db.Matches
                .AsNoTracking()
                .Where(m => m.Status == MatchPipelineStatus.Resolved)
                .OrderByDescending(m => m.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    ExternalId = m.ExternalId,
                    HomeTeam = m.HomeTeam,
                    AwayTeam = m.AwayTeam,
                    StartTime = m.StartTime,
                    OddsHome = m.OddsHome,
                    OddsAway = m.OddsAway,
                    Status = m.Status
                })
                .ToListAsync(ct);

            return Ok(data);
        }

        // GET /api/matches/history
        [HttpGet("ongoing")]
        public async Task<ActionResult<List<MatchDto>>> GetOngoing(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var data = await _db.Matches
                .AsNoTracking()
                .Where(m => m.Status == MatchPipelineStatus.Closed)
                .OrderBy(m => m.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    ExternalId = m.ExternalId,
                    HomeTeam = m.HomeTeam,
                    AwayTeam = m.AwayTeam,
                    StartTime = m.StartTime,
                    OddsHome = m.OddsHome,
                    OddsAway = m.OddsAway,
                    Status = m.Status
                })
                .ToListAsync(ct);

            return Ok(data);
        }
    }
}

