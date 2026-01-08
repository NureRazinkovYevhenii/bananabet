using BananaBet_API.DTO;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BananaBet_API.Services
{
    using System.Text.Json;

    public class FootballDataClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public FootballDataClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            _http.BaseAddress = new Uri(_config["FootballData:BaseUrl"]!);
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add(
                "X-Auth-Token",
                _config["FootballData:ApiKey"]!
            );
        }

        public async Task<List<UpcomingMatch>> GetUpcomingMatchesAsync(string competitionCode = "PL",int limit = 5)
        {
            var url = $"competitions/{competitionCode}/matches?status=SCHEDULED";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Football-Data error: {body}");
            }

            var stream = await response.Content.ReadAsStreamAsync();

            var data = await JsonSerializer.DeserializeAsync<FootballDataResponse>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (data?.Matches == null)
                return new List<UpcomingMatch>();

            return data.Matches
                .OrderBy(m => m.UtcDate)
                .Take(limit)
                .Select(m => new UpcomingMatch
                {
                    ExternalId = m.Id.ToString(),
                    HomeTeam = TeamNameNormalizer.Normalize(m.HomeTeam.Name),
                    AwayTeam = TeamNameNormalizer.Normalize(m.AwayTeam.Name),
                    StartTime = m.UtcDate
                })
                .ToList();
        }

        public async Task<MatchResultDto?> GetMatchResultAsync(string externalId)
        {
            var response = await _http.GetAsync($"matches/{externalId}");

            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("match", out var match))
                return null;

            var status = match.GetProperty("status").GetString();
            if (status != "FINISHED")
                return new MatchResultDto { Finished = false };

            var fullTime = match
                .GetProperty("score")
                .GetProperty("fullTime");

            return new MatchResultDto
            {
                Finished = true,
                HomeGoals = fullTime.GetProperty("home").GetInt32(),
                AwayGoals = fullTime.GetProperty("away").GetInt32()
            };
        }
    }



}
