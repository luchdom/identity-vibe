using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Data.Entities;
using Orders.Services.Interfaces;
using Shared.Common;
using System.Text.RegularExpressions;

namespace Orders.Services;

public sealed class IdempotencyService(OrdersDbContext context) : IIdempotencyService
{
    private static readonly Regex IdempotencyKeyPattern = new(@"^[a-zA-Z0-9\-_]{10,255}$", RegexOptions.Compiled);

    public async Task<Result<IdempotencyRecord?>> CheckIdempotencyAsync(
        string idempotencyKey,
        string userId,
        string httpMethod,
        string requestPath,
        string? correlationId = null)
    {
        try
        {
            if (!IsValidIdempotencyKey(idempotencyKey))
            {
                return ResultExtensions.Failure<IdempotencyRecord?>(
                    "INVALID_IDEMPOTENCY_KEY",
                    "Idempotency key must be 10-255 characters and contain only alphanumeric characters, hyphens, and underscores");
            }

            var existingRecord = await context.IdempotencyRecords
                .Where(record => record.IdempotencyKey == idempotencyKey && record.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingRecord == null)
            {
                return Result<IdempotencyRecord?>.Success(null);
            }

            if (existingRecord.IsExpired)
            {
                context.IdempotencyRecords.Remove(existingRecord);
                await context.SaveChangesAsync();
                return Result<IdempotencyRecord?>.Success(null);
            }

            return Result<IdempotencyRecord?>.Success(existingRecord);
        }
        catch (Exception ex)
        {
            return ResultExtensions.Failure<IdempotencyRecord?>("IDEMPOTENCY_CHECK_ERROR", $"An error occurred while checking idempotency: {ex.Message}");
        }
    }

    public async Task<Result<IdempotencyRecord>> CreateIdempotencyRecordAsync(
        string idempotencyKey,
        string userId,
        string httpMethod,
        string requestPath,
        int responseStatusCode,
        string? responseBody = null,
        int? createdResourceId = null,
        string? createdResourceType = null,
        string? correlationId = null)
    {
        try
        {
            if (!IsValidIdempotencyKey(idempotencyKey))
            {
                return ResultExtensions.Failure<IdempotencyRecord>(
                    "INVALID_IDEMPOTENCY_KEY",
                    "Idempotency key must be 10-255 characters and contain only alphanumeric characters, hyphens, and underscores");
            }

            var record = new IdempotencyRecord
            {
                IdempotencyKey = idempotencyKey,
                UserId = userId,
                HttpMethod = httpMethod,
                RequestPath = requestPath,
                ResponseStatusCode = responseStatusCode,
                ResponseBody = responseBody,
                CreatedResourceId = createdResourceId,
                CreatedResourceType = createdResourceType,
                CorrelationId = correlationId
            };

            context.IdempotencyRecords.Add(record);
            await context.SaveChangesAsync();

            return Result<IdempotencyRecord>.Success(record);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) == true)
        {
            var existingRecord = await context.IdempotencyRecords
                .Where(record => record.IdempotencyKey == idempotencyKey && record.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                return Result<IdempotencyRecord>.Success(existingRecord);
            }

            return ResultExtensions.Failure<IdempotencyRecord>("CONCURRENT_OPERATION", $"A concurrent operation with the same idempotency key was detected: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ResultExtensions.Failure<IdempotencyRecord>("IDEMPOTENCY_CREATION_ERROR", $"An error occurred while creating the idempotency record: {ex.Message}");
        }
    }

    public async Task<Result<int>> CleanupExpiredRecordsAsync()
    {
        try
        {
            var expiredRecords = await context.IdempotencyRecords
                .Where(record => record.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredRecords.Count > 0)
            {
                context.IdempotencyRecords.RemoveRange(expiredRecords);
                await context.SaveChangesAsync();
            }

            return Result<int>.Success(expiredRecords.Count);
        }
        catch (Exception ex)
        {
            return ResultExtensions.Failure<int>("CLEANUP_ERROR", $"An error occurred while cleaning up expired idempotency records: {ex.Message}");
        }
    }

    public bool IsValidIdempotencyKey(string? idempotencyKey)
    {
        return !string.IsNullOrWhiteSpace(idempotencyKey)
            && IdempotencyKeyPattern.IsMatch(idempotencyKey);
    }
}
