using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.ValueObjects;

public class ExportIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void New_CreatesValidExportId()
    {
        ExportId exportId = ExportId.New();

        Assert.NotEqual(Guid.Empty, exportId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuid_CreatesExportId()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        ExportId exportId = ExportId.From(uuid);

        Assert.Equal(Guid.Parse(uuid), exportId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuid_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExportId.From(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => ExportId.From(null!));
        Assert.Throws<ArgumentException>(() => ExportId.From(""));
        Assert.Throws<ArgumentException>(() => ExportId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuidVersion_ThrowsArgumentException()
    {
        // UUID v1 (not v4)
        string uuidV1 = "12345678-1234-1abc-9def-123456789012";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ExportId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuid_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        bool success = ExportId.TryParse(uuid, out ExportId exportId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), exportId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuid_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = ExportId.TryParse(invalidUuid, out ExportId exportId);

        Assert.False(success);
        Assert.Equal(default, exportId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullOrWhitespace_ReturnsFalse()
    {
        Assert.False(ExportId.TryParse(null, out _));
        Assert.False(ExportId.TryParse("", out _));
        Assert.False(ExportId.TryParse("   ", out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        ExportId exportId = ExportId.From(uuid);

        string result = exportId.ToString();

        Assert.Equal(uuid, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameUuid_AreEqual()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        ExportId exportId1 = ExportId.From(uuid);
        ExportId exportId2 = ExportId.From(uuid);

        Assert.Equal(exportId1, exportId2);
        Assert.True(exportId1 == exportId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentUuid_AreNotEqual()
    {
        ExportId exportId1 = ExportId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        ExportId exportId2 = ExportId.From("b2c3d4e5-2345-4bcd-9ef0-234567890123");

        Assert.NotEqual(exportId1, exportId2);
        Assert.True(exportId1 != exportId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        ExportId result = ExportId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExportId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExportId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExportId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExportId.Parse(invalidInput, null));
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
        bool success = ExportId.TryParse(validInput, null, out ExportId result);

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
        bool success = ExportId.TryParse(null, null, out ExportId result);

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
        bool success = ExportId.TryParse(invalidInput, null, out ExportId result);

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
        bool success = ExportId.TryParse(invalidInput, null, out ExportId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
