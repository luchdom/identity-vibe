using Orders.Entities;
using Shared.Common;

namespace Orders.Services.Interfaces;

public interface IIdempotencyService
{
    /// <summary>
    /// Checks if an idempotency key has been used before
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to check</param>
    /// <param name="userId">The user ID making the request</param>
    /// <param name="httpMethod">The HTTP method of the request</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Result containing existing record if found</returns>
    Task<Result<IdempotencyRecord?>> CheckIdempotencyAsync(
        string idempotencyKey, 
        string userId, 
        string httpMethod, 
        string requestPath, 
        string? correlationId = null);

    /// <summary>
    /// Creates a new idempotency record for a successful operation
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key</param>
    /// <param name="userId">The user ID making the request</param>
    /// <param name="httpMethod">The HTTP method of the request</param>
    /// <param name="requestPath">The request path</param>
    /// <param name="responseStatusCode">The HTTP response status code</param>
    /// <param name="responseBody">The response body to cache</param>
    /// <param name="createdResourceId">Optional ID of created resource</param>
    /// <param name="createdResourceType">Optional type of created resource</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Result containing the created record</returns>
    Task<Result<IdempotencyRecord>> CreateIdempotencyRecordAsync(
        string idempotencyKey,
        string userId,
        string httpMethod,
        string requestPath,
        int responseStatusCode,
        string? responseBody = null,
        int? createdResourceId = null,
        string? createdResourceType = null,
        string? correlationId = null);

    /// <summary>
    /// Removes expired idempotency records from the database
    /// </summary>
    /// <returns>Number of records cleaned up</returns>
    Task<Result<int>> CleanupExpiredRecordsAsync();

    /// <summary>
    /// Validates an idempotency key format
    /// </summary>
    /// <param name="idempotencyKey">The key to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidIdempotencyKey(string? idempotencyKey);
}