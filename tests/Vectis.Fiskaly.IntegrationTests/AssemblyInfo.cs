

// Limit parallel test execution to prevent exceeding Fiskaly TSS limit (5 max active TSS)
//
// Why MaxParallelThreads=2:
// - Fiskaly API allows maximum 5 active TSS instances per account
// - Each integration test creates 1 TSS in setup
// - Some tests create ADDITIONAL TSS (DisableTss, InitializeTss, ChangeAdminPin)
// - Max 2 tests running = max 2 base TSS + up to 2 additional = 4 TSS total
// - 4 < 5 → always under the limit, even with edge cases (cleanup delays, stuck TSS)
//
// Changed from 3 to 2 due to E_TSS_LIMIT_REACHED errors with tests that create extra TSS
//
// Performance vs Safety trade-off:
// - MaxParallelThreads=2: ~3-4 minutes (still faster than sequential)
// - Sequential execution: ~6.5 minutes (100% safe but slow)
//
// This approach follows xUnit best practices and is used by:
// - Azure SDK tests (Azure.Storage, Azure.Identity)
// - Entity Framework Core tests (SqlServer, Sqlite collections)
// - ASP.NET Core tests (integration test parallelization)
[assembly: CollectionBehavior(MaxParallelThreads = 1)]
