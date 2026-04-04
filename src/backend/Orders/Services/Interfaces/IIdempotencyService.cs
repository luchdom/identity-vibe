using Orders.Data.Entities;

namespace Orders.Services.Interfaces;

public interface IIdempotencyService
{
    Task<Result<IdempotencyRecord?>> CheckIdempotencyAsync(
        string idempotencyKey,
        string userId,
        string httpMethod,
        string requestPath,
        string? correlationId = null);

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

    Task<Result<int>> CleanupExpiredRecordsAsync();
    bool IsValidIdempotencyKey(string? idempotencyKey);
}
