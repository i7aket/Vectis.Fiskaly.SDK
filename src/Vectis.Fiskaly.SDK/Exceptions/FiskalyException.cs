namespace Vectis.Fiskaly.SDK.Exceptions;

public class FiskalyException : Exception
{
    public FiskalyException()
        : base()
    {
    }

    public FiskalyException(string message)
        : base(message)
    {
    }

    public FiskalyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
