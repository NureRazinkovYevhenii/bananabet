namespace BananaBet_API.DTO
{
    public class MatchFeaturesDto
    {
        public double Elo_Diff_Norm { get; set; }
        public double Elo_Signed_Sqrt { get; set; }
        public double Adj_Shots_Diff { get; set; }
        public double Form3_Diff { get; set; }
        public double Form5_Diff { get; set; }
    }

}
