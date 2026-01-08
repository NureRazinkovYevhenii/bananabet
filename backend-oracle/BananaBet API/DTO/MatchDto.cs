using BananaBet_API.Models;

namespace BananaBet_API.DTO
{
    public class MatchDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = null!;
        public string HomeTeam { get; set; } = null!;
        public string AwayTeam { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public decimal OddsHome { get; set; }
        public decimal OddsAway { get; set; }
        public MatchPipelineStatus Status { get; set; }
    }
}

