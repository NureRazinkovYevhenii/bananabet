namespace BananaBet_API.DTO
{
    public class OrderBookDto
    {
        public decimal HomeTotal { get; set; }
        public decimal AwayTotal { get; set; }

        // Convenience for UI
        public decimal Total => HomeTotal + AwayTotal;

        public decimal HomeShare => Total == 0 ? 0 : HomeTotal / Total;

        public decimal AwayShare => Total == 0 ? 0 : AwayTotal / Total;
    }
}

