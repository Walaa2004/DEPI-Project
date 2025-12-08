using System.Text;
using System.Text.Json;

namespace WebApplication1.Services
{
    public class ZoomService
    {
        private readonly string _accountId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl = "https://api.zoom.us/v2";
        private readonly HttpClient _httpClient;

        public ZoomService(IConfiguration configuration, HttpClient httpClient)
        {
            _accountId = configuration["Zoom:AccountId"];
            _clientId = configuration["Zoom:ClientId"];
            _clientSecret = configuration["Zoom:ClientSecret"];
            _httpClient = httpClient;
        }

        public async Task<ZoomMeetingResponse> CreateMeetingAsync(string topic, DateTime startTime, int durationMinutes, string doctorEmail)
        {
            try
            {
                // Get access token first
                var accessToken = await GetAccessTokenAsync();

                var meetingRequest = new
                {
                    topic = topic,
                    type = 2, // Scheduled meeting
                    start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    duration = durationMinutes,
                    timezone = "Africa/Cairo", // Egypt timezone
                    settings = new
                    {
                        host_video = true,
                        participant_video = true,
                        join_before_host = true,
                        mute_upon_entry = false,
                        watermark = false,
                        use_pmi = false,
                        approval_type = 2, // No registration required
                        audio = "both",
                        auto_recording = "none",
                        waiting_room = false
                    }
                };

                var json = JsonSerializer.Serialize(meetingRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/users/me/meetings", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var meetingResponse = JsonSerializer.Deserialize<ZoomMeetingResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    return meetingResponse;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to create Zoom meeting: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating Zoom meeting: {ex.Message}", ex);
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var tokenRequest = new
            {
                grant_type = "account_credentials",
                account_id = _accountId
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            var response = await _httpClient.PostAsync("https://zoom.us/oauth/token", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return tokenResponse.AccessToken;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get access token: {response.StatusCode} - {errorContent}");
            }
        }
    }

    public class ZoomMeetingResponse
    {
        public long Id { get; set; }
        public string Topic { get; set; }
        public string StartUrl { get; set; }
        public string JoinUrl { get; set; }
        public string Password { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}