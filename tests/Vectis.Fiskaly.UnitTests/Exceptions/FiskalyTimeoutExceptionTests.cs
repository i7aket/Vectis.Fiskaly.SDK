using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Exceptions;

public class FiskalyTimeoutExceptionTests
{
    // ============================================================================
    // Constructor Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_NoParameters_CreatesException()
    {
        FiskalyTimeoutException exception = new FiskalyTimeoutException();

        Assert.NotNull(exception);
        Assert.IsType<FiskalyTimeoutException>(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        string message = "Request timeout";

        FiskalyTimeoutException exception = new FiskalyTimeoutException(message);

        Assert.Contains(message, exception.Message);
        Assert.StartsWith("Fiskaly request timed out:", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        string message = "Timeout occurred";
        TimeoutException inner = new TimeoutException("Inner timeout");

        FiskalyTimeoutException exception = new FiskalyTimeoutException(message, inner);

        Assert.Contains(message, exception.Message);
        Assert.StartsWith("Fiskaly request timed out:", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    // ============================================================================
    // Message Format Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Message_AlwaysIncludesTimeoutPrefix()
    {
        FiskalyTimeoutException exception = new FiskalyTimeoutException("after 5 seconds");

        Assert.StartsWith("Fiskaly request timed out:", exception.Message);
        Assert.Contains("after 5 seconds", exception.Message);
    }

    // ============================================================================
    // Inner Exception Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithTimeoutExceptionAsInner_PreservesInnerException()
    {
        TimeoutException inner = new TimeoutException("Operation timed out");

        FiskalyTimeoutException exception = new FiskalyTimeoutException("HTTP request timeout", inner);

        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.Equal("Operation timed out", exception.InnerException.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithOperationCanceledExceptionAsInner_PreservesInnerException()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        OperationCanceledException inner = new OperationCanceledException("Operation was canceled", cts.Token);

        FiskalyTimeoutException exception = new FiskalyTimeoutException("Request canceled due to timeout", inner);

        Assert.IsType<OperationCanceledException>(exception.InnerException);
        Assert.Equal("Operation was canceled", exception.InnerException.Message);
    }

    // ============================================================================
    // Inheritance Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void FiskalyTimeoutException_InheritsFromFiskalyException()
    {
        FiskalyTimeoutException exception = new FiskalyTimeoutException("Timeout");

        Assert.IsAssignableFrom<FiskalyException>(exception);
    }
}
