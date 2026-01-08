namespace BananaBet_API.DTO
{
    public class CreateBetRequest
    {
        public string TxHash { get; set; } = null!;
        public int MatchId { get; set; }
        public int Pick { get; set; } // 1 = Home, 2 = Away
        public decimal Amount { get; set; }
        public string UserWalletAddress { get; set; } = null!;
    }
}

