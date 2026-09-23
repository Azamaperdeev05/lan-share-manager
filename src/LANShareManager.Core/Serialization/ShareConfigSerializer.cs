using System.Text.Json;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;

namespace LANShareManager.Core.Serialization;

public class ShareConfigSerializer : IShareConfigSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(IEnumerable<ShareExportConfig> configs)
    {
        return JsonSerializer.Serialize(configs, JsonOptions);
    }

    public IReadOnlyList<ShareExportConfig> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<ShareExportConfig>();

        try
        {
            var result = JsonSerializer.Deserialize<List<ShareExportConfig>>(json, JsonOptions);
            return result ?? new List<ShareExportConfig>();
        }
        catch
        {
            // If single object was provided instead of array
            try
            {
                var single = JsonSerializer.Deserialize<ShareExportConfig>(json, JsonOptions);
                return single != null ? new List<ShareExportConfig> { single } : Array.Empty<ShareExportConfig>();
            }
            catch
            {
                return Array.Empty<ShareExportConfig>();
            }
        }
    }
}
