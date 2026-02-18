using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports;

public class ExportExceptionMetadataTests
{
    // Test 1: AlreadyProcessing exception metadata
    [Trait("Category", "Unit")]
    [Fact]
    public void Get_AlreadyProcessing_ReturnsCorrectMetadata()
    {
        // Act
        ExportExceptionInfo metadata = ExportExceptionMetadata.Get(ExportExceptionCode.AlreadyProcessing);

        // Assert
        Assert.Equal(FiskalyErrorCategory.Transient, metadata.Category);
        Assert.True(metadata.IsRetryable);
        Assert.Contains("Wait for current export", metadata.RecoveryHint);
    }

    // Test 2-10: One test per ExportExceptionCode (BadRequest, ExportProcessingTimeout,
    //            IdNotFound, Internal, LogsNotDeleted, NoDataAvailable, TooManyRecords,
    //            TransactionIdNotFound, Unexpected)
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(ExportExceptionCode.BadRequest, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.ExportProcessingTimeout, FiskalyErrorCategory.Transient, true)]
    [InlineData(ExportExceptionCode.IdNotFound, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.Internal, FiskalyErrorCategory.Infrastructure, true)]
    [InlineData(ExportExceptionCode.LogsNotDeleted, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.NoDataAvailable, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.TooManyRecords, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.TransactionIdNotFound, FiskalyErrorCategory.Permanent, false)]
    [InlineData(ExportExceptionCode.Unexpected, FiskalyErrorCategory.Infrastructure, true)]
    public void Get_AllExceptionCodes_ReturnCorrectCategoryAndRetryable(
        ExportExceptionCode code, FiskalyErrorCategory expectedCategory, bool expectedRetryable)
    {
        // Act
        ExportExceptionInfo metadata = ExportExceptionMetadata.Get(code);

        // Assert
        Assert.Equal(expectedCategory, metadata.Category);
        Assert.Equal(expectedRetryable, metadata.IsRetryable);
        Assert.NotEmpty(metadata.RecoveryHint);
    }

    // Test 11: GetCategory returns correct category
    [Trait("Category", "Unit")]
    [Fact]
    public void GetCategory_AlreadyProcessing_ReturnsTransient()
    {
        // Act
        FiskalyErrorCategory category = ExportExceptionMetadata.GetCategory(ExportExceptionCode.AlreadyProcessing);

        // Assert
        Assert.Equal(FiskalyErrorCategory.Transient, category);
    }

    // Test 12: IsRetryable returns correct value
    [Trait("Category", "Unit")]
    [Fact]
    public void IsRetryable_AlreadyProcessing_ReturnsTrue()
    {
        // Act
        bool isRetryable = ExportExceptionMetadata.IsRetryable(ExportExceptionCode.AlreadyProcessing);

        // Assert
        Assert.True(isRetryable);
    }

    // Test 13: GetRecoveryHint returns non-empty string
    [Trait("Category", "Unit")]
    [Fact]
    public void GetRecoveryHint_AllCodes_ReturnsNonEmptyString()
    {
        foreach (ExportExceptionCode code in Enum.GetValues<ExportExceptionCode>())
        {
            // Act
            string hint = ExportExceptionMetadata.GetRecoveryHint(code);

            // Assert
            Assert.NotEmpty(hint);
        }
    }

    // Test 14: Unknown exception code throws ArgumentOutOfRangeException
    [Trait("Category", "Unit")]
    [Fact]
    public void Get_UnknownExceptionCode_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ExportExceptionMetadata.Get((ExportExceptionCode)9999));

        Assert.Contains("Unknown export exception code", exception.Message);
    }
}
