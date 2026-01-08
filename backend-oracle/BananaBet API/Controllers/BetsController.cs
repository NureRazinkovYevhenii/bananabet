using BananaBet_API.DTO;
using BananaBet_API.Models;
using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Controllers
{
    [ApiController]
    [Route("api/bets")]
    public class BetsController : ControllerBase
    {
        private readonly BetService _betService;
        private readonly BettingDbContext _db;

        public BetsController(BetService betService, BettingDbContext db)
        {
            _betService = betService;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBetRequest request, CancellationToken ct)
        {
            try
            {
                var bet = await _betService.CreateAsync(request, ct);
                var betDto = new BetDto {
                    Id = bet.Id,
                    UserWalletAddress = bet.UserWalletAddress,
                    MatchId = bet.MatchId,
                    MatchExternalId = (await _db.Matches.Where(m => m.Id == bet.MatchId).Select(m => m.ExternalId).FirstOrDefaultAsync(ct)) ?? string.Empty,
                    HomeTeam = (await _db.Matches.Where(m => m.Id == bet.MatchId).Select(m => m.HomeTeam).FirstOrDefaultAsync(ct)) ?? string.Empty,
                    AwayTeam = (await _db.Matches.Where(m => m.Id == bet.MatchId).Select(m => m.AwayTeam).FirstOrDefaultAsync(ct)) ?? string.Empty,
                    StartTime = (await _db.Matches.Where(m => m.Id == bet.MatchId).Select(m => m.StartTime).FirstOrDefaultAsync(ct)),
                    Pick = bet.Pick,
                    Amount = bet.Amount,
                    PlayAmount = bet.PlayAmount,
                    OddsAtMoment = bet.OddsAtMoment,
                    BetTime = bet.BetTime,
                    BlockchainTxHash = bet.BlockchainTxHash,
                    Status = bet.Status.ToString()
                };
                return Ok(betDto);
            }
            catch (BetConflictException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (BetValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("by-wallet/{walletAddress}")]
        public async Task<ActionResult<List<BetDto>>> GetByWallet(string walletAddress, CancellationToken ct)
        {
            var entities = await _db.Bets
                .AsNoTracking()
                .Include(b => b.Match)
                .Where(b => b.UserWalletAddress == walletAddress)
                .OrderByDescending(b => b.Id)
                .ToListAsync(ct);

            var bets = entities.Select(b => new BetDto
                {
                    Id = b.Id,
                    UserWalletAddress = b.UserWalletAddress,
                    MatchId = b.MatchId,
                    MatchExternalId = b.Match.ExternalId,
                    HomeTeam = b.Match.HomeTeam,
                    AwayTeam = b.Match.AwayTeam,
                    StartTime = b.Match.StartTime,
                    OnChainIndex = b.OnChainIndex,
                    Pick = b.Pick,
                    Amount = b.Amount,
                    PlayAmount = b.PlayAmount,
                    OddsAtMoment = b.OddsAtMoment,
                    BetTime = b.BetTime,
                    BlockchainTxHash = b.BlockchainTxHash,
                    Status = b.Status.ToString()
                })
                .ToList();

            return Ok(bets);
        }
    }
}

