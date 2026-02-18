namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

internal static class JwtTokenValidator
{
    private const int MinLength = 10;
    private const int MaxLength = 4096;

    public static void Validate(string token, string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token, argumentName);

        if (token.Length < MinLength)
        {
            throw new ArgumentException(
                $"JWT token is too short ({token.Length} characters). Minimum length is {MinLength} characters.",
                argumentName);
        }

        if (token.Length > MaxLength)
        {
            throw new ArgumentException(
                $"JWT token exceeds maximum length ({token.Length} characters). Maximum length is {MaxLength} characters.",
                argumentName);
        }

        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            bool isValid = (c >= 'A' && c <= 'Z') ||
                           (c >= 'a' && c <= 'z') ||
                           (c >= '0' && c <= '9') ||
                           c == '-' || c == '_' || c == '.';

            if (!isValid)
            {
                throw new ArgumentException(
                    $"JWT token contains invalid character '{c}' at position {i}. Only Base64Url characters (A-Z, a-z, 0-9, -, _, .) are allowed.",
                    argumentName);
            }
        }

        string[] parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException(
                $"Invalid JWT format. Expected 3 parts (header.payload.signature), but found {parts.Length} parts.",
                argumentName);
        }

        if (string.IsNullOrEmpty(parts[0]))
        {
            throw new ArgumentException("JWT header cannot be empty.", argumentName);
        }

        if (string.IsNullOrEmpty(parts[1]))
        {
            throw new ArgumentException("JWT payload cannot be empty.", argumentName);
        }

        if (string.IsNullOrEmpty(parts[2]))
        {
            throw new ArgumentException("JWT signature cannot be empty.", argumentName);
        }
    }
}
