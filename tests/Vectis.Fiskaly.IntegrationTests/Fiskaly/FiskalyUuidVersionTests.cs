using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Tests verifying that Fiskaly SDK identifiers comply with UUIDv4 requirements.
/// </summary>
/// <remarks>
/// <para><strong>Context:</strong></para>
/// <para>Fiskaly SIGN DE API v2.1.34 explicitly requires UUIDv4 format for 17 of 33 endpoints.</para>
/// <para>Future API versions will strictly enforce this requirement (per official docs).</para>
///
/// <para><strong>Test Strategy:</strong></para>
/// <list type="bullet">
///   <item>Verify .New() methods generate valid UUIDv4 (version nibble = '4')</item>
///   <item>Verify round-trip JSON serialization preserves UUIDv4 format</item>
///   <item>Verify UUIDv7 format is rejected during deserialization</item>
/// </list>
///
/// <para><strong>Related:</strong></para>
/// <para>See official Fiskaly SIGN DE API documentation for UUID requirements.</para>
/// </remarks>
public partial class FiskalyUuidVersionTests
{
    // UUIDv4 regex pattern: version nibble (position 14) must be '4', variant must be 8/9/a/b
    [GeneratedRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.IgnoreCase)]
    private static partial Regex UuidV4Regex();

    // Sample UUIDv7 for negative testing (version nibble = '7')
    private const string UuidV7Sample = "0199ed65-f350-74fa-baf3-387daac11a81";

    #region TssId Tests

    [Trait("Category", "Integration")]
    [Fact]
    public void TssId_New_ShouldGenerateValidUuidV4()
    {
        // Act
        TssId tssId = TssId.New();
        string uuidString = tssId.ToString();

        // Assert
        uuidString.Should().MatchRegex(UuidV4Regex(), "generated TSS ID must comply with UUIDv4 format");
        uuidString[14].Should().Be('4', "version nibble at position 14 must be '4' for UUIDv4");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void TssId_JsonRoundTrip_ShouldPreserveUuidV4Format()
    {
        // Arrange
        TssId original = TssId.New();

        // Act
        string json = JsonSerializer.Serialize(original);
        TssId deserialized = JsonSerializer.Deserialize<TssId>(json);

        // Assert
        deserialized.Should().Be(original, "deserialized value should match original");
        deserialized.ToString().Should().MatchRegex(UuidV4Regex(), "round-trip must preserve UUIDv4 format");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void TssId_From_ShouldRejectUuidV7Format()
    {
        // Act
        Func<TssId> act = () => TssId.From(UuidV7Sample);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid UUIDv4 format*", "UUIDv7 should be rejected");
    }

    #endregion

    #region ClientId Tests

    [Trait("Category", "Integration")]
    [Fact]
    public void ClientId_New_ShouldGenerateValidUuidV4()
    {
        // Act
        ClientId clientId = ClientId.New();
        string uuidString = clientId.ToString();

        // Assert
        uuidString.Should().MatchRegex(UuidV4Regex(), "generated Client ID must comply with UUIDv4 format");
        uuidString[14].Should().Be('4', "version nibble at position 14 must be '4' for UUIDv4");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ClientId_JsonRoundTrip_ShouldPreserveUuidV4Format()
    {
        // Arrange
        ClientId original = ClientId.New();

        // Act
        string json = JsonSerializer.Serialize(original);
        ClientId deserialized = JsonSerializer.Deserialize<ClientId>(json);

        // Assert
        deserialized.Should().Be(original, "deserialized value should match original");
        deserialized.ToString().Should().MatchRegex(UuidV4Regex(), "round-trip must preserve UUIDv4 format");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ClientId_From_ShouldRejectUuidV7Format()
    {
        // Act
        Func<ClientId> act = () => ClientId.From(UuidV7Sample);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid UUIDv4 format*", "UUIDv7 should be rejected");
    }

    #endregion

    #region TxId Tests

    [Trait("Category", "Integration")]
    [Fact]
    public void TransactionId_New_ShouldGenerateValidUuidV4()
    {
        // Act
        TxId txId = TxId.New();
        string uuidString = txId.ToString();

        // Assert
        uuidString.Should().MatchRegex(UuidV4Regex(), "generated Transaction ID must comply with UUIDv4 format");
        uuidString[14].Should().Be('4', "version nibble at position 14 must be '4' for UUIDv4");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void TransactionId_JsonRoundTrip_ShouldPreserveUuidV4Format()
    {
        // Arrange
        TxId original = TxId.New();

        // Act
        string json = JsonSerializer.Serialize(original);
        TxId deserialized = JsonSerializer.Deserialize<TxId>(json);

        // Assert
        deserialized.Should().Be(original, "deserialized value should match original");
        deserialized.ToString().Should().MatchRegex(UuidV4Regex(), "round-trip must preserve UUIDv4 format");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void TransactionId_From_ShouldRejectUuidV7Format()
    {
        // Act
        Func<TxId> act = () => TxId.From(UuidV7Sample);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid UUIDv4 format*", "UUIDv7 should be rejected");
    }

    #endregion

    #region ExportId Tests

    [Trait("Category", "Integration")]
    [Fact]
    public void ExportId_New_ShouldGenerateValidUuidV4()
    {
        // Act
        ExportId exportId = ExportId.New();
        string uuidString = exportId.ToString();

        // Assert
        uuidString.Should().MatchRegex(UuidV4Regex(), "generated Export ID must comply with UUIDv4 format");
        uuidString[14].Should().Be('4', "version nibble at position 14 must be '4' for UUIDv4");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ExportId_JsonRoundTrip_ShouldPreserveUuidV4Format()
    {
        // Arrange
        ExportId original = ExportId.New();

        // Act
        string json = JsonSerializer.Serialize(original);
        ExportId deserialized = JsonSerializer.Deserialize<ExportId>(json);

        // Assert
        deserialized.Should().Be(original, "deserialized value should match original");
        deserialized.ToString().Should().MatchRegex(UuidV4Regex(), "round-trip must preserve UUIDv4 format");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ExportId_From_ShouldRejectUuidV7Format()
    {
        // Act
        Func<ExportId> act = () => ExportId.From(UuidV7Sample);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid UUIDv4 format*", "UUIDv7 should be rejected");
    }

    #endregion

    #region Batch Verification Tests

    [Trait("Category", "Integration")]
    [Theory]
    [InlineData(100)]
    public void AllIdentifiers_New_ShouldConsistentlyGenerateUuidV4(int iterations)
    {
        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            string tssId = TssId.New().ToString();
            string clientId = ClientId.New().ToString();
            string txId = TxId.New().ToString();
            string exportId = ExportId.New().ToString();

            tssId.Should().MatchRegex(UuidV4Regex(), $"TssId iteration {i} should be UUIDv4");
            clientId.Should().MatchRegex(UuidV4Regex(), $"ClientId iteration {i} should be UUIDv4");
            txId.Should().MatchRegex(UuidV4Regex(), $"TxId iteration {i} should be UUIDv4");
            exportId.Should().MatchRegex(UuidV4Regex(), $"ExportId iteration {i} should be UUIDv4");
        }
    }

    #endregion
}
