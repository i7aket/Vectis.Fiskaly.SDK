using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Responses;

public class TssResponseTests
{
    private readonly JsonSerializerOptions _options;
    private const string SampleCertificateBase64 =
        "MIIDXTCCAuSgAwIBAgIRAPHXGU48md/O8RCUSSgPl7gwCgYIKoZIzj0EAwMwVjELMAkGA1UEBhMCQVQxFTATBgNVBAoTDGZpc2thbHkgR21iSDEkMCIGA1UEAxMbVEVTVC1GSVNLQUxZLVRTRS1ST09ULUNBLTAxMQowCAYDVQQFEwExMB4XDTIyMDkwODE1NTExNloXDTM1MDkwODIzNTk1OVowVjELMAkGA1UEBhMCQVQxFTATBgNVBAoTDGZpc2thbHkgR21iSDEkMCIGA1UEAxMbVEVTVC1GSVNLQUxZLVRTRS1ST09ULUNBLTAxMQowCAYDVQQFEwExMHowFAYHKoZIzj0CAQYJKyQDAwIIAQELA2IABEfh45qvcQVr1gBin0RLhtj/rt87az1UmNREUPrzcZZ6kPmjpyomWA2elr6KHLZX3x2H+TlIocXt5uUS/t2ZwHjVEMR/DQf5Jq7RTzHG9PIsqjvN3UEj7ATrSZqjoQkT26OCAXAwggFsMA4GA1UdDwEB/wQEAwIBBjASBgNVHRMBAf8ECDAGAQH/AgEBMB0GA1UdDgQWBBRKs88WZoCgzdtz/v4A31XDqvmphDBEBggrBgEFBQcBAQQ4MDYwNAYIKwYBBQUHMAKGKGh0dHBzOi8va2Fzc2Vuc2ljaHYtdGVzdC1wa2kuZmlza2FseS5jb20wRwYDVR0SBEAwPoESb2ZmaWNlQGZpc2thbHkuY29thihodHRwczovL2thc3NlbnNpY2h2LXRlc3QtcGtpLmZpc2thbHkuY29tME8GA1UdIARIMEYwRAYKKwYBBAGDtiABAzA2MDQGCCsGAQUFBwIBFihodHRwczovL2thc3NlbnNpY2h2LXRlc3QtcGtpLmZpc2thbHkuY29tMEcGA1UdEQRAMD6BEm9mZmljZUBmaXNrYWx5LmNvbYYoaHR0cHM6Ly9rYXNzZW5zaWNodi10ZXN0LXBraS5maXNrYWx5LmNvbTAKBggqhkjOPQQDAwNnADBkAjAT/Q6IksdSDFTqMZHEpurblwoAlFzLLRiWLn7DXm3oCflvyOXGrHb45rT409t1NhwCMGyFU9S/dvPUmEXjMhUcSa5PGAzRE0NCFDSExQ4Q72DKV9A2OlJJw645o/1uOr1Hrg==";

    public TssResponseTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new UnixEpochDateTimeOffsetConverterFactory(),
                new JsonStringEnumConverter()
            }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_UninitializedTss_ReturnsPopulatedObject()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "description": "Test TSS",
                          "state": "UNINITIALIZED",
                          "_type": "TSS",
                          "_env": "TEST"
                      }
                      """;

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal("Test TSS", response.Description);
        Assert.Equal(TssState.Uninitialized, response.State);
        Assert.Equal(ResourceType.Tss, response.Type);
        Assert.Equal(Env.Test, response.Env);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_InitializedTss_HasSerialNumber()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "state": "INITIALIZED",
                          "serial_number": "fiskaly-12345678",
                          "_type": "TSS",
                          "_env": "TEST",
                          "_version": "2.1.33",
                          "time_creation": 1704276000
                      }
                      """;

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(TssState.Initialized, response.State);
        Assert.Equal("fiskaly-12345678", response.SerialNumber?.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithTimestamps_ParsesCorrectly()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "state": "INITIALIZED",
                          "time_creation": 1704276000,
                          "time_init": 1704276300,
                          "_type": "TSS",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.NotNull(response.TimeInit);
        Assert.Equal(1704276000, response.TimeCreation?.ToUnixTimeSeconds());
        Assert.Equal(1704276300, response.TimeInit?.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithCounters_ParsesSignatureCounter()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "state": "INITIALIZED",
                          "signature_counter": 1234567,
                          "transaction_counter": 890,
                          "_type": "TSS",
                          "_env": "TEST",
                          "_version": "2.1.33",
                          "time_creation": 1704276000
                      }
                      """;

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(1234567, response.SignatureCounter);
        Assert.Equal(890, response.TransactionCounter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithBsiCertificationId_ExposesValueObject()
    {
        string json = """
                      {
                          "_id": "c1d2e3f4-1234-4abc-9def-123456789012",
                          "state": "INITIALIZED",
                          "bsi_certification_id": "0717-2025",
                          "_type": "TSS",
                          "_env": "TEST"
                      }
                      """;

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal("0717-2025", response.BsiCertificationId?.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CertificateSerialNumber_IsDerivedFromCertificate()
    {
        string json = "{\n" +
                      "    \"_id\": \"d1e2f3a4-1234-4abc-9def-123456789012\",\n" +
                      "    \"state\": \"INITIALIZED\",\n" +
                      $"    \"certificate\": \"{SampleCertificateBase64}\",\n" +
                      "    \"_type\": \"TSS\",\n" +
                      "    \"_env\": \"TEST\"\n" +
                      "}";

        TssResponse? response = JsonSerializer.Deserialize<TssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal("F1D7194E3C99DFCEF1109449280F97B8", response.CertificateSerialNumber?.Value);
    }
}
