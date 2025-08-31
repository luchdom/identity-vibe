namespace Shared.Common;

/// <summary>
/// Represents an error with code and message
/// </summary>
public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static implicit operator Error(string message) => new("GENERAL_ERROR", message);

    public override string ToString() => $"[{Code}] {Message}";
}