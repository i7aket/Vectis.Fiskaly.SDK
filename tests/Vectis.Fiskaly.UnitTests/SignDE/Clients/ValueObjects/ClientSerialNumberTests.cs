using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.ValueObjects;

public class ClientSerialNumberTests
{
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("ABC123")]
    [InlineData("POS-001")]
    [InlineData("KASSE (1)")]
    [InlineData("Terminal:2023=v1.5")]
    [InlineData("A-Z a-z 0-9 '()+,-.:=?")]
    public void From_ValidSerialNumber_ReturnsSerialNumber(string value)
    {
        ClientSerialNumber serial = ClientSerialNumber.From(value);

        Assert.Equal(value, serial.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_EmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() =>
            ClientSerialNumber.From(value));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("ABC/123")]
    [InlineData("POS_001")]
    [InlineData("Terminal@Home")]
    [InlineData("KASSE#1")]
    public void From_InvalidCharacters_ThrowsFormatException(string value)
    {
        FormatException exception = Assert.Throws<FormatException>(() =>
            ClientSerialNumber.From(value));

        Assert.Contains("does not match DSFinV-K requirements", exception.Message);
        Assert.Contains("'/' and '_' are excluded", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TooLong_ThrowsFormatException()
    {
        string tooLong = new string('A', 71);

        FormatException exception = Assert.Throws<FormatException>(() =>
            ClientSerialNumber.From(tooLong));

        Assert.Contains("DSFinV-K requirements", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TrimsWhitespace()
    {
        ClientSerialNumber serial = ClientSerialNumber.From("  ABC123  ");

        Assert.Equal("ABC123", serial.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("ABC123", true)]
    [InlineData("ABC/123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParse_VariousValues_ReturnsExpected(string value, bool expected)
    {
        bool result = ClientSerialNumber.TryParse(value, out ClientSerialNumber serial);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.Equal(value, serial.Value);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsValue()
    {
        ClientSerialNumber serial = ClientSerialNumber.From("ABC123");

        string value = serial.ToString();

        Assert.Equal("ABC123", value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromString_CreatesSerialNumber()
    {
        ClientSerialNumber serial = ClientSerialNumber.From("ABC123");

        Assert.Equal("ABC123", serial.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ValidSerialNumber_ReturnsJsonString()
    {
        ClientSerialNumber serial = ClientSerialNumber.From("ABC123");

        string json = JsonSerializer.Serialize(serial);

        Assert.Equal("\"ABC123\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidJsonString_ReturnsSerialNumber()
    {
        string json = "\"ABC123\"";

        ClientSerialNumber serial = JsonSerializer.Deserialize<ClientSerialNumber>(json);

        Assert.Equal("ABC123", serial.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullValue_ReturnsDefault()
    {
        string json = "null";

        ClientSerialNumber result = JsonSerializer.Deserialize<ClientSerialNumber>(json);

        Assert.Equal(default(ClientSerialNumber), result);
        Assert.Null(result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MaxLength_70Characters_IsValid()
    {
        string maxLength = new string('A', 70);

        ClientSerialNumber serial = ClientSerialNumber.From(maxLength);

        Assert.Equal(70, serial.Value.Length);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "ABC123";

        // Act
        ClientSerialNumber result = ClientSerialNumber.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClientSerialNumber.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientSerialNumber.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientSerialNumber.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidCharacters_ThrowsFormatException()
    {
        // Arrange: Contains invalid '/' character
        string invalidInput = "ABC/123";

        // Act & Assert
        Assert.Throws<FormatException>(() => ClientSerialNumber.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "ABC123";

        // Act
        bool success = ClientSerialNumber.TryParse(validInput, null, out ClientSerialNumber result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = ClientSerialNumber.TryParse(null, null, out ClientSerialNumber result);

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
        bool success = ClientSerialNumber.TryParse(invalidInput, null, out ClientSerialNumber result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange: Contains invalid '/' character
        string invalidInput = "ABC/123";

        // Act
        bool success = ClientSerialNumber.TryParse(invalidInput, null, out ClientSerialNumber result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
