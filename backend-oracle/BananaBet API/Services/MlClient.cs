using BananaBet_API.DTO;

namespace BananaBet_API.Services
{
    public class MlClient
    {
        private readonly HttpClient _http;

        public MlClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<MlPrediction> PredictAsync(MatchFeaturesDto features)
        {
            var res = await _http.PostAsJsonAsync("/predict", features);

            if (!res.IsSuccessStatusCode)
                throw new Exception("ML service error");

            return await res.Content.ReadFromJsonAsync<MlPrediction>()
                   ?? throw new Exception("Invalid ML response");
        }
    }

    public class MlPrediction
    {
        public double Home_Win_Prob { get; set; }
        public double Away_Win_Prob { get; set; }
        public double Fair_Odd_Home { get; set; }
        public double Fair_Odd_Away { get; set; }
    }
}
