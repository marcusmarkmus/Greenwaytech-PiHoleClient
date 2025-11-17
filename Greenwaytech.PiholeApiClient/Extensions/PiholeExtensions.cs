using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using System.IO.Compression;
using System.Text.Json;

namespace Greenwaytech.PiholeApiClient.Extensions;

internal static class PiholeExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    internal static readonly HashSet<string> _skipArchiveEntriesList =
    [
        
            "etc/pihole/dhcp.leases",
            "etc/pihole/pihole-FTL.db",
            "etc/hosts"
    ];

    internal static PiholeApiSession GetPiholeApiSession(this PiholeAuthResponse piholeAuthResponse)
    {
        return new PiholeApiSession
        {
            Valid = piholeAuthResponse.Session?.Valid,
            Totp = piholeAuthResponse.Session?.Totp,
            Sid = piholeAuthResponse.Session?.Sid,
            Csrf = piholeAuthResponse.Session?.Csrf,
            Validity = piholeAuthResponse.Session?.Validity,
            Message = piholeAuthResponse.Session?.Message,
            PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow
        };
    }
    internal static string ComputeHash(this object data)
    {
        // Serialize the object to JSON
        var jsonData = JsonSerializer.Serialize(data, _jsonSerializerOptions);
        // Convert the JSON string to a byte array
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
        // Compute the SHA256 hash of the byte array
        var hashBytes = System.Security.Cryptography.SHA256.HashData(jsonBytes);
        // Convert the hash bytes to a lowercase hexadecimal string
        return Convert.ToHexStringLower(hashBytes);
    }

    internal static string ComputeHash(this byte[] data)
    {
        //data is a zip, extract the content and compute the hash
        var zipContents = ExtractZipContents(data);

        var hashBytes = System.Security.Cryptography.SHA256.HashData([.. zipContents.SelectMany(kv => kv.Value)]);
        return Convert.ToHexStringLower(hashBytes);
    }




    internal static Dictionary<string, byte[]> ExtractZipContents(byte[] zipBytes)
    {
        var result = new Dictionary<string, byte[]>();

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
        {
            if( _skipArchiveEntriesList.Contains(entry.FullName))
            {
                continue; // Skip entries that are in the skip list
            }
            using var entryStream = entry.Open();
            using var msEntry = new MemoryStream();
            entryStream.CopyTo(msEntry);
            result[entry.FullName] = msEntry.ToArray();
        }

        return result;
    }

}
