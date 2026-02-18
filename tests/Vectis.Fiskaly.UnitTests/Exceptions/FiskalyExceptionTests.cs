using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Exceptions;

public class FiskalyExceptionTests
{
    // ============================================================================
    // Constructor Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_NoParameters_CreatesException()
    {
        FiskalyException exception = new FiskalyException();

        Assert.NotNull(exception);
        Assert.IsType<FiskalyException>(exception);
        Assert.NotNull(exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        string message = "Test error message";

        FiskalyException exception = new FiskalyException(message);

        Assert.Equal(message, exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        string message = "Outer error";
        InvalidOperationException inner = new InvalidOperationException("Inner error");

        FiskalyException exception = new FiskalyException(message, inner);

        Assert.Equal(message, exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithEmptyMessage_AcceptsEmptyString()
    {
        FiskalyException exception = new FiskalyException(string.Empty);

        Assert.NotNull(exception);
        Assert.Equal(string.Empty, exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullInnerException_AcceptsNull()
    {
        string message = "Test error";

        FiskalyException exception = new FiskalyException(message, null!);

        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    // ============================================================================
    // Inheritance Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void FiskalyException_IsExceptionType()
    {
        FiskalyException exception = new FiskalyException();

        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FiskalyException_InheritsExceptionProperties()
    {
        FiskalyException exception = new FiskalyException("Test");

        // Verify exception has standard Exception properties
        Assert.NotNull(exception.Data);
        Assert.NotNull(exception.Message);
        Assert.Equal("Test", exception.Message);
    }

    // ============================================================================
    // Behavior Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ContainsExceptionTypeName()
    {
        FiskalyException exception = new FiskalyException("Test error");

        string str = exception.ToString();

        Assert.Contains("FiskalyException", str);
        Assert.Contains("Test error", str);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void InnerException_IsPreserved()
    {
        InvalidOperationException inner = new InvalidOperationException("Inner");
        FiskalyException exception = new FiskalyException("Outer", inner);

        InvalidOperationException captured = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Same(inner, captured);
        Assert.Equal("Inner", captured.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ExceptionData_CanStoreCustomData()
    {
        FiskalyException exception = new FiskalyException("Test");

        exception.Data["CustomKey"] = "CustomValue";
        exception.Data["RequestId"] = 12345;

        Assert.Equal("CustomValue", exception.Data["CustomKey"]);
        Assert.Equal(12345, exception.Data["RequestId"]);
    }
}
