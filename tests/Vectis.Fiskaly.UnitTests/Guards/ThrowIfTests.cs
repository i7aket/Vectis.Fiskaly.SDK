using Vectis.Fiskaly.SDK.Guards;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Guards;

/// <summary>
/// Unit tests for ThrowIf guard clause utility.
/// </summary>
public class ThrowIfTests
{
    // ========================================
    // Default Value Tests (Should Throw)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_ThrowsArgumentException_WhenTssIdIsDefault()
    {
        // Arrange
        TssId defaultTssId = default(TssId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultTssId));

        Assert.Equal("Identifier cannot be empty. (Parameter 'defaultTssId')", exception.Message);
        Assert.Equal("defaultTssId", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_ThrowsArgumentException_WhenClientIdIsDefault()
    {
        // Arrange
        ClientId defaultClientId = default(ClientId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultClientId));

        Assert.Equal("Identifier cannot be empty. (Parameter 'defaultClientId')", exception.Message);
        Assert.Equal("defaultClientId", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_ThrowsArgumentException_WhenTransactionIdIsDefault()
    {
        // Arrange
        TxId defaultTransactionId = default(TxId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultTransactionId));

        Assert.Equal("Identifier cannot be empty. (Parameter 'defaultTransactionId')", exception.Message);
        Assert.Equal("defaultTransactionId", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_ThrowsArgumentException_WhenExportIdIsDefault()
    {
        // Arrange
        ExportId defaultExportId = default(ExportId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultExportId));

        Assert.Equal("Identifier cannot be empty. (Parameter 'defaultExportId')", exception.Message);
        Assert.Equal("defaultExportId", exception.ParamName);
    }

    // ========================================
    // Valid Value Tests (Should NOT Throw)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_DoesNotThrow_WhenTssIdIsValid()
    {
        // Arrange
        TssId validTssId = TssId.New();

        // Act & Assert
        Exception? exception = Record.Exception(() => ThrowIf.Default(validTssId));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_DoesNotThrow_WhenClientIdIsValid()
    {
        // Arrange
        ClientId validClientId = ClientId.New();

        // Act & Assert
        Exception? exception = Record.Exception(() => ThrowIf.Default(validClientId));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_DoesNotThrow_WhenTransactionIdIsValid()
    {
        // Arrange
        TxId validTransactionId = TxId.New();

        // Act & Assert
        Exception? exception = Record.Exception(() => ThrowIf.Default(validTransactionId));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_DoesNotThrow_WhenExportIdIsValid()
    {
        // Arrange
        ExportId validExportId = ExportId.New();

        // Act & Assert
        Exception? exception = Record.Exception(() => ThrowIf.Default(validExportId));

        Assert.Null(exception);
    }

    // ========================================
    // Parameter Name Capture Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_CapturesParameterName_UsingCallerArgumentExpression()
    {
        // Arrange
        TssId tssId = default(TssId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(tssId));

        // CallerArgumentExpression should automatically capture "tssId" as parameter name
        Assert.Equal("tssId", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_CapturesParameterName_FromMethodParameter()
    {
        // Arrange & Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => MethodWithDefaultParameter(default(ClientId)));

        // Should capture "clientId" from the method parameter
        Assert.Equal("clientId", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_UsesExplicitParameterName_WhenProvided()
    {
        // Arrange
        TssId defaultValue = default(TssId);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultValue, "customParamName"));

        Assert.Equal("customParamName", exception.ParamName);
    }

    // ========================================
    // Message Format Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Default_HasConsistentMessageFormat()
    {
        // Arrange
        TssId defaultValue = default(TssId);

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ThrowIf.Default(defaultValue));

        // Assert
        Assert.Contains("Identifier cannot be empty.", exception.Message);
        Assert.Contains("Parameter", exception.Message);
    }

    // ========================================
    // Helper Methods
    // ========================================

    private static void MethodWithDefaultParameter(ClientId clientId)
    {
        ThrowIf.Default(clientId);
    }
}
