namespace BananaBet_API.DTO
{
    public class UpcomingMatch
    {
        public string ExternalId { get; set; } = null!;
        public string HomeTeam { get; set; } = null!;
        public string AwayTeam { get; set; } = null!;
        public DateTime StartTime { get; set; }
    }

}
