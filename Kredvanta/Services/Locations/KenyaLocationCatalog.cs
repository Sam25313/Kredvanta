using System.Text.Json;

namespace Kredvanta.Services.Locations;

public interface IKenyaLocationCatalog
{
    bool ContainsCounty(string? county);

    bool ContainsSubCounty(string? county, string? subCounty);

    bool ContainsWard(string? county, string? subCounty, string? ward);
}

public sealed class KenyaLocationCatalog : IKenyaLocationCatalog
{
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _locations;

    public KenyaLocationCatalog(IWebHostEnvironment environment)
    {
        var filePath = Path.Combine(environment.WebRootPath, "data", "kenya-locations.json");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Kenya location data was not found.", filePath);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        var counties = document.RootElement.GetProperty("counties");
        _locations = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var countyElement in counties.EnumerateArray())
        {
            var countyName = countyElement.GetProperty("name").GetString()
                ?? throw new InvalidDataException("A county in the location data has no name.");
            var subCounties = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var subCountyElement in countyElement.GetProperty("subCounties").EnumerateArray())
            {
                var subCountyName = subCountyElement.GetProperty("name").GetString()
                    ?? throw new InvalidDataException("A sub-county in the location data has no name.");
                var wards = subCountyElement.GetProperty("wards")
                    .EnumerateArray()
                    .Select(ward => ward.GetString()
                        ?? throw new InvalidDataException("A ward in the location data has no name."))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                subCounties.Add(subCountyName, wards);
            }

            _locations.Add(countyName, subCounties);
        }

        if (_locations.Count == 0)
        {
            throw new InvalidDataException("Kenya location data contains no counties.");
        }
    }

    public bool ContainsCounty(string? county) =>
        !string.IsNullOrWhiteSpace(county) && _locations.ContainsKey(county.Trim());

    public bool ContainsSubCounty(string? county, string? subCounty) =>
        !string.IsNullOrWhiteSpace(county) &&
        !string.IsNullOrWhiteSpace(subCounty) &&
        _locations.TryGetValue(county.Trim(), out var subCounties) &&
        subCounties.ContainsKey(subCounty.Trim());

    public bool ContainsWard(string? county, string? subCounty, string? ward) =>
        !string.IsNullOrWhiteSpace(county) &&
        !string.IsNullOrWhiteSpace(subCounty) &&
        !string.IsNullOrWhiteSpace(ward) &&
        _locations.TryGetValue(county.Trim(), out var subCounties) &&
        subCounties.TryGetValue(subCounty.Trim(), out var wards) &&
        wards.Contains(ward.Trim());
}
