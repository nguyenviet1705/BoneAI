namespace BoneAI.Domain.Exceptions;

public class PayloadTooLargeException : AppException
{
    public PayloadTooLargeException(string message) : base(message, 413)
    {
    }
}
