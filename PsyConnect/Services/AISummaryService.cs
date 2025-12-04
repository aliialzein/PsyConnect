using System.Text;
using System.Text.Json;

public class AISummaryService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public AISummaryService(IConfiguration config, HttpClient http)
    {
        _config = config;
        _http = http;
    }

    public async Task<string> GenerateAdminSummary(object stats)
    {
        string apiKey = _config["AI:ApiKey"];

        var prompt = $@"
Generate a short professional admin summary for a psychotherapy platform.
Use the data below and give clear business insight.

DATA:
{JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true })}

Return only a short summary paragraph.
";

        var requestBody = new
        {
            model = _config["AI:Model"],
            messages = new[]
            {
                new { role = "system", content = "You are a business analytics assistant." },
                new { role = "user", content = prompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine(json);
        using var doc = JsonDocument.Parse(json);

        // If OpenAI returned error (like 429, invalid key, etc...)
        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            return "AI unavailable: " + error.GetProperty("message").GetString();
        }

        // If response did not include choices (unexpected failure)
        if (!doc.RootElement.TryGetProperty("choices", out var choices))
        {
            return "AI unavailable (no data returned)";
        }

        // Normal successful response
        return choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();


    }
}
