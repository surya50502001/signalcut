namespace SignalCut.Application.Common;

public abstract class SignalCutException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected SignalCutException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class InsufficientCreditsException : SignalCutException
{
    public decimal RequiredCredits { get; }
    public decimal AvailableCredits { get; }

    public InsufficientCreditsException(decimal required, decimal available)
        : base("INSUFFICIENT_CREDITS", $"You need {required} credits to process this operation, but only have {available} available.", 402)
    {
        RequiredCredits = required;
        AvailableCredits = available;
    }
}

public class UnauthorizedMediaException : SignalCutException
{
    public Guid SourceId { get; }

    public UnauthorizedMediaException(Guid sourceId, string message)
        : base("UNAUTHORIZED_MEDIA", message, 403)
    {
        SourceId = sourceId;
    }
}

public class TenantAccessDeniedException : SignalCutException
{
    public TenantAccessDeniedException(string message = "You do not have permission to access resources in this organization.")
        : base("TENANT_ACCESS_DENIED", message, 403)
    {
    }
}

public class NotFoundException : SignalCutException
{
    public NotFoundException(string entityName, object key)
        : base("NOT_FOUND", $"{entityName} with identifier '{key}' was not found.", 404)
    {
    }
}

public class ValidationException : SignalCutException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation failures occurred.", 422)
    {
        Errors = errors;
    }

    public ValidationException(string property, string message)
        : base("VALIDATION_ERROR", message, 422)
    {
        Errors = new Dictionary<string, string[]> { { property, new[] { message } } };
    }
}

public class ConflictException : SignalCutException
{
    public ConflictException(string message)
        : base("CONFLICT", message, 409)
    {
    }
}

public class SecurityException : SignalCutException
{
    public SecurityException(string message)
        : base("SECURITY_VIOLATION", message, 400)
    {
    }
}
