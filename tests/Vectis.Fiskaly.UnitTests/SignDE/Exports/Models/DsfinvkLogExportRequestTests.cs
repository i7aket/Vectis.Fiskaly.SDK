using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Models;

public class DsfinvkLogExportRequestTests
{
    // ============================================================================
    // Query Parameter Generation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithTransactionNumber_GeneratesCorrectParameter()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            TransactionNumber = TransactionSequenceNumber.From(42)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "transaction_number" && p.Value == "42");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithSignatureCounters_GeneratesCorrectParameters()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            StartSignatureCounter = SignatureCounter.From(10),
            EndSignatureCounter = SignatureCounter.From(20)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "start_signature_counter" && p.Value == "10");
        Assert.Contains(parameters, p => p.Key == "end_signature_counter" && p.Value == "20");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithMaximumRecords_IncludesMaximumParameter()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            MaximumNumberRecords = ExportLimit.From(5000)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Contains(parameters, p => p.Key == "maximum_number_records" && p.Value == "5000");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithAllFilters_GeneratesAllParameters()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            TransactionNumber = TransactionSequenceNumber.From(100),
            StartSignatureCounter = SignatureCounter.From(50),
            EndSignatureCounter = SignatureCounter.From(150),
            MaximumNumberRecords = ExportLimit.From(1000)
        };

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Equal(4, parameters.Count());
        Assert.Contains(parameters, p => p.Key == "transaction_number");
        Assert.Contains(parameters, p => p.Key == "start_signature_counter");
        Assert.Contains(parameters, p => p.Key == "end_signature_counter");
        Assert.Contains(parameters, p => p.Key == "maximum_number_records");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithNoFilters_ReturnsEmptyCollection()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest();

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Empty(parameters);
    }

    // ============================================================================
    // Validation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithInvalidSignatureCounters_ThrowsInvalidOperationException()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            StartSignatureCounter = SignatureCounter.From(100),
            EndSignatureCounter = SignatureCounter.From(50)
        };

        Assert.Throws<InvalidOperationException>(() => request.ToQueryParameters());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithInvalidTransactionNumbers_ThrowsInvalidOperationException()
    {
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            StartTransactionNumber = TransactionSequenceNumber.From(200),
            EndTransactionNumber = TransactionSequenceNumber.From(100)
        };

        Assert.Throws<InvalidOperationException>(() => request.ToQueryParameters());
    }
}
