namespace BananaBet_API.Models
{
    public class Match
    {
        public int Id { get; set; }

        // ID із зовнішнього постачальника (має бути унікальним)
        public string ExternalId { get; set; } = null!;

        public string HomeTeam { get; set; } = null!;
        public string AwayTeam { get; set; } = null!;

        // Дата/час початку у UTC
        public DateTime StartTime { get; set; }

        // Коефіцієнти, розраховані ML-модулем
        public decimal OddsHome { get; set; }
        public decimal OddsAway { get; set; }

        // Статус state-machine пайплайну
        public MatchPipelineStatus Status { get; set; } = MatchPipelineStatus.Fetched;

        // Home | Away | Draw (після завершення)
        public string? Result { get; set; }

        public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    }
}
