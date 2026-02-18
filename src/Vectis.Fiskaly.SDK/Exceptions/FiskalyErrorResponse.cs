#nullable enable

using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Exceptions;

public record FiskalyErrorResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    [JsonPropertyName("status_code")]
    public required int StatusCode { get; init; }
    [JsonPropertyName("error")]
    public required string Error { get; init; }
}
