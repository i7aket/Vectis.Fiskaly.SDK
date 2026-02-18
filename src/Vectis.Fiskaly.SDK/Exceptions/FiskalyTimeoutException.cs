namespace Vectis.Fiskaly.SDK.Exceptions;

public class FiskalyTimeoutException : FiskalyException
{
    public FiskalyTimeoutException()
        : base()
    {
    }

    public FiskalyTimeoutException(string message)
        : base($"Fiskaly request timed out: {message}")
    {
    }

    public FiskalyTimeoutException(string message, Exception innerException)
        : base($"Fiskaly request timed out: {message}", innerException)
    {
    }
}
