namespace Rbac.Contracts;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }

    public static Result Ok() => new() { IsSuccess = true };
    public static Result Fail(string error, string? code = null) =>
        new() { IsSuccess = false, Error = error, ErrorCode = code };

    public static Result<T> Ok<T>(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Fail<T>(string error, string? code = null) =>
        new() { IsSuccess = false, Error = error, ErrorCode = code };
}

public class Result<T> : Result
{
    public T? Value { get; init; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class AuthorizationContextDto
{
    public Guid? TenantId { get; init; }
    public Guid? ScopeId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
}

public sealed class AuthorizationDecisionDto
{
    public bool IsAllowed { get; init; }
    public string Permission { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
