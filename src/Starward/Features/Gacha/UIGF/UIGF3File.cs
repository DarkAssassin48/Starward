using Starward.Core.Gacha;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Features.Gacha.UIGF;

public class UIGF3File<T> where T : GachaLogItem
{

    [JsonPropertyName("info")]
    public UIAF3FileInfo Info { get; set; }


    [JsonPropertyName("list")]
    public List<T> List { get; set; }


    internal static UIGF3File<T> ParseForImport(string json)
    {
        var archive = JsonSerializer.Deserialize<UIGF3File<T>>(json);
        if (archive?.Info is not { Uid: > 0 } info || archive.List is not { Count: > 0 } list ||
            list.Exists(static item => item is null))
        {
            throw new InvalidDataException("A legacy gacha archive must have an account UID and a non-empty list of records.");
        }
        foreach (var item in list)
        {
            item.Lang ??= info.Lang ?? "";
            if (item.Uid == 0)
            {
                item.Uid = info.Uid;
            }
        }
        return archive;
    }

}



public class UIAF3FileInfo
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    [JsonPropertyName("export_timestamp")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ExportTimestamp { get; set; }

    [JsonPropertyName("export_time")]
    public string ExportTime { get; set; }

    [JsonPropertyName("export_app")]
    public string ExportApp { get; set; } = "Starward";

    [JsonPropertyName("export_app_version")]
    public string ExportAppVersion { get; set; }

    [JsonPropertyName("uigf_version")]
    public string UigfVersion { get; set; } = "v3.0";

    [JsonPropertyName("srgf_version")]
    public string SrgfVersion { get; set; } = "v1.0";

    [JsonPropertyName("region_time_zone")]
    public int? RegionTimeZone { get; set; }

}
