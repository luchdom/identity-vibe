namespace Shared.Common;

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public readonly struct Result
{
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error => IsFailure ? _error ?? throw new InvalidOperationException("Error cannot be null in failed Result") : throw new InvalidOperationException("Cannot access Error of a successful Result");

    private Result(bool isSuccess, Error? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result from string
    /// </summary>
    public static Result Failure(string code, string message) => new(false, new Error(code, message));

    /// <summary>
    /// Implicit conversion from Error to Result
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Binds the result to another operation if successful, otherwise returns the same failure
    /// </summary>
    public Result<T> Bind<T>(Func<Result<T>> binder)
    {
        return IsSuccess ? binder() : Result<T>.Failure(Error);
    }

    /// <summary>
    /// Executes action if successful, returns same result
    /// </summary>
    public Result Tap(Action action)
    {
        if (IsSuccess)
            action();
        return this;
    }

    /// <summary>
    /// Matches success or failure and returns a common type
    /// </summary>
    public T Match<T>(Func<T> onSuccess, Func<Error, T> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}