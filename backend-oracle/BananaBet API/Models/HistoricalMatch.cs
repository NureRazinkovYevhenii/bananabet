namespace BananaBet_API.Models
{
    public class HistoricalMatch
    {
        public int Id { get; set; }

        public DateTime MatchDate { get; set; }

        public string HomeTeam { get; set; } = null!;
        public string AwayTeam { get; set; } = null!;

        // Elo рейтинги команд на момент матчу
        public double HomeElo { get; set; }
        public double AwayElo { get; set; }

        // Форма команд (останні N матчів)
        public int Form3Home { get; set; }
        public int Form5Home { get; set; }
        public int Form3Away { get; set; }
        public int Form5Away { get; set; }

        // Забиті голи (або xG / shots on target — залежить від датасету)
        public int HomeTarget { get; set; }
        public int AwayTarget { get; set; }
    }
}
