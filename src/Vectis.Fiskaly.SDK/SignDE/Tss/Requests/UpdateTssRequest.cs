using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.Requests;

public sealed partial record UpdateTssRequest
{
    private const int MaxDescriptionLength = 100;
    [JsonPropertyName("state")]
    public TssState State { get; init; }
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataCollection? Metadata { get; init; }

    [SetsRequiredMembers]
    private UpdateTssRequest(TssState state, string? description, MetadataCollection? metadata)
    {
        State = state;
        Description = description;
        Metadata = metadata;
    }

    public static UpdateTssRequest Initialize(string? description = null, MetadataCollection? metadata = null)
    {
        string? normalizedDescription = NormalizeDescription(description);
        ValidateDescription(normalizedDescription, nameof(description));

        return new UpdateTssRequest(TssState.Initialized, normalizedDescription, metadata);
    }

    public static UpdateTssRequest Uninitialize(MetadataCollection? metadata = null)
    {
        return new UpdateTssRequest(TssState.Uninitialized, null, metadata);
    }

    public static UpdateTssRequest Disable(MetadataCollection? metadata = null)
    {
        return new UpdateTssRequest(TssState.Disabled, null, metadata);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        return description.Length == 0 ? null : description;
    }

    private static void ValidateDescription(string? description, string argumentName)
    {
        if (description is null)
        {
            return;
        }

        if (description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description must not exceed {MaxDescriptionLength} characters (OpenAPI constraint). " +
                $"Got: {description.Length} characters.",
                argumentName);
        }

        if (!DescriptionPattern().IsMatch(description))
        {
            throw new ArgumentException(
                $"Description contains invalid characters. Allowed: A-Z, a-z, 0-9, space, and special characters '()+,-./:=?. " +
                $"Got: \"{description}\"",
                argumentName);
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9 '()+,-./:=?]{0,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex DescriptionPattern();
}
