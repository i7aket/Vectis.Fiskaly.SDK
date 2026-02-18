using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Extensions;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Extensions;

public class ExportJobExtensionsTests
{
    // ============================================================================
    // IsCompleted() Tests
    // ============================================================================

    // Test 1: IsCompleted returns true for COMPLETED state
    [Trait("Category", "Unit")]
    [Fact]
    public void IsCompleted_WhenStateCompleted_ReturnsTrue()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Completed,
            Type = ResourceType.Export
        };

        // Act
        bool result = export.IsCompleted();

        // Assert
        Assert.True(result);
    }

    // ============================================================================
    // IsFailed() Tests
    // ============================================================================

    // Test 2: IsFailed returns true for ERROR state
    [Trait("Category", "Unit")]
    [Fact]
    public void IsFailed_WhenStateError_ReturnsTrue()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Error,
            ExceptionCode = ExportExceptionCode.Internal,
            Type = ResourceType.Export
        };

        // Act
        bool result = export.IsFailed();

        // Assert
        Assert.True(result);
    }

    // ============================================================================
    // IsPending() Tests
    // ============================================================================

    // Test 3: IsPending returns true for PENDING state
    [Trait("Category", "Unit")]
    [Fact]
    public void IsPending_WhenStatePending_ReturnsTrue()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Pending,
            Type = ResourceType.Export
        };

        // Act
        bool result = export.IsPending();

        // Assert
        Assert.True(result);
    }

    // Test 4: IsPending returns true for WORKING state
    [Trait("Category", "Unit")]
    [Fact]
    public void IsPending_WhenStateWorking_ReturnsTrue()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Working,
            Type = ResourceType.Export
        };

        // Act
        bool result = export.IsPending();

        // Assert
        Assert.True(result);
    }

    // ============================================================================
    // IsCancelled() Tests
    // ============================================================================

    // Test 5: IsCancelled returns true for CANCELLED state
    [Trait("Category", "Unit")]
    [Fact]
    public void IsCancelled_WhenStateCancelled_ReturnsTrue()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Cancelled,
            Type = ResourceType.Export
        };

        // Act
        bool result = export.IsCancelled();

        // Assert
        Assert.True(result);
    }

    // ============================================================================
    // ThrowIfFailed() Tests
    // ============================================================================

    // Test 6: ThrowIfFailed does not throw when state is COMPLETED
    [Trait("Category", "Unit")]
    [Fact]
    public void ThrowIfFailed_WhenStateCompleted_DoesNotThrow()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Completed,
            Type = ResourceType.Export
        };

        // Act & Assert
        Exception? exception = Record.Exception(() => export.ThrowIfFailed());
        Assert.Null(exception);
    }

    // Test 7: ThrowIfFailed throws InvalidOperationException when state is ERROR
    [Trait("Category", "Unit")]
    [Fact]
    public void ThrowIfFailed_WhenStateError_ThrowsInvalidOperationException()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Error,
            ExceptionCode = ExportExceptionCode.Internal,
            Type = ResourceType.Export
        };

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => export.ThrowIfFailed());
        Assert.Contains("550e8400-e29b-41d4-a716-446655440000", exception.Message);
        Assert.Contains("Internal", exception.Message);
    }

    // ============================================================================
    // GetExceptionMetadata() Tests
    // ============================================================================

    // Test 8: GetExceptionMetadata returns metadata when state is ERROR
    [Trait("Category", "Unit")]
    [Fact]
    public void GetExceptionMetadata_WhenStateError_ReturnsMetadata()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Error,
            ExceptionCode = ExportExceptionCode.TooManyRecords,
            Type = ResourceType.Export
        };

        // Act
        ExportExceptionInfo? metadata = export.GetExceptionMetadata();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(FiskalyErrorCategory.Permanent, metadata.Category);
        Assert.False(metadata.IsRetryable);
    }

    // Test 9: GetExceptionMetadata returns null when state is COMPLETED
    [Trait("Category", "Unit")]
    [Fact]
    public void GetExceptionMetadata_WhenStateCompleted_ReturnsNull()
    {
        // Arrange
        ExportJob export = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            State = ExportState.Completed,
            Type = ResourceType.Export
        };

        // Act
        ExportExceptionInfo? metadata = export.GetExceptionMetadata();

        // Assert
        Assert.Null(metadata);
    }
}
