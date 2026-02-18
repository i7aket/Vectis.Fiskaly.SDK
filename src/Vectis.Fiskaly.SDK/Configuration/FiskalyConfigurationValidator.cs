namespace Vectis.Fiskaly.SDK.Configuration;

using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

public sealed class FiskalyConfigurationValidator : IValidateOptions<FiskalyConfiguration>
{
    public ValidateOptionsResult Validate(string? name, FiskalyConfiguration options)
    {
        List<string> errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            errors.Add("Fiskaly API Key is required. Configure 'Fiskaly:ApiKey'.");
        }
        else
        {
            if (options.ApiKey.Length < 1 || options.ApiKey.Length > 512)
            {
                errors.Add(
                    $"Fiskaly API Key length must be between 1 and 512 characters. " +
                    $"Current length: {options.ApiKey.Length}. " +
                    "Verify your configuration in appsettings.json or environment variables.");
            }

            if (!Regex.IsMatch(options.ApiKey, @".*[^\s].*"))
            {
                errors.Add(
                    "Fiskaly API Key must contain at least one non-whitespace character. " +
                    "Current value appears to be only whitespace. " +
                    "Verify your configuration.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.ApiSecret))
        {
            errors.Add("Fiskaly API Secret is required. Configure 'Fiskaly:ApiSecret'.");
        }
        else
        {
            if (!Regex.IsMatch(options.ApiSecret, @"^[0-9A-Za-z]{43}$"))
            {
                errors.Add(
                    "Fiskaly API Secret must be exactly 43 alphanumeric characters. " +
                    "Expected format: test_xxxxxxxxxxxxxxxxxxxxx_xxx (43 chars total). " +
                    $"Current length: {options.ApiSecret.Length}. " +
                    "Verify your configuration in appsettings.json or environment variables.");
            }
        }

        if (!TryValidateUrl(options.BaseUrl, options.AllowHttpForPrivateNetworks))
        {
            errors.Add("Fiskaly Base URL must be a valid HTTPS URL (HTTP allowed only for localhost/testing; private LAN allowed when Fiskaly:AllowHttpForPrivateNetworks=true).");
        }
        else if (!string.IsNullOrEmpty(options.BaseUrl) && !options.BaseUrl.EndsWith("/"))
        {
            errors.Add(
                "Fiskaly Base URL must end with trailing slash for correct URI resolution. " +
                $"Current value: '{options.BaseUrl}'. " +
                "Add '/' to the end.");
        }

        if (!TryValidateUrl(options.ManagementBaseUrl, options.AllowHttpForPrivateNetworks))
        {
            errors.Add("Fiskaly Management Base URL must be a valid HTTPS URL (HTTP allowed only for localhost/testing; private LAN allowed when Fiskaly:AllowHttpForPrivateNetworks=true).");
        }
        else if (!string.IsNullOrEmpty(options.ManagementBaseUrl) && !options.ManagementBaseUrl.EndsWith("/"))
        {
            errors.Add(
                "Fiskaly Management Base URL must end with trailing slash for correct URI resolution. " +
                $"Current value: '{options.ManagementBaseUrl}'. " +
                "Add '/' to the end.");
        }


        ValidateClient(options.AuthClient, "AuthClient", errors);
        ValidateClient(options.AdminClient, "AdminClient", errors);
        ValidateClient(options.TssClient, "TssClient", errors);
        ValidateClient(options.TransactionClient, "TransactionClient", errors);
        ValidateClient(options.ExportClient, "ExportClient", errors);
        ValidateClient(options.ClientManagementClient, "ClientManagementClient", errors);
        ValidateClient(options.OrganizationClient, "OrganizationClient", errors);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateClient(FiskalyClientConfiguration? client, string name, List<string> errors)
    {
        if (client is null)
        {
            errors.Add($"{name} configuration is required.");
            return;
        }

        if (client.TimeoutSeconds <= 0)
        {
            errors.Add($"{name}: Timeout must be greater than 0 seconds.");
        }

        if (client.RetryCount is < 1 or > 10)
        {
            errors.Add($"{name}: Retry count must be between 1 and 10.");
        }

        if (client.CategoryDelays.TransientDelaySeconds <= 0)
        {
            errors.Add($"{name}: CategoryDelays.TransientDelaySeconds must be greater than 0 seconds.");
        }

        if (client.CategoryDelays.InfrastructureDelaySeconds <= 0)
        {
            errors.Add($"{name}: CategoryDelays.InfrastructureDelaySeconds must be greater than 0 seconds.");
        }

        if (client.CategoryDelays.AuthenticationDelaySeconds <= 0)
        {
            errors.Add($"{name}: CategoryDelays.AuthenticationDelaySeconds must be greater than 0 seconds.");
        }

        if (client.CircuitBreakerThreshold < 0)
        {
            errors.Add($"{name}: Circuit breaker threshold must be >= 0 (0 = disabled).");
        }

        if (client.CircuitBreakerDurationSeconds <= 0)
        {
            errors.Add($"{name}: Circuit breaker duration must be greater than 0 seconds.");
        }
    }

    private static bool TryValidateUrl(string url, bool allowHttpForPrivateNetworks)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
            return false;

        if (uriResult.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        if (uriResult.Scheme == Uri.UriSchemeHttp && uriResult.IsLoopback)
        {
            return true;
        }

        if (uriResult.Scheme == Uri.UriSchemeHttp
            && allowHttpForPrivateNetworks
            && IsPrivateNetworkHost(uriResult.Host))
        {
            return true;
        }

        return false;
    }

    private static bool IsPrivateNetworkHost(string host)
    {
        if (!IPAddress.TryParse(host, out IPAddress? address))
        {
            return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            byte[] octets = address.GetAddressBytes();

            return octets[0] == 10
                   || (octets[0] == 172 && octets[1] is >= 16 and <= 31)
                   || (octets[0] == 192 && octets[1] == 168)
                   || (octets[0] == 169 && octets[1] == 254);
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
            {
                return true;
            }

            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 0xfc || bytes[0] == 0xfd;
        }

        return false;
    }
}
