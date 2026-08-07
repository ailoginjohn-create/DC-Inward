namespace InwardDC.Domain.Exceptions;

/// <summary>Base exception for all domain-level errors raised by the application.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Raised when a required business rule is violated (e.g., insufficient stock).</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Raised when input validation fails. Messages are user friendly.</summary>
public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(IEnumerable<string> errors)
        : base(string.Join(Environment.NewLine, errors)) { }
}

/// <summary>Raised when an entity could not be found.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Raised on duplicate records (code/serial/username conflicts).</summary>
public class DuplicateException : DomainException
{
    public DuplicateException(string message) : base(message) { }
}

/// <summary>Raised for authentication / authorization failures.</summary>
public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message) { }
}

/// <summary>Raised when a requested operation requires a higher permission level.</summary>
public class AuthorizationException : DomainException
{
    public AuthorizationException(string message) : base(message) { }
}
