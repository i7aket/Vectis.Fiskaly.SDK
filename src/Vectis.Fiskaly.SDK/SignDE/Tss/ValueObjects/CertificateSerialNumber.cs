using System.Formats.Asn1;
using System.Text;
using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

public readonly record struct CertificateSerialNumber
{
    private static readonly Regex HexPattern = new("^[0-9A-F]{1,128}$", RegexOptions.Compiled);

    public string Value { get; } = string.Empty;

    private CertificateSerialNumber(string value)
    {
        Value = value;
    }

    public static CertificateSerialNumber From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Certificate serial number cannot be null or whitespace.", nameof(value));
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Certificate serial number must not exceed 128 hex characters.", nameof(value));
        }

        if (!HexPattern.IsMatch(normalized))
        {
            throw new FormatException("Certificate serial number must be a hexadecimal string (0-9, A-F).");
        }

        return new CertificateSerialNumber(normalized);
    }

    public static bool TryFrom(string? value, out CertificateSerialNumber serialNumber)
    {
        serialNumber = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            serialNumber = From(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryFromCertificate(string? certificateData, out CertificateSerialNumber serialNumber)
    {
        serialNumber = default;
        if (string.IsNullOrWhiteSpace(certificateData))
        {
            return false;
        }

        try
        {
            byte[] derData = DecodeCertificateBytes(certificateData);
            serialNumber = ExtractSerialNumber(derData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;

    private static byte[] DecodeCertificateBytes(string certificateData)
    {
        if (!certificateData.Contains("-----BEGIN", StringComparison.Ordinal))
        {
            return Convert.FromBase64String(certificateData);
        }

        StringBuilder builder = new();
        using StringReader reader = new(certificateData);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("-----", StringComparison.Ordinal))
            {
                continue;
            }

            builder.Append(line.Trim());
        }

        return Convert.FromBase64String(builder.ToString());
    }

    private static CertificateSerialNumber ExtractSerialNumber(byte[] derData)
    {
        AsnReader certificate = new(derData, AsnEncodingRules.DER);
        AsnReader certificateSequence = certificate.ReadSequence();
        AsnReader tbsCertificate = certificateSequence.ReadSequence();

        if (tbsCertificate.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            tbsCertificate.ReadEncodedValue();
        }

        ReadOnlyMemory<byte> serialBytes = tbsCertificate.ReadIntegerBytes();
        string hex = Convert.ToHexString(serialBytes.Span);
        hex = hex.TrimStart('0');
        if (hex.Length == 0)
        {
            hex = "0";
        }
        return From(hex);
    }
}
