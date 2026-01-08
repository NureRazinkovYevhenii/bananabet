namespace BananaBet_API.DTO
{
    public class BetDto
    {
        public int Id { get; set; }
        public string UserWalletAddress { get; set; } = null!;
        public int MatchId { get; set; }
        public string MatchExternalId { get; set; } = null!;
        public string HomeTeam { get; set; } = null!;
        public string AwayTeam { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public int Pick { get; set; } // 1 = Home, 2 = Away
        public int OnChainIndex { get; set; }
        public decimal Amount { get; set; }
        public decimal PlayAmount { get; set; }
        public decimal OddsAtMoment { get; set; }
        public DateTime BetTime { get; set; }
        public string BlockchainTxHash { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}

