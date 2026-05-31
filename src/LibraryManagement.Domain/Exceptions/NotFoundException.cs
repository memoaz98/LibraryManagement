namespace LibraryManagement.Domain.Exceptions;

/// <summary>
/// Raised when a requested aggregate or entity cannot be found in the repository.
/// </summary>
/// <remarks>
/// Distinct from <see cref="DomainException"/>: a not-found result is not a
/// rule violation, it is the absence of an expected resource. Translated to
/// 404 Not Found at the HTTP boundary.
/// </remarks>
public class NotFoundException : Exception
{
    public string EntityName { get; }
    public object EntityKey { get; }

    public NotFoundException(string entityName, object entityKey)
        : base($"{entityName} with id '{entityKey}' was not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }
}