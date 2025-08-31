using Microsoft.EntityFrameworkCore;
using Orders.Data;
using Orders.Entities;
using Orders.Services.Interfaces;
using Shared.Common;
using System.Text.RegularExpressions;

namespace Orders.Services;

public class IdempotencyService(OrdersDbContext context) : IIdempotencyService
{
    // Idempotency key should be a GUID, UUID, or similar unique string
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
                return Result<IdempotencyRecord?>.Failure("INVALID_IDEMPOTENCY_KEY", 
                    "Idempotency key must be 10-255 characters and contain only alphanumeric characters, hyphens, and underscores");
            }

            var existingRecord = await context.IdempotencyRecords
                .Where(r => r.IdempotencyKey == idempotencyKey && r.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                if (existingRecord.IsExpired)
                {
                    context.IdempotencyRecords.Remove(existingRecord);
                    await context.SaveChangesAsync();
                    return Result<IdempotencyRecord?>.Success(null);
                }

                return Result<IdempotencyRecord?>.Success(existingRecord);
            }

            return Result<IdempotencyRecord?>.Success(null);
        }
        catch (Exception)
        {
            return Result<IdempotencyRecord?>.Failure("IDEMPOTENCY_CHECK_ERROR", 
                "An error occurred while checking idempotency");
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
                return Result<IdempotencyRecord>.Failure("INVALID_IDEMPOTENCY_KEY", 
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
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint") == true)
        {
            // Handle race condition where another request created the same key
            var existingRecord = await context.IdempotencyRecords
                .Where(r => r.IdempotencyKey == idempotencyKey && r.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingRecord != null)
            {
                return Result<IdempotencyRecord>.Success(existingRecord);
            }

            return Result<IdempotencyRecord>.Failure("CONCURRENT_OPERATION", 
                "A concurrent operation with the same idempotency key was detected");
        }
        catch (Exception)
        {
            return Result<IdempotencyRecord>.Failure("IDEMPOTENCY_CREATION_ERROR", 
                "An error occurred while creating the idempotency record");
        }
    }

    public async Task<Result<int>> CleanupExpiredRecordsAsync()
    {
        try
        {
            var expiredRecords = await context.IdempotencyRecords
                .Where(r => r.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredRecords.Any())
            {
                context.IdempotencyRecords.RemoveRange(expiredRecords);
                await context.SaveChangesAsync();
            }

            return Result<int>.Success(expiredRecords.Count);
        }
        catch (Exception)
        {
            return Result<int>.Failure("CLEANUP_ERROR", 
                "An error occurred while cleaning up expired idempotency records");
        }
    }

    public bool IsValidIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return false;

        return IdempotencyKeyPattern.IsMatch(idempotencyKey);
    }
}