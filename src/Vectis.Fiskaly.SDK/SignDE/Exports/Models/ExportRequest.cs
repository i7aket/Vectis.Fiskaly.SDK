using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Models;

public abstract class ExportRequestBase : IQueryParameterProvider
{
    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        List<KeyValuePair<string, string?>> parameters = new List<KeyValuePair<string, string?>>();
        Apply(parameters);
        return parameters;
    }

    protected abstract void Apply(List<KeyValuePair<string, string?>> parameters);

    protected static void AddParameter(List<KeyValuePair<string, string?>> parameters, string name, string? value)
    {
        if (value is null)
        {
            return;
        }

        parameters.Add(new KeyValuePair<string, string?>(name, value));
    }
}

public abstract class DsfinvkExportRequestBase : ExportRequestBase
{
    public ExportLimit? MaximumNumberRecords { get; init; }

    public SignatureCounter? StartSignatureCounter { get; init; }

    public SignatureCounter? EndSignatureCounter { get; init; }

    public TransactionSequenceNumber? StartTransactionNumber { get; init; }

    public TransactionSequenceNumber? EndTransactionNumber { get; init; }

    protected void ApplyCommonFilters(List<KeyValuePair<string, string?>> parameters)
    {
        if (StartSignatureCounter.HasValue && EndSignatureCounter.HasValue &&
            EndSignatureCounter.Value.Value < StartSignatureCounter.Value.Value)
        {
            throw new InvalidOperationException("End signature counter cannot be less than start signature counter.");
        }

        if (StartTransactionNumber.HasValue && EndTransactionNumber.HasValue &&
            EndTransactionNumber.Value.Value < StartTransactionNumber.Value.Value)
        {
            throw new InvalidOperationException("End transaction number cannot be less than start transaction number.");
        }

        if (MaximumNumberRecords.HasValue)
        {
            AddParameter(parameters, "maximum_number_records", MaximumNumberRecords.Value.Value.ToString());
        }

        if (StartSignatureCounter.HasValue)
        {
            AddParameter(parameters, "start_signature_counter", StartSignatureCounter.Value.Value.ToString());
        }

        if (EndSignatureCounter.HasValue)
        {
            AddParameter(parameters, "end_signature_counter", EndSignatureCounter.Value.Value.ToString());
        }

        if (StartTransactionNumber.HasValue)
        {
            AddParameter(parameters, "start_transaction_number", StartTransactionNumber.Value.Value.ToString());
        }

        if (EndTransactionNumber.HasValue)
        {
            AddParameter(parameters, "end_transaction_number", EndTransactionNumber.Value.Value.ToString());
        }
    }
}

public class DsfinvkFullExportRequest : DsfinvkExportRequestBase
{
    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public ClientId? ClientId { get; init; }

    protected override void Apply(List<KeyValuePair<string, string?>> parameters)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
        {
            throw new InvalidOperationException("End date cannot be earlier than start date.");
        }

        if (StartDate.HasValue)
        {
            AddParameter(parameters, "start_date", StartDate.Value.ToUnixTimeSeconds().ToString());
        }

        if (EndDate.HasValue)
        {
            AddParameter(parameters, "end_date", EndDate.Value.ToUnixTimeSeconds().ToString());
        }

        if (ClientId.HasValue)
        {
            AddParameter(parameters, "client_id", ClientId.Value.ToString());
        }

        ApplyCommonFilters(parameters);
    }
}

public sealed class DsfinvkClientExportRequest : ExportRequestBase
{
    public DsfinvkClientExportRequest(ClientId clientId)
    {
        ClientId = clientId;
    }

    public ClientId ClientId { get; }

    protected override void Apply(List<KeyValuePair<string, string?>> parameters)
    {
        AddParameter(parameters, "client_id", ClientId.ToString());
    }
}

public sealed class DsfinvkLogExportRequest : DsfinvkExportRequestBase
{
    public TransactionSequenceNumber? TransactionNumber { get; init; }

    protected override void Apply(List<KeyValuePair<string, string?>> parameters)
    {
        if (TransactionNumber.HasValue)
        {
            AddParameter(parameters, "transaction_number", TransactionNumber.Value.Value.ToString());
        }

        ApplyCommonFilters(parameters);
    }
}
