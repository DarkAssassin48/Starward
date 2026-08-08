using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionBuff
{

    private string? _simpleDesc;

    private string? _mechanismName;


    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string Name { get; set; }

    [JsonPropertyName("desc_mi18n")]
    public string Desc { get; set; }

    [JsonPropertyName("simple_desc_mi18m")]
    public string? SimpleDesc
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_simpleDesc))
            {
                return _simpleDesc.Trim();
            }

            return GetExtensionString("simple_desc_mi18m")
                ?? GetExtensionString("simple_desc_mi18n");
        }
        set => _simpleDesc = value;
    }

    /// <summary>
    /// Localized title of the Pure Fiction Grit mechanic.
    /// Added by Starward before the complete record is serialized to SQLite.
    /// </summary>
    [JsonPropertyName("mechanism_buff")]
    public string? MechanismName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_mechanismName))
            {
                return _mechanismName.Trim();
            }

            return GetExtensionString("mechanism_buff");
        }
        set => _mechanismName = value;
    }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    private string? GetExtensionString(string key)
    {
        if (ExtensionData?.TryGetValue(key, out object? value) is not true || value is null)
        {
            return null;
        }

        string? result = value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };

        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

}
