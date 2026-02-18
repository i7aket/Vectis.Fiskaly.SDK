using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.ValueObjects;

/// <summary>
/// Comprehensive test suite for TssSerialNumber value object.
/// Covers all validation rules, edge cases, and DSFinV-K 2.3 compliance requirements.
/// Pattern: ^[A-Za-z0-9 '()+,-.:=?]{1,70}$ (excludes '/' and '_' for DSFinV-K 2.3)
/// </summary>
public class TssSerialNumberTests
{
    // ===========================================================================================
    // Category 1: Valid Formats (8 tests)
    // ===========================================================================================

    [Fact]
    public void From_Simple_Alphanumeric_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("ABC123");

        // Assert
        Assert.Equal("ABC123", serialNumber.Value);
    }

    [Fact]
    public void From_With_Spaces_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("ABC 123 XYZ");

        // Assert
        Assert.Equal("ABC 123 XYZ", serialNumber.Value);
    }

    [Fact]
    public void From_With_Special_Chars_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("ABC-123.456:789=XYZ");

        // Assert
        Assert.Equal("ABC-123.456:789=XYZ", serialNumber.Value);
    }

    [Fact]
    public void From_Min_Length_One_Char_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("A");

        // Assert
        Assert.Equal("A", serialNumber.Value);
    }

    [Fact]
    public void From_Max_Length_70_Chars_Valid()
    {
        // Arrange
        string value = new string('A', 70); // Exactly 70 characters

        // Act
        TssSerialNumber serialNumber = TssSerialNumber.From(value);

        // Assert
        Assert.Equal(value, serialNumber.Value);
        Assert.Equal(70, serialNumber.Value.Length);
    }

    [Fact]
    public void From_Mixed_Case_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("AbC123xYz");

        // Assert
        Assert.Equal("AbC123xYz", serialNumber.Value);
    }

    [Fact]
    public void From_Numbers_Only_Valid()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("1234567890");

        // Assert
        Assert.Equal("1234567890", serialNumber.Value);
    }

    [Fact]
    public void From_All_Allowed_Special_Chars_Valid()
    {
        // Arrange - All allowed special chars: space '()+,-.:=?
        string value = "ABC '()+,-.:=? 123";

        // Act
        TssSerialNumber serialNumber = TssSerialNumber.From(value);

        // Assert
        Assert.Equal(value, serialNumber.Value);
    }

    // ===========================================================================================
    // Category 2: Invalid Formats (10 tests)
    // ===========================================================================================

    [Fact]
    public void From_Empty_String_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => TssSerialNumber.From(""));
    }

    [Fact]
    public void From_Whitespace_Only_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => TssSerialNumber.From("   "));
    }

    [Fact]
    public void From_Null_String_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => TssSerialNumber.From(null!));
    }

    [Fact]
    public void From_Exceeds_MaxLength_71_Chars_ThrowsFormatException()
    {
        // Arrange - 71 characters (exceeds max of 70)
        string value = new string('A', 71);

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
        Assert.Contains("1-70 characters", exception.Message);
    }

    [Fact]
    public void From_Contains_Slash_ThrowsFormatException()
    {
        // Arrange - Forward slash '/' is excluded for DSFinV-K 2.3 compliance
        string value = "ABC/123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
        Assert.Contains("'/' and '_' are excluded", exception.Message);
    }

    [Fact]
    public void From_Contains_Underscore_ThrowsFormatException()
    {
        // Arrange - Underscore '_' is excluded for DSFinV-K 2.3 compliance
        string value = "ABC_123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
        Assert.Contains("'/' and '_' are excluded", exception.Message);
    }

    [Fact]
    public void From_Contains_Invalid_Char_At_ThrowsFormatException()
    {
        // Arrange - '@' is not in allowed character set
        string value = "ABC@123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
    }

    [Fact]
    public void From_Contains_Invalid_Char_Hash_ThrowsFormatException()
    {
        // Arrange - '#' is not in allowed character set
        string value = "ABC#123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
    }

    [Fact]
    public void From_Contains_Newline_ThrowsFormatException()
    {
        // Arrange
        string value = "ABC\n123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
    }

    [Fact]
    public void From_Unicode_Chars_ThrowsFormatException()
    {
        // Arrange - Unicode characters not allowed
        string value = "ABC€123";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => TssSerialNumber.From(value));
        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
    }

    // ===========================================================================================
    // Category 3: TryParse (4 tests)
    // ===========================================================================================

    [Fact]
    public void TryParse_Valid_ReturnsTrue()
    {
        // Arrange
        string value = "ABC123";

        // Act
        bool result = TssSerialNumber.TryParse(value, null, out TssSerialNumber serialNumber);

        // Assert
        Assert.True(result);
        Assert.NotNull(serialNumber);
        Assert.Equal(value, serialNumber.Value);
    }

    [Fact]
    public void TryParse_Invalid_ReturnsFalse()
    {
        // Arrange - Contains invalid character '/'
        string value = "ABC/123";

        // Act
        bool result = TssSerialNumber.TryParse(value, null, out TssSerialNumber serialNumber);

        // Assert
        Assert.False(result);
        Assert.Equal(default, serialNumber);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        // Arrange & Act
        bool result = TssSerialNumber.TryParse(null, null, out TssSerialNumber serialNumber);

        // Assert
        Assert.False(result);
        Assert.Equal(default, serialNumber);
    }

    [Fact]
    public void TryParse_Whitespace_ReturnsFalse()
    {
        // Arrange & Act
        bool result = TssSerialNumber.TryParse("   ", null, out TssSerialNumber serialNumber);

        // Assert
        Assert.False(result);
        Assert.Equal(default, serialNumber);
    }

    // ===========================================================================================
    // Category 4: Trimming (3 tests)
    // ===========================================================================================

    [Fact]
    public void From_Leading_Whitespace_Trimmed()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("   ABC123");

        // Assert
        Assert.Equal("ABC123", serialNumber.Value);
    }

    [Fact]
    public void From_Trailing_Whitespace_Trimmed()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("ABC123   ");

        // Assert
        Assert.Equal("ABC123", serialNumber.Value);
    }

    [Fact]
    public void From_Both_Whitespace_Trimmed()
    {
        // Arrange & Act
        TssSerialNumber serialNumber = TssSerialNumber.From("   ABC123   ");

        // Assert
        Assert.Equal("ABC123", serialNumber.Value);
    }

    // ===========================================================================================
    // Category 5: Explicit API (2 tests)
    // ===========================================================================================

    [Fact]
    public void From_ValidString_CreatesSerialNumber()
    {
        // Arrange
        string value = "ABC123";

        // Act
        TssSerialNumber serialNumber = TssSerialNumber.From(value);

        // Assert
        Assert.Equal(value, serialNumber.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        TssSerialNumber serialNumber = TssSerialNumber.From("ABC123");

        // Act
        string value = serialNumber.ToString();

        // Assert
        Assert.Equal("ABC123", value);
    }

    // ===========================================================================================
    // Category 6: Equality (2 tests)
    // ===========================================================================================

    [Fact]
    public void Equality_Same_Value_Equal()
    {
        // Arrange
        TssSerialNumber serialNumber1 = TssSerialNumber.From("ABC123");
        TssSerialNumber serialNumber2 = TssSerialNumber.From("ABC123");

        // Act & Assert
        Assert.Equal(serialNumber1, serialNumber2);
        Assert.True(serialNumber1 == serialNumber2);
        Assert.False(serialNumber1 != serialNumber2);
        Assert.Equal(serialNumber1.GetHashCode(), serialNumber2.GetHashCode());
    }

    [Fact]
    public void Equality_Different_Value_NotEqual()
    {
        // Arrange
        TssSerialNumber serialNumber1 = TssSerialNumber.From("ABC123");
        TssSerialNumber serialNumber2 = TssSerialNumber.From("XYZ789");

        // Act & Assert
        Assert.NotEqual(serialNumber1, serialNumber2);
        Assert.False(serialNumber1 == serialNumber2);
        Assert.True(serialNumber1 != serialNumber2);
    }

    // ===========================================================================================
    // Category 7: JSON (1 test)
    // ===========================================================================================

    [Fact]
    public void JSON_Serialization_Roundtrip()
    {
        // Arrange
        TssSerialNumber originalSerialNumber = TssSerialNumber.From("ABC123");
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act - Serialize
        string json = JsonSerializer.Serialize(originalSerialNumber, options);

        // Assert - JSON should be just the string value
        Assert.Equal("\"ABC123\"", json);

        // Act - Deserialize
        TssSerialNumber deserializedSerialNumber = JsonSerializer.Deserialize<TssSerialNumber>(json, options);

        // Assert - Roundtrip successful
        Assert.NotNull(deserializedSerialNumber);
        Assert.Equal(originalSerialNumber, deserializedSerialNumber);
        Assert.Equal("ABC123", deserializedSerialNumber.Value);
    }
}
