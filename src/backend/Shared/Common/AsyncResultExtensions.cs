namespace Shared.Common;

/// <summary>
/// Async extensions for Result pattern
/// </summary>
public static class AsyncResultExtensions
{
    /// <summary>
    /// Async Map operation
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<TOut>> mapper)
    {
        var result = await resultTask;
        if (result.IsFailure)
            return Result<TOut>.Failure(result.Error);

        try
        {
            var mappedValue = await mapper(result.Value);
            return Result<TOut>.Success(mappedValue);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure("MAPPING_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Async Bind operation
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<Result<TOut>>> binder)
    {
        var result = await resultTask;
        return result.IsFailure 
            ? Result<TOut>.Failure(result.Error)
            : await binder(result.Value);
    }

    /// <summary>
    /// Async Tap operation
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }
}