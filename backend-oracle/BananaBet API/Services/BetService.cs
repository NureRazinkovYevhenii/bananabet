using BananaBet_API.DTO;
using BananaBet_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BananaBet_API.Services
{
    public class BetService
    {
        private readonly BettingDbContext _db;
        private readonly BlockchainTxVerifierService _verifier;
        private readonly BlockchainClient _blockchain; // Added
        private readonly ILogger<BetService> _logger;

        public BetService(
            BettingDbContext db,
            BlockchainTxVerifierService verifier,
            BlockchainClient blockchain, // Added
            ILogger<BetService> logger)
        {
            _db = db;
            _verifier = verifier;
            _blockchain = blockchain; // Added
            _logger = logger;
        }

        public async Task<Bet> CreateAsync(CreateBetRequest request, CancellationToken ct)
        {
            if (request.Pick is not (1 or 2))
            {
                _logger.LogWarning("Invalid pick {Pick} for tx {Tx}", request.Pick, request.TxHash);
                throw new BetValidationException("Invalid pick");
            }

            if (request.Amount <= 0)
            {
                _logger.LogWarning("Invalid amount {Amount} for tx {Tx}", request.Amount, request.TxHash);
                throw new BetValidationException("Invalid amount");
            }

            if (await _db.Bets.AnyAsync(b => b.BlockchainTxHash == request.TxHash, ct))
            {
                _logger.LogWarning("Duplicate tx hash {TxHash}", request.TxHash);
                throw new BetConflictException("Tx already used");
            }

            var verified = await _verifier.VerifyPlaceBetAsync(request.TxHash);
            if (verified == null)
            {
                throw new BetValidationException("Transaction verification failed");
            }

            if (!string.Equals(request.UserWalletAddress, verified.UserWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Sender mismatch for tx {Tx}: req {ReqSender} vs chain {ChainSender}", request.TxHash, request.UserWalletAddress, verified.UserWalletAddress);
                throw new BetValidationException("Sender mismatch");
            }

            if (request.Pick != verified.Pick)
            {
                _logger.LogWarning("Pick mismatch for tx {Tx}: req {ReqPick} vs chain {ChainPick}", request.TxHash, request.Pick, verified.Pick);
                throw new BetValidationException("Pick mismatch");
            }

            if (Math.Abs(request.Amount - verified.Amount) > 0.0000001m)
            {
                _logger.LogWarning("Amount mismatch for tx {Tx}: req {ReqAmount} vs chain {ChainAmount}", request.TxHash, request.Amount, verified.Amount);
                throw new BetValidationException("Amount mismatch");
            }

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);
            if (match == null)
            {
                _logger.LogWarning("Match not found {MatchId} for tx {Tx}", request.MatchId, request.TxHash);
                throw new BetValidationException("Match not found");
            }

            if (match.Status != MatchPipelineStatus.Open)
            {
                _logger.LogWarning("Match not open {MatchId} status {Status} for tx {Tx}", request.MatchId, match.Status, request.TxHash);
                throw new BetValidationException("Match is not open");
            }

            if (!string.Equals(match.ExternalId, verified.MatchExternalId, StringComparison.Ordinal))
            {
                _logger.LogWarning("ExternalId mismatch for match {MatchId}: db {DbExtId} vs chain {ChainExtId}", request.MatchId, match.ExternalId, verified.MatchExternalId);
                throw new BetValidationException("ExternalId mismatch");
            }

            var odds = request.Pick == 1 ? match.OddsHome : match.OddsAway;

            // --- Determine OnChainIndex ---
            if (!System.Numerics.BigInteger.TryParse(match.ExternalId, out var extId))
                throw new BetValidationException("Invalid Match External Id");

            var allChainBets = await _blockchain.GetBetsByMatchAsync(extId, ct);
            
            // Find all indices on-chain strictly matching our verified User, Amount, Pick
            var candidateIndices = allChainBets
                .Where(b => 
                    b.User.Equals(verified.UserWalletAddress, StringComparison.OrdinalIgnoreCase) && 
                    b.Side == verified.Pick && 
                    Math.Abs(b.Amount - verified.Amount) < 0.000001m)
                .Select(b => b.OnChainIndex)
                .ToList();

            if (!candidateIndices.Any())
            {
                 // This might happen if 'GetBetsByMatchAsync' sees slightly stale state (race between tx confirmation and query?)
                 // But VerifyPlaceBetAsync confirmed it was successful.
                 // Maybe RPC node lag. Retrying or failing.
                 throw new BetValidationException("Bet not found in contract state yet (indexing lag?)");
            }

            // Find which indices are already occupied in our DB
            var usedIndices = await _db.Bets
                .Where(b => b.MatchId == match.Id && candidateIndices.Contains(b.OnChainIndex))
                .Select(b => b.OnChainIndex)
                .ToListAsync(ct);

            // First index that is NOT in usedIndices
            var freeIndex = candidateIndices.Except(usedIndices).OrderBy(x => x).FirstOrDefault(-1);

            if (freeIndex == -1)
            {
                 // Ambiguity: We see N bets on chain, and we have N bets in DB.
                 // But user claims this is a NEW bet (checked by TxHash uniqueness earlier).
                 // This means on-chain state has fewer bets than we expect? 
                 // Or we actually found N+1 bets?
                 throw new BetConflictException("Potential duplicate processing or sync lag.");
            }

            var bet = new Bet
            {
                MatchId = match.Id,
                Amount = request.Amount,
                PlayAmount = 0m,                 // ще не матчилась
                Pick = request.Pick,
                OddsAtMoment = odds,
                UserWalletAddress = request.UserWalletAddress,
                BlockchainTxHash = request.TxHash,
                BetTime = DateTime.UtcNow,
                Status = BetStatus.Placed,
                OnChainIndex = freeIndex
            };

            _db.Bets.Add(bet);
            await _db.SaveChangesAsync(ct);

            return bet;
        }
    }

    public class BetValidationException : Exception
    {
        public BetValidationException(string message) : base(message) { }
    }

    public class BetConflictException : Exception
    {
        public BetConflictException(string message) : base(message) { }
    }
}

