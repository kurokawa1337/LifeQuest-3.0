using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LifeQuest.Services;

public sealed record WeatherInfo(string City, string Country, double TempC, double FeelsC, int Code, double WindMs);
public sealed record QuoteInfo(string Text, string Author);

public sealed record DailyBrief(WeatherInfo? Weather, QuoteInfo? Quote, string Category)
{
    public static readonly DailyBrief Empty = new(null, null, "any");
}

public static class DailyBriefService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static async Task<WeatherInfo?> GetWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;

        var geo = await Http.GetFromJsonAsync<GeoResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city.Trim())}" +
            "&count=1&language=uk&format=json");
        var g = geo?.Results?.FirstOrDefault();
        if (g == null) return null;

        var w = await Http.GetFromJsonAsync<ForecastResponse>(
            $"https://api.open-meteo.com/v1/forecast?latitude={g.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&longitude={g.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            "&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m&windspeed_unit=ms");
        if (w?.Current == null) return null;

        return new WeatherInfo(g.Name, g.Country ?? "", w.Current.Temperature2m, w.Current.ApparentTemperature,
            w.Current.WeatherCode, w.Current.WindSpeed10m);
    }

    public static async Task<QuoteInfo?> GetQuoteAsync()
    {
        var q = await Http.GetFromJsonAsync<QuoteResponse>("https://dummyjson.com/quotes/random");
        return q == null ? null : new QuoteInfo(q.Quote, q.Author);
    }

    public static async Task<DailyBrief> GetBriefAsync(string city)
    {
        var weatherTask = GetWeatherAsync(city);
        var quoteTask = GetQuoteAsync();
        try
        {
            await Task.WhenAll(weatherTask, quoteTask);
        }
        catch
        {
            await Task.WhenAll(
                Ignore(weatherTask),
                Ignore(quoteTask));
        }

        var weather = weatherTask.Status == TaskStatus.RanToCompletion ? weatherTask.Result : null;
        var quote = quoteTask.Status == TaskStatus.RanToCompletion ? quoteTask.Result : null;
        return new DailyBrief(weather, quote, WeatherCategory(weather?.Code));
    }

    private static async Task Ignore(Task t)
    {
        try { await t; } catch { }
    }

    public static string WeatherCategory(int? code) => code switch
    {
        0 => "clear",
        1 or 2 or 3 => "clouds",
        45 or 48 => "fog",
        >= 95 => "storm",
        >= 71 and <= 77 or 85 or 86 => "snow",
        >= 51 => "rain",
        _ => "any"
    };

    private sealed class GeoResponse
    {
        [JsonPropertyName("results")]
        public List<GeoPlace>? Results { get; set; }
    }

    private sealed class GeoPlace
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("current")]
        public CurrentBlock? Current { get; set; }
    }

    private sealed class CurrentBlock
    {
        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; set; }
        [JsonPropertyName("apparent_temperature")]
        public double ApparentTemperature { get; set; }
        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; set; }
    }

    private sealed class QuoteResponse
    {
        [JsonPropertyName("quote")]
        public string Quote { get; set; } = "";
        [JsonPropertyName("author")]
        public string Author { get; set; } = "";
    }
}
