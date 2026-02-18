using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Models;

public class DsfinvkFullExportRequestTests
{
    // ============================================================================
    // Query Parameter Generation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithDateRange_GeneratesCorrectParameters()
    {
        DateTimeOffset startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset endDate = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);

        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartDate = startDate,
            EndDate = endDate
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "start_date" && p.Value == startDate.ToUnixTimeSeconds().ToString());
        Assert.Contains(parameters, p => p.Key == "end_date" && p.Value == endDate.ToUnixTimeSeconds().ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithClientId_IncludesClientIdParameter()
    {
        ClientId clientId = ClientId.From("87654321-4321-4321-9321-210987654321");

        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            ClientId = clientId
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "client_id" && p.Value == clientId.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithSignatureCounters_GeneratesCorrectParameters()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartSignatureCounter = SignatureCounter.From(100),
            EndSignatureCounter = SignatureCounter.From(500)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "start_signature_counter" && p.Value == "100");
        Assert.Contains(parameters, p => p.Key == "end_signature_counter" && p.Value == "500");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithTransactionNumbers_GeneratesCorrectParameters()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartTransactionNumber = TransactionSequenceNumber.From(50),
            EndTransactionNumber = TransactionSequenceNumber.From(150)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "start_transaction_number" && p.Value == "50");
        Assert.Contains(parameters, p => p.Key == "end_transaction_number" && p.Value == "150");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithMaximumRecords_IncludesMaximumParameter()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            MaximumNumberRecords = ExportLimit.From(10000)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "maximum_number_records" && p.Value == "10000");
    }

    // ============================================================================
    // Validation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithInvalidDateRange_ThrowsInvalidOperationException()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartDate = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        Assert.Throws<InvalidOperationException>(() => request.ToQueryParameters());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithInvalidSignatureCounters_ThrowsInvalidOperationException()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartSignatureCounter = SignatureCounter.From(500),
            EndSignatureCounter = SignatureCounter.From(100)
        };

        Assert.Throws<InvalidOperationException>(() => request.ToQueryParameters());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithInvalidTransactionNumbers_ThrowsInvalidOperationException()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartTransactionNumber = TransactionSequenceNumber.From(150),
            EndTransactionNumber = TransactionSequenceNumber.From(50)
        };

        Assert.Throws<InvalidOperationException>(() => request.ToQueryParameters());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithNoFilters_ReturnsEmptyCollection()
    {
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest();

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Empty(parameters);
    }
}
