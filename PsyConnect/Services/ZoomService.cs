using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class ZoomService
{
    private readonly HttpClient _http;
    private readonly ZoomSettings _settings;

    public ZoomService(HttpClient http, IOptions<ZoomSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var values = new Dictionary<string, string>
        {
            { "grant_type", "account_credentials" },
            { "account_id", _settings.AccountId }
        };

        var authHeader = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}")
        );

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://zoom.us/oauth/token"
        )
        {
            Content = new FormUrlEncodedContent(values)
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Basic", authHeader);

        var response = await _http.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Zoom TOKEN error: " + result);

        return JsonDocument.Parse(result)
            .RootElement.GetProperty("access_token")
            .GetString();
    }

    public async Task<string> CreateMeetingAsync(string topic, DateTime time)
    {
        var token = await GetAccessTokenAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.zoom.us/v2/users/me/meetings"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            topic = topic,
            type = 2,
            start_time = time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            duration = 60,
            timezone = "Asia/Beirut",
            settings = new
            {
                join_before_host = true,
                approval_type = 0,
                waiting_room = false
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Zoom API error: " + result);

        return JsonDocument.Parse(result)
            .RootElement.GetProperty("join_url")
            .GetString();
    }
}
