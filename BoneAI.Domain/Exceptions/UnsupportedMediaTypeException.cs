namespace BoneAI.Domain.Exceptions;

public class UnsupportedMediaTypeException : AppException
{
    public UnsupportedMediaTypeException(string message) : base(message, 415)
    {
    }
}
