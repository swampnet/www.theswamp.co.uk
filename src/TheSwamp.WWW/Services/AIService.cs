using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace TheSwamp.WWW.Services
{
    public interface IAI
    {
        void SetSystemPrompt(string prompt);
        IAsyncEnumerable<string> StreamAsync(string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }


    public class AIService : IAI
    {
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";

        private readonly IConfiguration _cfg;
        private readonly ILogger<WineService> _logger;
        private readonly HttpClient _httpClient;
        private string _systemPrompt;

        public string? Model { get; }

        public AIService(IConfiguration cfg, ILogger<WineService> logger)
        {
            _cfg = cfg;
            _logger = logger;
            _httpClient = new HttpClient();

            Model = _cfg["OpenRouter:Model"];

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg["OpenRouter:ApiKey"]);
        }

        public void SetSystemPrompt(string prompt)
        {
            _systemPrompt = prompt;
        }

        public async IAsyncEnumerable<string> StreamAsync(string userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messages = BuildMessages(userPrompt);

            var requestBody = new
            {
                model = Model,
                stream = true,
                messages
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, API_URL)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line["data: ".Length..];

                if (data == "[DONE]")
                {
                    yield break;
                }

                var token = ExtractDeltaContent(data);

                if (token is not null)
                {
                    _logger.LogDebug(token);
                    yield return token;
                }
            }
        }

        private object[] BuildMessages(string userPrompt)
        {
            if (_systemPrompt is not null)
            {
                return
                [
                    new { role = "system", content = _systemPrompt },
                    new { role = "user", content = userPrompt }
                ];
            }

            return [new { role = "user", content = userPrompt }];
        }


        private static string? ExtractDeltaContent(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return null;
                }

                var delta = choices[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString();
                }
            }
            catch (JsonException)
            {
                // Malformed chunk — skip silently
            }

            return null;
        }
    }
}
