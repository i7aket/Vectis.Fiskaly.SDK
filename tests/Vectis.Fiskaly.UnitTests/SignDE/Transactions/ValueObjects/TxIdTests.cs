using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class TxIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void New_CreatesValidTransactionId()
    {
        TxId transactionId = TxId.New();

        Assert.NotEqual(Guid.Empty, transactionId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuid_CreatesTransactionId()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        TxId transactionId = TxId.From(uuid);

        Assert.Equal(Guid.Parse(uuid), transactionId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuid_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => TxId.From(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => TxId.From(null!));
        Assert.Throws<ArgumentException>(() => TxId.From(""));
        Assert.Throws<ArgumentException>(() => TxId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuidVersion_ThrowsArgumentException()
    {
        // UUID v1 (not v4)
        string uuidV1 = "12345678-1234-1abc-9def-123456789012";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => TxId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuid_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        bool success = TxId.TryParse(uuid, out TxId transactionId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), transactionId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuid_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = TxId.TryParse(invalidUuid, out TxId transactionId);

        Assert.False(success);
        Assert.Equal(default, transactionId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullOrWhitespace_ReturnsFalse()
    {
        Assert.False(TxId.TryParse(null, out _));
        Assert.False(TxId.TryParse("", out _));
        Assert.False(TxId.TryParse("   ", out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        TxId transactionId = TxId.From(uuid);

        string result = transactionId.ToString();

        Assert.Equal(uuid, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameUuid_AreEqual()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        TxId transactionId1 = TxId.From(uuid);
        TxId transactionId2 = TxId.From(uuid);

        Assert.Equal(transactionId1, transactionId2);
        Assert.True(transactionId1 == transactionId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentUuid_AreNotEqual()
    {
        TxId transactionId1 = TxId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        TxId transactionId2 = TxId.From("b2c3d4e5-2345-4bcd-9ef0-234567890123");

        Assert.NotEqual(transactionId1, transactionId2);
        Assert.True(transactionId1 != transactionId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        TxId result = TxId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TxId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TxId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TxId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TxId.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        bool success = TxId.TryParse(validInput, null, out TxId result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = TxId.TryParse(null, null, out TxId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidInput_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "";

        // Act
        bool success = TxId.TryParse(invalidInput, null, out TxId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuidFormat_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act
        bool success = TxId.TryParse(invalidInput, null, out TxId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
