using BananaBet_API.Models;
using BananaBet_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BananaBet_API.Controllers
{
    public class ClaimRequest
    {
        public string TxHash { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/claims")]
    public class ClaimController : ControllerBase
    {
        private readonly BlockchainTxVerifierService _verifier;
        private readonly BettingDbContext _db;

        public ClaimController(BlockchainTxVerifierService verifier, BettingDbContext db)
        {
            _verifier = verifier;
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Claim([FromBody] ClaimRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(request.TxHash))
                return BadRequest("TxHash is required");

            var verification = await _verifier.VerifyClaimAsync(request.TxHash);
            if (!verification.Valid || verification.User == null)
                return BadRequest("Invalid transaction or claim failed");

            // Look for bets that are either Win or Refunded for this match and user.
            var bets = await _db.Bets
                .Include(b => b.Match)
                .Where(b => b.Match.ExternalId == verification.ExternalId &&
                            b.UserWalletAddress == verification.User.ToLower() &&
                            (b.Status == BetStatus.Win || b.Status == BetStatus.Refunded))
                .ToListAsync(ct);

            if (bets.Count == 0)
            {
                 // Check if already claimed
                 var claimedBets = await _db.Bets
                    .Include(b => b.Match)
                    .Where(b => b.Match.ExternalId == verification.ExternalId &&
                                b.UserWalletAddress == verification.User.ToLower() &&
                                b.Status == BetStatus.Claimed)
                    .AnyAsync(ct);
                 
                 if (claimedBets) return Ok(new { message = "Already claimed" });
                 
                 return NotFound("No claimable bets found for this match/user");
            }

            foreach (var bet in bets)
            {
                bet.Status = BetStatus.Claimed;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { count = bets.Count, newStatus = "Claimed" });
        }
    }
}
