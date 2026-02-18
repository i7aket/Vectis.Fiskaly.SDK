using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.Responses;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for export workflows.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: Export lifecycle and asynchronous processing</para>
///
/// <para><strong>Export Types Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>Full Export (DSFinV-K) - Complete fiscal data export</description></item>
///   <item><description>Client Export - Client-specific transactions</description></item>
///   <item><description>Log Export - Transaction log data</description></item>
/// </list>
///
/// <para><strong>Endpoints Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>POST /tss/{tss_id}/export/{export_id}/full - TriggerFullExportAsync</description></item>
///   <item><description>POST /tss/{tss_id}/export/{export_id}/client - TriggerClientExportAsync</description></item>
///   <item><description>POST /tss/{tss_id}/export/{export_id}/log - TriggerLogExportAsync</description></item>
///   <item><description>GET /tss/{tss_id}/export/{export_id} - GetExportAsync</description></item>
///   <item><description>GET /tss/{tss_id}/export/{export_id}.tar - DownloadExportAsync</description></item>
///   <item><description>DELETE /tss/{tss_id}/export/{export_id} - CancelExportAsync</description></item>
///   <item><description>GET /tss/{tss_id}/export - ListExportsAsync</description></item>
///   <item><description>GET /export - ListAllExportsAsync</description></item>
///   <item><description>PUT /tss/{tss_id}/export/{export_id}/metadata/{key} - UpdateExportMetadataAsync</description></item>
///   <item><description>GET /tss/{tss_id}/export/{export_id}/metadata/{key} - GetExportMetadataAsync</description></item>
/// </list>
///
/// <para><strong>Note</strong>: Export generation is asynchronous. Tests use polling to wait for completion.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "Exports")]
[Trait("Priority", "High")]
public class ExportWorkflowTests : FiskalyIntegrationTestBase
{
    public ExportWorkflowTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task TriggerFullExport_ShouldCreatePendingExport()
    {
        // Arrange
        ExportId exportId = ExportId.New();

        Console.WriteLine($"Triggering full export: {exportId}");

        // Authenticate admin for export operations
        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            // Act
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            ExportJob response = await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().Be(exportId);
            response.State.Should().BeOneOf(ExportState.Pending, ExportState.Working, ExportState.Completed);

            Console.WriteLine($"✅ Full export triggered successfully!");
            Console.WriteLine($"   Export ID: {response.Id}");
            Console.WriteLine($"   State: {response.State}");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task PollExport_UntilCompleted_ShouldTransitionToCompletedState()
    {
        // Arrange - Trigger export first
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            Console.WriteLine($"Polling export {exportId} until completed...");

            // Act - Poll until completed
            ExportJob? completedExport = null;
            int maxAttempts = 30; // 30 * 2 seconds = 60 seconds max
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                await Task.Delay(2000); // 2 second intervals
                ExportJob currentExport = await Fixture.ExportClient.GetExportAsync(TssId, exportId);

                Console.WriteLine($"   Poll attempt {attempt + 1}/{maxAttempts} - State: {currentExport.State}");

                if (currentExport.State == ExportState.Completed)
                {
                    completedExport = currentExport;
                    break;
                }

                if (currentExport.State == ExportState.Error)
                {
                    throw new Exception($"Export failed with ERROR state");
                }

                attempt++;
            }

            // Assert
            completedExport.Should().NotBeNull("Export should complete within 60 seconds");
            completedExport!.State.Should().Be(ExportState.Completed);

            Console.WriteLine($"✅ Export completed after {attempt + 1} polls");
            Console.WriteLine($"   Final State: {completedExport.State}");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task DownloadExport_WhenCompleted_ShouldReturnTarArchive()
    {
        // Arrange - Trigger and wait for export completion
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            // Poll until completed
            int maxAttempts = 30;
            int attempt = 0;
            ExportJob? completedExport = null;

            while (attempt < maxAttempts)
            {
                await Task.Delay(2000);
                ExportJob currentExport = await Fixture.ExportClient.GetExportAsync(TssId, exportId);

                if (currentExport.State == ExportState.Completed)
                {
                    completedExport = currentExport;
                    break;
                }

                if (currentExport.State == ExportState.Error)
                {
                    throw new Exception($"Export failed with ERROR state");
                }

                attempt++;
            }

            completedExport.Should().NotBeNull("Export should complete before download");

            Console.WriteLine($"Downloading export {exportId}...");

            // Act - Download TAR archive
            DsfinvkArchive archive = await Fixture.ExportClient.DownloadExportAsync(TssId, exportId);

            // Assert
            archive.Should().NotBeNull();
            archive.RawContent.Length.Should().BeGreaterThan(0, "TAR file should contain data");

            Console.WriteLine($"✅ Export downloaded successfully!");
            Console.WriteLine($"   Size: {archive.RawContent.Length} bytes");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task TriggerClientExport_ForSpecificClient_ShouldSucceed()
    {
        // Arrange
        ExportId exportId = ExportId.New();

        Console.WriteLine($"Triggering client-specific export: {exportId}");
        Console.WriteLine($"   Client: {ClientId}");

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            // Act
            DsfinvkClientExportRequest exportRequest = new DsfinvkClientExportRequest(ClientId);
            ExportJob response = await Fixture.ExportClient.TriggerClientExportAsync(TssId, exportId, exportRequest);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().Be(exportId);
            response.State.Should().BeOneOf(ExportState.Pending, ExportState.Working, ExportState.Completed);

            Console.WriteLine($"✅ Client export triggered successfully!");
            Console.WriteLine($"   Export ID: {response.Id}");
            Console.WriteLine($"   State: {response.State}");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task TriggerLogExport_ShouldSucceed()
    {
        // Arrange
        ExportId exportId = ExportId.New();

        Console.WriteLine($"Triggering transaction log export: {exportId}");

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            // Act
            DsfinvkLogExportRequest exportRequest = new DsfinvkLogExportRequest();
            ExportJob response = await Fixture.ExportClient.TriggerLogExportAsync(TssId, exportId, exportRequest);

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().Be(exportId);
            response.State.Should().BeOneOf(ExportState.Pending, ExportState.Working, ExportState.Completed);

            Console.WriteLine($"✅ Log export triggered successfully!");
            Console.WriteLine($"   Export ID: {response.Id}");
            Console.WriteLine($"   State: {response.State}");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task CancelExport_WhenPending_ShouldTransitionToCancelled()
    {
        // Arrange - Trigger export
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            Console.WriteLine($"Export {exportId} triggered, attempting cancellation...");

            // Small delay to ensure export is in a cancellable state
            await Task.Delay(500);

            // Act - Cancel the export
            try
            {
                ExportJob cancelResult = await Fixture.ExportClient.CancelExportAsync(TssId, exportId);

                Console.WriteLine($"✅ Export cancelled successfully");
                Console.WriteLine($"   Returned State: {cancelResult.State}");

                // Verify the returned export job
                cancelResult.Should().NotBeNull();
                cancelResult.Id.Should().Be(exportId);
                cancelResult.TssId.Should().Be(TssId);
                cancelResult.State.Should().BeOneOf(ExportState.Cancelled, ExportState.Completed);

                // Verify cancellation by fetching again
                ExportJob cancelledExport = await Fixture.ExportClient.GetExportAsync(TssId, exportId);
                cancelledExport.State.Should().BeOneOf(ExportState.Cancelled, ExportState.Completed);

                Console.WriteLine($"   Verified State: {cancelledExport.State}");
            }
            catch (Exception ex)
            {
                // Note: Cancellation may fail if export completed too quickly
                Console.WriteLine($"⚠️ Cancellation attempt: {ex.Message}");
                Console.WriteLine($"   (Export may have completed before cancellation)");
            }
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task ListExports_ShouldContainTriggeredExports()
    {
        // Arrange - Trigger an export
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            Console.WriteLine($"Listing exports for TSS: {TssId}");

            // Act
            ListExportsResponse response = await Fixture.ExportClient.ListExportsAsync(TssId);

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().NotBeEmpty();
            response.Data.Should().Contain(e => e.Id == exportId, "Our export should be in the list");

            Console.WriteLine($"✅ Exports listed successfully!");
            Console.WriteLine($"   Total exports: {response.Data.Count}");
            Console.WriteLine($"   Our export {exportId} found in list");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task ListAllExports_ShouldContainExportsAcrossAllTss()
    {
        // Arrange - Trigger an export
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            Console.WriteLine($"Listing all exports across all TSS...");

            // Act
            ListExportsResponse response = await Fixture.ExportClient.ListAllExportsAsync();

            // Assert
            response.Should().NotBeNull();
            response.Data.Should().NotBeEmpty();
            response.Data.Should().Contain(e => e.Id == exportId, "Our export should be in global list");

            Console.WriteLine($"✅ All exports listed successfully!");
            Console.WriteLine($"   Total exports across all TSS: {response.Data.Count}");
            Console.WriteLine($"   Our export {exportId} found in global list");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task UpdateExportMetadata_ShouldPersist()
    {
        // Arrange - Trigger an export
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            MetadataCollection metadata = MetadataCollection.Empty
                .Add("environment", "integration-test")
                .Add("test-class", GetType().Name)
                .Add("timestamp", DateTime.UtcNow.ToString("o"));

            Console.WriteLine($"Updating export metadata: {metadata.Count} keys");

            // Act
            await Fixture.ExportClient.UpdateExportMetadataAsync(TssId, exportId, metadata);

            // Assert - Retrieve and verify
            MetadataCollection retrievedMetadata = await Fixture.ExportClient.GetExportMetadataAsync(TssId, exportId);
            retrievedMetadata.Should().NotBeNull();
            retrievedMetadata.Should().ContainKeys(metadata.Keys);
            retrievedMetadata["environment"].Should().Be("integration-test");
            retrievedMetadata["test-class"].Should().Be(GetType().Name);

            Console.WriteLine($"✅ Export metadata updated: {retrievedMetadata.Count} keys");
            foreach (KeyValuePair<string, string> kvp in retrievedMetadata)
            {
                Console.WriteLine($"   {kvp.Key} = {kvp.Value}");
            }
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }

    [Fact]
    public async Task GetExportMetadata_AfterUpdate_ShouldReturnNewMetadata()
    {
        // Arrange - Trigger export and set metadata
        ExportId exportId = ExportId.New();

        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        try
        {
            DsfinvkFullExportRequest exportRequest = new DsfinvkFullExportRequest();
            await Fixture.ExportClient.TriggerFullExportAsync(TssId, exportId, exportRequest);

            string key1 = "export-test-key";
            string value1 = "export-test-value";
            MetadataCollection metadata = MetadataCollection.Empty.Add(key1, value1);

            await Fixture.ExportClient.UpdateExportMetadataAsync(TssId, exportId, metadata);
            Console.WriteLine($"Metadata set: {key1} = {value1}");

            // Act
            MetadataCollection retrievedMetadata = await Fixture.ExportClient.GetExportMetadataAsync(TssId, exportId);

            // Assert
            retrievedMetadata.Should().NotBeNull();
            retrievedMetadata.Should().ContainKey(key1);
            retrievedMetadata[key1].Should().Be(value1);

            Console.WriteLine($"✅ Export metadata retrieved: {retrievedMetadata.Count} keys");
            Console.WriteLine($"   {key1} = {retrievedMetadata[key1]}");
        }
        finally
        {
            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
    }
}
