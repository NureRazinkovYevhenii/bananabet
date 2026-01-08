namespace BananaBet_API.DTO
{
    public class FootballDataResponse
    {
        public List<FootballMatchDto> Matches { get; set; } = new();
    }

    public class FootballMatchDto
    {
        public int Id { get; set; }
        public DateTime UtcDate { get; set; }
        public TeamDto HomeTeam { get; set; } = null!;
        public TeamDto AwayTeam { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class TeamDto
    {
        public string Name { get; set; } = null!;
    }

}
