namespace BananaBet_API.DTO
{
    public class ApiMatchDto
    {
        public FixtureDto Fixture { get; set; } = null!;
        public TeamsDto Teams { get; set; } = null!;
    }

    public class FixtureDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

    public class TeamsDto
    {
        public TeamDto Home { get; set; } = null!;
        public TeamDto Away { get; set; } = null!;
    }

}
