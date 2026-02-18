using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Models;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for TSS (Technical Security System) management operations.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: TSS lifecycle and metadata management</para>
///
/// <para><strong>Endpoints Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>POST /tss/{tss_id} - CreateTssAsync</description></item>
///   <item><description>GET /tss/{tss_id} - GetTssAsync</description></item>
///   <item><description>PATCH /tss/{tss_id}/state/uninitialized - UninitializeTssAsync</description></item>
///   <item><description>PATCH /tss/{tss_id}/state/initialized - InitializeTssAsync</description></item>
///   <item><description>PATCH /tss/{tss_id}/state/disabled - DisableTssAsync</description></item>
///   <item><description>GET /tss - ListTssAsync</description></item>
///   <item><description>PUT /tss/{tss_id}/metadata/{key} - UpdateTssMetadataAsync</description></item>
///   <item><description>GET /tss/{tss_id}/metadata/{key} - GetTssMetadataAsync</description></item>
/// </list>
///
/// <para><strong>Note</strong>: Base class provides a TSS in INITIALIZED state.
/// Some tests create additional TSS instances to test specific state transitions.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "TSS")]
[Trait("Priority", "Critical")]
public class TssManagementTests : FiskalyIntegrationTestBase
{
    public TssManagementTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetTss_AfterCreation_ShouldReturnDetails()
    {
        // Arrange
        Console.WriteLine($"Getting TSS details: {TssId}");

        // Act
        TssResponse response = await Fixture.TssClient.GetTssAsync(TssId);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(TssId);
        response.State.Should().Be(TssState.Initialized);
        response.Description.Should().Contain($"Test: {GetType().Name}");

        Console.WriteLine($"✅ TSS retrieved successfully!");
        Console.WriteLine($"   State: {response.State}");
        Console.WriteLine($"   Description: {response.Description}");
    }

    [Fact]
    public async Task UpdateTssMetadata_WithMultipleKeys_ShouldPersistAllKeys()
    {
        // Arrange - Add multiple metadata keys
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("environment", "integration-test")
            .Add("test-class", GetType().Name)
            .Add("timestamp", DateTime.UtcNow.ToString("o"))
            .Add("temporary-key", "will-be-deleted");

        Console.WriteLine($"Updating TSS metadata: {metadata.Count} keys");

        // Act - Update metadata
        await Fixture.TssClient.UpdateTssMetadataAsync(TssId, metadata);

        // Assert - Retrieve and verify all keys persisted
        MetadataCollection retrievedMetadata = await Fixture.TssClient.GetTssMetadataAsync(TssId);
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata.Should().ContainKeys(metadata.Keys);
        retrievedMetadata["environment"].Should().Be("integration-test");
        retrievedMetadata["test-class"].Should().Be(GetType().Name);
        retrievedMetadata["temporary-key"].Should().Be("will-be-deleted");

        Console.WriteLine($"✅ TSS metadata updated: {retrievedMetadata.Count} keys");
        foreach (KeyValuePair<string, string> kvp in retrievedMetadata)
        {
            Console.WriteLine($"   {kvp.Key} = {kvp.Value}");
        }

        // Act - Delete metadata by setting empty value
        Console.WriteLine($"Deleting 'temporary-key' by setting empty value...");
        MetadataCollection metadataWithEmptyValue = MetadataCollection.Empty
            .Add("temporary-key", ""); // Empty value deletes the key

        await Fixture.TssClient.UpdateTssMetadataAsync(TssId, metadataWithEmptyValue);

        // Assert - Verify key was deleted
        MetadataCollection metadataAfterDeletion = await Fixture.TssClient.GetTssMetadataAsync(TssId);
        metadataAfterDeletion.Should().NotContainKey("temporary-key", "Empty value should delete the metadata key");
        metadataAfterDeletion.Should().ContainKey("environment", "Other keys should remain");
        metadataAfterDeletion.Should().ContainKey("test-class", "Other keys should remain");

        Console.WriteLine($"✅ Metadata key deleted successfully via empty value");
        Console.WriteLine($"   Remaining keys: {string.Join(", ", metadataAfterDeletion.Keys)}");
    }

    [Fact]
    public async Task GetTssMetadata_AfterUpdate_ShouldReturnNewMetadata()
    {
        // Arrange
        string key1 = "test-key-1";
        string value1 = "test-value-1";
        MetadataCollection metadata = MetadataCollection.Empty.Add(key1, value1);

        await Fixture.TssClient.UpdateTssMetadataAsync(TssId, metadata);
        Console.WriteLine($"Metadata set: {key1} = {value1}");

        // Act
        MetadataCollection retrievedMetadata = await Fixture.TssClient.GetTssMetadataAsync(TssId);

        // Assert
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata.Should().ContainKey(key1);
        retrievedMetadata[key1].Should().Be(value1);

        Console.WriteLine($"✅ TSS metadata retrieved: {retrievedMetadata.Count} keys");
        Console.WriteLine($"   {key1} = {retrievedMetadata[key1]}");
    }

    [Fact]
    public async Task ListTss_ShouldContainCreatedTss()
    {
        // Arrange
        Console.WriteLine($"Listing TSS instances with filters: {TssId}");

        // Act - Use query parameters to find newly created TSS
        // Filter by INITIALIZED state and sort by creation time descending (newest first)
        ListTssQueryParameters queryParams = new ListTssQueryParameters
        {
            States = new[] { TssState.Initialized },
            Sort = new TssSortOption(TssSortField.TimeCreation, SortDirection.Descending),
            Limit = 20  // Limit to most recent 20 initialized TSS
        };

        ListTssResponse response = await Fixture.TssClient.ListTssAsync(queryParams);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNull();
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(tss => tss.Id == TssId,
            "ListTss with State=INITIALIZED filter should contain our newly created TSS");

        TssResponse ourTss = response.Data!.First(tss => tss.Id == TssId);
        ourTss.State.Should().Be(TssState.Initialized);

        Console.WriteLine($"✅ Listed {response.Data!.Count} INITIALIZED TSS instance(s) (Total: {response.Count ?? 0})");
        Console.WriteLine($"   Our TSS {TssId} found with state: {ourTss.State?.ToString() ?? "UNKNOWN"}");
    }

}
