using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SportHub.Services
{
    public record GeocodeResult(double Lat, double Lon, string DisplayName, string Source);

    public interface IGeocodingService
    {
        Task<GeocodeResult?> ResolveAsync(string? address, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GeocodeResult>> SearchAsync(string? query, int limit = 5, CancellationToken cancellationToken = default);
    }

    public class GeocodingService : IGeocodingService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GeocodingService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GeocodeResult?> ResolveAsync(string? address, CancellationToken cancellationToken = default)
        {
            var results = await SearchAsync(address, 1, cancellationToken);
            return results.FirstOrDefault();
        }

        public async Task<IReadOnlyList<GeocodeResult>> SearchAsync(string? query, int limit = 5, CancellationToken cancellationToken = default)
        {
            var q = query?.Trim();
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
                return Array.Empty<GeocodeResult>();

            limit = Math.Clamp(limit, 1, 10);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<GeocodeResult>();

            void Add(GeocodeResult? item)
            {
                if (item == null) return;
                var key = $"{item.Lat:F5},{item.Lon:F5}";
                if (seen.Add(key))
                    results.Add(item);
            }

            var parsed = VietnameseAddressParser.TryParse(q);
            if (parsed != null)
            {
                foreach (var item in await NominatimStructuredSearchAsync(parsed, limit, cancellationToken))
                    Add(item);
            }

            foreach (var item in await NominatimFreeTextSearchAsync(q, limit, cancellationToken))
                Add(item);

            if (results.Count < limit)
            {
                foreach (var item in await PhotonSearchAsync(q, limit, cancellationToken))
                    Add(item);
            }

            return results.Take(limit).ToList();
        }

        private async Task<IReadOnlyList<GeocodeResult>> NominatimStructuredSearchAsync(
            ParsedVietnameseAddress parsed,
            int limit,
            CancellationToken cancellationToken)
        {
            var query = new List<string>
            {
                "format=json",
                "countrycodes=vn",
                $"limit={limit}",
                "addressdetails=1"
            };

            if (!string.IsNullOrWhiteSpace(parsed.HouseNumber))
                query.Add($"housenumber={Uri.EscapeDataString(parsed.HouseNumber)}");
            if (!string.IsNullOrWhiteSpace(parsed.Street))
                query.Add($"street={Uri.EscapeDataString(parsed.Street)}");
            if (!string.IsNullOrWhiteSpace(parsed.City))
                query.Add($"city={Uri.EscapeDataString(parsed.City)}");

            if (string.IsNullOrWhiteSpace(parsed.Street))
                return Array.Empty<GeocodeResult>();

            var url = $"https://nominatim.openstreetmap.org/search?{string.Join("&", query)}";
            return await FetchNominatimAsync(url, "structured", cancellationToken);
        }

        private async Task<IReadOnlyList<GeocodeResult>> NominatimFreeTextSearchAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            var url =
                "https://nominatim.openstreetmap.org/search?format=json" +
                $"&q={Uri.EscapeDataString(query)}&countrycodes=vn&addressdetails=1&limit={limit}";
            return await FetchNominatimAsync(url, "nominatim", cancellationToken);
        }

        private async Task<IReadOnlyList<GeocodeResult>> PhotonSearchAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = CreateClient();
                var url = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(query)}&limit={limit}&lang=vi";
                var json = await client.GetStringAsync(url, cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("features", out var features) ||
                    features.ValueKind != JsonValueKind.Array)
                    return Array.Empty<GeocodeResult>();

                var list = new List<GeocodeResult>();
                foreach (var feature in features.EnumerateArray())
                {
                    if (!feature.TryGetProperty("geometry", out var geom) ||
                        !geom.TryGetProperty("coordinates", out var coords) ||
                        coords.GetArrayLength() < 2)
                        continue;

                    var lon = coords[0].GetDouble();
                    var lat = coords[1].GetDouble();
                    var name = feature.TryGetProperty("properties", out var props)
                        ? BuildPhotonLabel(props)
                        : query;
                    list.Add(new GeocodeResult(lat, lon, name, "photon"));
                }

                return list;
            }
            catch
            {
                return Array.Empty<GeocodeResult>();
            }
        }

        private async Task<IReadOnlyList<GeocodeResult>> FetchNominatimAsync(
            string url,
            string source,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = CreateClient();
                var json = await client.GetStringAsync(url, cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return Array.Empty<GeocodeResult>();

                var list = new List<GeocodeResult>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (!el.TryGetProperty("lat", out var latEl) || !el.TryGetProperty("lon", out var lonEl))
                        continue;
                    if (!double.TryParse(latEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                        continue;
                    if (!double.TryParse(lonEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                        continue;

                    var display = el.TryGetProperty("display_name", out var dn)
                        ? dn.GetString() ?? string.Empty
                        : string.Empty;
                    list.Add(new GeocodeResult(lat, lon, display, source));
                }

                return list;
            }
            catch
            {
                return Array.Empty<GeocodeResult>();
            }
        }

        private static string BuildPhotonLabel(JsonElement props)
        {
            var parts = new List<string>();
            if (props.TryGetProperty("housenumber", out var hn) && hn.ValueKind == JsonValueKind.String)
                parts.Add(hn.GetString()!);
            if (props.TryGetProperty("street", out var st) && st.ValueKind == JsonValueKind.String)
                parts.Add(st.GetString()!);
            if (props.TryGetProperty("city", out var city) && city.ValueKind == JsonValueKind.String)
                parts.Add(city.GetString()!);
            else if (props.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                parts.Add(name.GetString()!);
            return parts.Count > 0 ? string.Join(", ", parts) : "Địa điểm";
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient(nameof(GeocodingService));
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SportHub/1.0 (geocoding)");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "vi");
            return client;
        }
    }

    public sealed class ParsedVietnameseAddress
    {
        public string? HouseNumber { get; init; }
        public string Street { get; init; } = string.Empty;
        public string? City { get; init; }
    }

    public static class VietnameseAddressParser
    {
        private static readonly Regex PatternWithCity = new(
            @"^(?:(?:số|so)\s*)?(?<num>\d+[A-Za-z]?(?:/\d+)?)\s+(?<street>[^,]+?)\s*,\s*(?<city>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex PatternNoCity = new(
            @"^(?:(?:số|so)\s*)?(?<num>\d+[A-Za-z]?(?:/\d+)?)\s+(?<street>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static ParsedVietnameseAddress? TryParse(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var text = input.Trim();
            var match = PatternWithCity.Match(text);
            if (!match.Success)
                match = PatternNoCity.Match(text);

            if (!match.Success)
                return null;

            var street = match.Groups["street"].Value.Trim();
            if (string.IsNullOrWhiteSpace(street))
                return null;

            return new ParsedVietnameseAddress
            {
                HouseNumber = match.Groups["num"].Value.Trim(),
                Street = street,
                City = match.Groups["city"].Success ? match.Groups["city"].Value.Trim() : null
            };
        }
    }
}
