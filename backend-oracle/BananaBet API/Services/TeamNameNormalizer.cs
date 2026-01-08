namespace BananaBet_API.Services
{
    public static class TeamNameNormalizer
    {
        private static readonly Dictionary<string, string> Map =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // ===== Arsenal =====
                ["Arsenal FC"] = "Arsenal",
                ["Arsenal"] = "Arsenal",

                // ===== Manchester City =====
                ["Manchester City FC"] = "Man City",
                ["Manchester City"] = "Man City",
                ["Man City"] = "Man City",

                // ===== Aston Villa =====
                ["Aston Villa FC"] = "Aston Villa",
                ["Aston Villa"] = "Aston Villa",

                // ===== Liverpool =====
                ["Liverpool FC"] = "Liverpool",
                ["Liverpool"] = "Liverpool",

                // ===== Chelsea =====
                ["Chelsea FC"] = "Chelsea",
                ["Chelsea"] = "Chelsea",

                // ===== Manchester United =====
                ["Manchester United FC"] = "Man United",
                ["Manchester United"] = "Man United",
                ["Man United"] = "Man United",

                // ===== Sunderland =====
                ["Sunderland AFC"] = "Sunderland",
                ["Sunderland"] = "Sunderland",

                // ===== Everton =====
                ["Everton FC"] = "Everton",
                ["Everton"] = "Everton",

                // ===== Brentford =====
                ["Brentford FC"] = "Brentford",
                ["Brentford"] = "Brentford",

                // ===== Crystal Palace =====
                ["Crystal Palace FC"] = "Crystal Palace",
                ["Crystal Palace"] = "Crystal Palace",

                // ===== Fulham =====
                ["Fulham FC"] = "Fulham",
                ["Fulham"] = "Fulham",

                // ===== Tottenham =====
                ["Tottenham Hotspur FC"] = "Tottenham",
                ["Tottenham Hotspur"] = "Tottenham",
                ["Tottenham"] = "Tottenham",

                // ===== Newcastle =====
                ["Newcastle United FC"] = "Newcastle",
                ["Newcastle United"] = "Newcastle",
                ["Newcastle"] = "Newcastle",

                // ===== Brighton =====
                ["Brighton & Hove Albion FC"] = "Brighton",
                ["Brighton & Hove Albion"] = "Brighton",
                ["Brighton"] = "Brighton",

                // ===== Bournemouth =====
                ["AFC Bournemouth"] = "Bournemouth",
                ["Bournemouth"] = "Bournemouth",

                // ===== Leeds =====
                ["Leeds United FC"] = "Leeds",
                ["Leeds United"] = "Leeds",
                ["Leeds"] = "Leeds",

                // ===== Nottingham Forest =====
                ["Nottingham Forest FC"] = "Nott'm Forest",
                ["Nottingham Forest"] = "Nott'm Forest",
                ["Forest"] = "Nott'm Forest",
                ["Nottingham"] = "Nott'm Forest",
                ["Nott'm Forest"] = "Nott'm Forest",

                // ===== West Ham =====
                ["West Ham United FC"] = "West Ham",
                ["West Ham United"] = "West Ham",
                ["West Ham"] = "West Ham",

                // ===== Burnley =====
                ["Burnley FC"] = "Burnley",
                ["Burnley"] = "Burnley",

                // ===== Wolves =====
                ["Wolverhampton Wanderers FC"] = "Wolves",
                ["Wolverhampton Wanderers"] = "Wolves",
                ["Wolves"] = "Wolves",
            };

        public static string Normalize(string name)
        {
            var key = name.Trim();

            if (Map.TryGetValue(key, out var normalized))
                return normalized;

            throw new Exception($"❌ Unknown team name: {name}");
        }
    }

}
