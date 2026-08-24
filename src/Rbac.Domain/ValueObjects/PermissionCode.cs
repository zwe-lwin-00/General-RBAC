namespace Rbac.Domain.ValueObjects;

/// <summary>
/// Canonical permission identifier in the form <c>resource.action</c>, e.g. <c>passenger.read</c>.
/// </summary>
public readonly record struct PermissionCode
{
    public const int MaxSegmentLength = 64;
    public const int MaxValueLength = MaxSegmentLength * 2 + 1;

    public string Resource { get; }
    public string Action { get; }
    public string Value => $"{Resource}.{Action}";

    private PermissionCode(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }

    public static PermissionCode Create(string resource, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        resource = NormalizeSegment(resource);
        action = NormalizeSegment(action);
        ValidateSegment(resource, nameof(resource), allowDot: true);
        ValidateSegment(action, nameof(action), allowDot: false);
        return new PermissionCode(resource, action);
    }

    public static PermissionCode Parse(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var trimmed = code.Trim();
        var separator = trimmed.LastIndexOf('.');
        if (separator <= 0 || separator == trimmed.Length - 1)
        {
            throw new ArgumentException("Permission code must be in the form 'resource.action'.", nameof(code));
        }

        return Create(trimmed[..separator], trimmed[(separator + 1)..]);
    }

    public static bool TryParse(string? code, out PermissionCode permissionCode)
    {
        permissionCode = default;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            permissionCode = Parse(code);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString() => Value;

    private static string NormalizeSegment(string value) => value.Trim().ToLowerInvariant();

    private static void ValidateSegment(string value, string paramName, bool allowDot)
    {
        if (value.Length is < 1 or > MaxSegmentLength)
        {
            throw new ArgumentException($"Must be 1-{MaxSegmentLength} characters.", paramName);
        }

        if (allowDot && (value.StartsWith('.') || value.EndsWith('.') || value.Contains("..", StringComparison.Ordinal)))
        {
            throw new ArgumentException("Resource cannot start, end, or contain consecutive dots.", paramName);
        }

        if (value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-' && !(allowDot && c == '.')))
        {
            throw new ArgumentException("Only letters, digits, underscore, hyphen, and (for resource) dots are allowed.", paramName);
        }
    }
}
