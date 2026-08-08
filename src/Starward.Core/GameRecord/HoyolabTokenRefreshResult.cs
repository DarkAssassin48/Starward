using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord;


public class HoyolabTokenRefreshResult
{
    [JsonPropertyName("tokens")]
    public List<HoyolabToken> Tokens { get; set; } = [];


    public string? GetToken(int tokenType)
    {
        return Tokens.FirstOrDefault(x => x.TokenType == tokenType)?.Token;
    }
}


public class HoyolabToken
{
    [JsonPropertyName("token_type")]
    public int TokenType { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";
}
