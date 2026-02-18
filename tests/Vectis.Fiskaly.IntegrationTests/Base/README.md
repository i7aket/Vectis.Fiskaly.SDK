# Fiskaly Integration Test Base Infrastructure

## Overview

Base classes for writing Fiskaly integration tests with automatic TSS and Client setup/cleanup.

This infrastructure follows the **Minimal State + API as Source of Truth** pattern, ensuring test isolation and automatic resource cleanup.

## Quick Start

```csharp
using Vectis.Fiskaly.IntegrationTests.Base;

public class MyTransactionTests : FiskalyIntegrationTestBase
{
    public MyTransactionTests(FiskalyClientFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task CreateTransaction_ShouldSucceed()
    {
        // TssId and ClientId already available!
        var tx = await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            TxId.New(),
            new StartTransactionRequest { ClientId = ClientId });

        tx.State.Should().Be(TxState.Active);
    }
}
```

## Local Test Credentials

Integration tests read configuration in this order:

1. `appsettings.test.json` (tracked template with empty credentials)
2. `appsettings.test.local.json` (optional local override, not committed)

Create local override:

```bash
cp Vectis.Fiskaly.IntegrationTests/appsettings.test.local.example.json \
   Vectis.Fiskaly.IntegrationTests/appsettings.test.local.json
```

Fill real Fiskaly credentials only in `appsettings.test.local.json`.

## Architecture

### Minimal State Pattern

**Only store what cannot be retrieved from API:**

- `TssId` - TSS identifier
- `ClientId` - Client identifier
- `AdminPuk` - returned **ONLY ONCE** on TSS creation (cannot be retrieved later)
- `AdminPin` - for admin authentication

**Everything else is retrieved from API when needed:**

- Active transactions → `ListTransactionsAsync()`
- Transaction details → `GetTransactionAsync()`
- Client details → `GetClientAsync()`
- TSS details → `GetTssAsync()`

### API as Source of Truth

Cleanup uses API to discover current state instead of tracking created entities:

```csharp
// ❌ OLD APPROACH (tracking)
private List<TxId> _createdTransactions = new();

public async Task CreateTransaction(TxId id)
{
    await StartTransactionAsync(id);
    _createdTransactions.Add(id);  // Manual tracking
}

public async Task Cleanup()
{
    foreach (var id in _createdTransactions)  // Tracked list
        await CancelTransactionAsync(id);
}

// ✅ NEW APPROACH (API-based)
public async Task Cleanup()
{
    var transactions = await ListTransactionsAsync();  // API query
    foreach (var tx in transactions.Where(t => t.State == Active))
        await CancelTransactionAsync(tx.Id);
}
```

**Benefits:**
- No manual tracking required
- Handles transactions created by any test
- Always reflects actual API state
- Simpler, less error-prone

## What Gets Created

Per test class (IClassFixture + IAsyncLifetime):

| Resource | State | When |
|----------|-------|------|
| TSS | INITIALIZED | InitializeAsync() |
| Client | REGISTERED | InitializeAsync() |
| Admin PIN | Set | InitializeAsync() |

**Setup sequence** (InitializeAsync):

1. Create TSS → CREATED state
2. Capture AdminPuk (⚠️ only returned once!)
3. Set Admin PIN via ChangeAdminPinAsync
4. Authenticate Admin
5. Initialize TSS → INITIALIZED state
6. Register Client → REGISTERED state
7. Logout Admin

## What Gets Cleaned Up

After all tests in class complete (DisposeAsync):

| Step | Operation | API Method |
|------|-----------|------------|
| 1 | Authenticate Admin | AuthenticateAdminAsync |
| 2 | Cancel active transactions | ListTransactionsAsync + CancelTransactionAsync |
| 3 | Deregister client | UpdateClientAsync |
| 4 | Disable TSS | DisableTssAsync (⚠️ permanent!) |
| 5 | Logout Admin | LogoutAdminAsync |

**Important:**
- DisableTssAsync is **PERMANENT** - TSS cannot be reactivated
- All cleanup operations wrapped in try-catch (suppress exceptions)
- Cleanup failures don't mask test results

## Protected Properties

Available in all derived test classes:

```csharp
protected FiskalyClientFixture Fixture { get; }  // API clients
protected TssId TssId { get; }                   // Auto-created TSS
protected ClientId ClientId { get; }             // Auto-created Client
protected string AdminPuk { get; }               // Admin PUK (one-time)
protected string AdminPin { get; }               // Admin PIN
```

## Example Tests

See `Examples/SimpleTssTransactionTests.cs` for complete examples:

- **StartTransaction** - Basic transaction creation
- **FinishTransaction** - Complete flow with receipt data
- **CreateMultipleTransactions** - Batch transaction processing
- **CancelTransaction** - Transaction cancellation

## Comparison: Old vs New Approach

### Old Approach (Fiskaly/ folder)

```csharp
// Uses shared resources from appsettings.test.json
private readonly string _sharedTssId = "shared-tss-from-config";
private readonly string _sharedClientId = "shared-client-from-config";

// Problem: Resources created with different API key
// Result: E_ACCESS_DENIED (403 Forbidden)
```

**Issues:**
- ❌ Shared state between tests
- ❌ Dependency on external configuration
- ❌ Tests fail with different API keys
- ❌ No isolation between test classes

### New Approach (Base/ folder)

```csharp
// Creates isolated resources per test class
public class MyTests : FiskalyIntegrationTestBase
{
    // Automatic setup - TssId and ClientId created fresh
    // Automatic cleanup - all resources cleaned up
}
```

**Benefits:**
- ✅ Full test isolation (each test class gets own TSS + Client)
- ✅ No external dependencies (creates own resources)
- ✅ Works with any API key
- ✅ Automatic cleanup

## xUnit Lifecycle

```
Test Class Constructor
        ↓
InitializeAsync() ← Creates TSS + Client (ONCE)
        ↓
Test Method 1
        ↓
Test Method 2
        ↓
Test Method N
        ↓
DisposeAsync() ← Cleanup all resources (ONCE)
```

**Key Points:**
- InitializeAsync runs **ONCE** before all tests
- DisposeAsync runs **ONCE** after all tests
- All tests share the same TSS and Client
- Each test should clean up its own transactions (or rely on DisposeAsync)

## Best Practices

### ✅ DO

```csharp
// Use base class properties
await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, ...);

// Let automatic cleanup handle resources
// (no manual cleanup needed in tests)

// Use ITestOutputHelper for logging
_output.WriteLine($"Transaction started: {txId}");
```

### ❌ DON'T

```csharp
// Don't create your own TSS/Client
var myTss = await Fixture.TssClient.CreateTssAsync(...);  // Use TssId instead

// Don't track entities manually
private List<TxId> _transactions = new();  // API-based cleanup handles this

// Don't skip base constructor
public MyTests() { }  // Missing: base(fixture)
```

## Troubleshooting

### "E_ACCESS_DENIED" errors

**Cause:** Using shared TSS from old tests (different API key)

**Solution:** Inherit from `FiskalyIntegrationTestBase` instead of using shared resources

### Tests run but TSS not cleaned up

**Cause:** DisposeAsync not called (test runner crash)

**Solution:** TSS cleanup is best-effort only; run manual cleanup script if needed

### Admin authentication fails

**Cause:** AdminPin mismatch

**Solution:** Check `AdminPin` property value (default: "base-test-pin-12345")

## Migration Guide

### From Old Tests to New Base Class

**Before:**
```csharp
public class MyTests : IClassFixture<FiskalyClientFixture>
{
    private readonly FiskalyClientFixture _fixture;

    public MyTests(FiskalyClientFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyTest()
    {
        // Manual TSS creation
        var tssId = TssId.New();
        var tss = await _fixture.TssClient.CreateTssAsync(tssId);
        // ... manual setup ...

        // Test code

        // ... manual cleanup ...
    }
}
```

**After:**
```csharp
public class MyTests : FiskalyIntegrationTestBase
{
    public MyTests(FiskalyClientFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task MyTest()
    {
        // TssId and ClientId ready to use!
        // Test code
        // Automatic cleanup!
    }
}
```

## API Reference

### FiskalyIntegrationTestBase

**Constructor:**
```csharp
protected FiskalyIntegrationTestBase(FiskalyClientFixture fixture)
```

**Properties:**
```csharp
protected FiskalyClientFixture Fixture { get; }
protected TssId TssId { get; }
protected ClientId ClientId { get; }
protected string AdminPuk { get; }
protected string AdminPin { get; } // = "base-test-pin-12345"
```

**Methods:**
```csharp
public async Task InitializeAsync()  // Auto-called by xUnit
public async Task DisposeAsync()     // Auto-called by xUnit
```

## Further Reading

- **Fiskaly API Documentation**: See SDK/Documentation/
- **xUnit IAsyncLifetime**: https://xunit.net/docs/shared-context#async-lifetime
- **xUnit IClassFixture**: https://xunit.net/docs/shared-context#class-fixture
