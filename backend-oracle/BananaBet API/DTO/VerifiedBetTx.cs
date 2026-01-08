namespace BananaBet_API.DTO
{
    public class VerifiedBetTx
    {
        public string MatchExternalId { get; set; } = null!;
        public int Pick { get; set; }
        public decimal Amount { get; set; }
        public string UserWalletAddress { get; set; } = null!;
    }
}

