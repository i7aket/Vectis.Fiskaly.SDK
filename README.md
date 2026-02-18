# Vectis.Fiskaly.SDK

`Vectis.Fiskaly.SDK` is a production-oriented .NET SDK for Fiskaly SIGN DE v2
(German fiscal compliance, KassenSichV / AO 146a).

It provides strongly typed clients and resilient HTTP pipelines for:
- TSS lifecycle (`ITssClient`)
- Client lifecycle (`IClientManagementClient`)
- Transactions (`ITransactionClient`)
- Exports (`IExportClient`)
- Management API organizations (`IOrganizationClient`)
- Authentication (`IFiskalyAuthenticationService`)

## Package

- Package ID: `Vectis.Fiskaly.SDK`
- Current channel: `1.0.0-rc.1`
- Target framework: `net10.0`
- Repository: `https://github.com/i7aket/Vectis.Fiskaly.SDK`

## Installation

```bash
dotnet add package Vectis.Fiskaly.SDK --prerelease
```

## Quick Start (ASP.NET Core)

### 1) Configure credentials

```json
{
  "Fiskaly": {
    "ApiKey": "test_your_api_key",
    "ApiSecret": "your_43_char_api_secret",
    "BaseUrl": "https://kassensichv-middleware.fiskaly.com/api/v2/",
    "ManagementBaseUrl": "https://dashboard.fiskaly.com/api/v0/"
  }
}
```

### 2) Register SDK in DI

```csharp
using Vectis.Fiskaly.SDK;

builder.Services.AddFiskaly(builder.Configuration);
```

### 3) Resolve and use typed clients

```csharp
using Vectis.Fiskaly.SDK.SignDE.Tss;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

ITssClient tssClient = app.Services.GetRequiredService<ITssClient>();
TssId tssId = TssId.New();

var created = await tssClient.CreateTssAsync(tssId, cancellationToken: ct);
Console.WriteLine($"Created TSS: {created.Id}");
```

## Real Transaction Flow (used in Vectis)

This is the same pattern used in the `Vectis` app signing strategy:
1. Start transaction (`ACTIVE`)
2. Build payload (`Receipt`)
3. Finish transaction (`FINISHED`)

```csharp
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

ITransactionClient transactionClient = serviceProvider.GetRequiredService<ITransactionClient>();

TssId tssId = TssId.From("d9ee9052-fd45-4846-af24-818d10353cdb");
ClientId clientId = ClientId.From("0d918f2a-3c47-4662-a665-8e565d61109b");
TxId txId = TxId.New();

MetadataCollection metadata = MetadataCollection.Empty
    .Add("order_id", "ORD-2026-000123")
    .Add("cashpoint", "POS-01");

StartTransactionRequest start = new()
{
    ClientId = clientId,
    Metadata = metadata
};

_ = await transactionClient.StartTransactionAsync(tssId, txId, start, ct);

Receipt receipt = new()
{
    ReceiptType = ReceiptType.Receipt,
    AmountsPerVatRate =
    [
        new VatRateAmount
        {
            VatRate = VatRate.Normal,
            Amount = MoneyAmount.Create(45.00m, CurrencyCode.EUR)
        }
    ],
    AmountsPerPaymentType =
    [
        new PaymentTypeAmount
        {
            PaymentType = PaymentType.Cash,
            Amount = MoneyAmount.Create(45.00m, CurrencyCode.EUR)
        }
    ]
};

FinishTransactionRequest finish = FinishTransactionRequest.CreateReceipt(clientId, receipt, metadata);
var finished = await transactionClient.FinishTransactionAsync(tssId, txId, finish, cancellationToken: ct);

Console.WriteLine($"Tx finished: {finished.Id}, qr={finished.QrCodeData}");
```

## TSS + Client Setup Example

```csharp
using Vectis.Fiskaly.SDK.SignDE.Clients;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

ITssClient tssClient = sp.GetRequiredService<ITssClient>();
IClientManagementClient clientClient = sp.GetRequiredService<IClientManagementClient>();

TssId tssId = TssId.New();
ClientId clientId = ClientId.New();

MetadataCollection tssMetadata = MetadataCollection.Empty.Add("cashpoint", "POS-01");

_ = await tssClient.CreateTssAsync(tssId, tssMetadata, ct);
_ = await tssClient.UpdateTssAsync(tssId, UpdateTssRequest.Initialize("Main terminal", tssMetadata), ct);

CreateClientRequest createClient = new()
{
    SerialNumber = ClientSerialNumber.From("POS-01"),
    Metadata = MetadataCollection.Empty.Add("location", "Berlin")
};

_ = await clientClient.CreateClientAsync(tssId, clientId, createClient, ct);
```

## Export Example (Trigger -> Poll -> Download)

```csharp
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

IExportClient exportClient = sp.GetRequiredService<IExportClient>();

TssId tssId = TssId.From("d9ee9052-fd45-4846-af24-818d10353cdb");
ExportId exportId = ExportId.New();

DsfinvkFullExportRequest request = new()
{
    StartDate = DateTimeOffset.UtcNow.AddDays(-1),
    EndDate = DateTimeOffset.UtcNow,
    MaximumNumberRecords = ExportLimit.From(10_000)
};

ExportJob job = await exportClient.TriggerFullExportAsync(tssId, exportId, request, ct);

while (job.State is ExportState.Pending or ExportState.Working)
{
    await Task.Delay(TimeSpan.FromSeconds(2), ct);
    job = await exportClient.GetExportAsync(tssId, exportId, ct);
}

if (job.State != ExportState.Completed)
{
    throw new InvalidOperationException($"Export failed. State={job.State}, Exception={job.ExceptionCode}");
}

var archive = await exportClient.DownloadExportAsync(tssId, exportId, cancellationToken: ct);
Console.WriteLine($"Downloaded export with {archive.Segments.Count} segment(s)");
```

## Authentication + Management API Example

```csharp
using Vectis.Fiskaly.SDK.Authentication;
using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Organizations;

IFiskalyAuthenticationService auth = sp.GetRequiredService<IFiskalyAuthenticationService>();
IOrganizationClient organizations = sp.GetRequiredService<IOrganizationClient>();

ApiKeyCredentials credentials = new(
    ApiKey.From("test_your_api_key"),
    ApiSecret.From("your_43_char_api_secret"));

var authResponse = await auth.AuthenticateAsync(credentials, ct);
var organizationId = authResponse.Claims?.OrganizationId
    ?? throw new InvalidOperationException("Organization ID is missing in auth claims.");

var organization = await organizations.GetOrganizationAsync(organizationId, ct);
Console.WriteLine($"Organization: {organization.Name}");
```

## Configuration Reference

Minimum required settings:
- `Fiskaly:ApiKey`
- `Fiskaly:ApiSecret`

Important validation rules:
- `BaseUrl` and `ManagementBaseUrl` must be absolute and end with `/`
- HTTPS is required by default
- HTTP is allowed only for localhost (or private LAN when `AllowHttpForPrivateNetworks=true`)
- `ApiSecret` must be exactly 43 alphanumeric characters

### Per-client default resilience profile

| Client | Timeout (s) | RetryCount | CircuitBreakerThreshold | CircuitBreakerDuration (s) |
|---|---:|---:|---:|---:|
| `AuthClient` | 10 | 2 | 0 | 10 |
| `AdminClient` | 10 | 1 | 5 | 30 |
| `TssClient` | 30 | 3 | 5 | 60 |
| `TransactionClient` | 45 | 5 | 10 | 90 |
| `ExportClient` | 120 | 2 | 3 | 240 |
| `ClientManagementClient` | 30 | 3 | 5 | 60 |
| `OrganizationClient` | 30 | 3 | 5 | 60 |

### Runtime override in code

```csharp
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.Configuration;

builder.Services.AddFiskaly(builder.Configuration, configure: cfg =>
{
    cfg.TransactionClient.UseHighResilience();
    cfg.AuthClient.DisableResilience();
});
```

## Error Handling Best Practices

Production usage in `Vectis` maps SDK exceptions to domain errors:
- `FiskalyApiException`: HTTP/API error with code/category/details
- `FiskalyTimeoutException`: request timeout
- `FiskalyException`: generic SDK-level failure

```csharp
using Vectis.Fiskaly.SDK.Exceptions;

try
{
    await transactionClient.StartTransactionAsync(tssId, txId, start, ct);
}
catch (FiskalyApiException ex) when (ex.IsRetryable)
{
    logger.LogWarning(
        "Retryable API error. Status={Status}, Code={Code}, Correlation={CorrelationId}",
        ex.StatusCode,
        ex.ErrorCode,
        ex.CorrelationId);
    throw;
}
catch (FiskalyApiException ex)
{
    logger.LogError(
        "API error. Status={Status}, Code={Code}, Category={Category}, Hint={Hint}",
        ex.StatusCode,
        ex.ErrorCode,
        ex.Category,
        ex.GetRecoveryHint());
    throw;
}
catch (FiskalyTimeoutException ex)
{
    logger.LogWarning(ex, "Fiskaly timeout");
    throw;
}
```

## Metadata Rules

`MetadataCollection` is immutable and validated:
- max 40 entries
- max key length: 40
- max value length: 500

```csharp
MetadataCollection metadata = MetadataCollection.Empty
    .Add("order_id", "ORD-123")
    .Add("cashier", "alice");
```

For merge-style metadata updates, send only changed keys.
In Fiskaly APIs that support remove-on-empty semantics, deleted keys can be sent with empty string.

## Integration Tests

`Vectis.Fiskaly.IntegrationTests` loads configuration in this order:
1. `appsettings.test.json` (tracked template)
2. `appsettings.test.local.json` (local override, gitignored)

Create local override:

```bash
cp Vectis.Fiskaly.IntegrationTests/appsettings.test.local.example.json \
   Vectis.Fiskaly.IntegrationTests/appsettings.test.local.json
```

Put real credentials only into `appsettings.test.local.json`.

## Troubleshooting

### Retryable API errors (5xx / transient)

The SDK marks transient API failures as retryable and applies resilient retry policy.
If it still fails after retries:
- wait and retry
- verify there is no conflicting process on the same fiscal resources
- review your timeout/retry configuration for the affected client

### `401 Unauthorized`

- verify `ApiKey` and `ApiSecret`
- check environment mismatch (test vs production credentials/base URL)

### Configuration validation errors at startup

- ensure URL values are absolute and end with `/`
- ensure `ApiSecret` length/format is valid

### Missing `admin_puk`

`admin_puk` is returned only once when creating a TSS. Persist it immediately if your flow needs it.

## Security Notes

- Never commit real credentials.
- Use user-secrets, environment variables, CI secret stores, or vaults.
- Prefer least-privilege runtime identities.
- Be careful with verbose logs in development (`sensitive data logging` can expose payloads).

## Maintainer

Maintained by **Anatoliy Yermakov** for the Vectis open-source community.

## Official References

- Fiskaly docs: https://developer.fiskaly.com
- SIGN DE API: https://developer.fiskaly.com/api/sign-de
