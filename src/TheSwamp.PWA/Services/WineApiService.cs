using System.Net.Http.Json;
using TheSwamp.PWA.Models;

namespace TheSwamp.PWA.Services;

public class WineApiService
{
	private readonly HttpClient _http;
	private readonly AppConfig _config;

	public WineApiService(HttpClient http, AppConfig config)
	{
		_http = http;
		_config = config;
	}

	public async Task<IReadOnlyCollection<WineDto>> SearchAsync(string term, CancellationToken ct = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"api/wine?term={Uri.EscapeDataString(term)}");
		request.Headers.Add("X-Api-Key", _config.ApiKey);

		using var response = await _http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<WineDto>>(ct)
			?? [];
	}
}
