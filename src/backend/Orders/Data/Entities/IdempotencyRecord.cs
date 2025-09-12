using System.ComponentModel.DataAnnotations;

namespace Orders.Data.Entities;

public class IdempotencyRecord
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string IdempotencyKey { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string HttpMethod { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    
    [Required]
    public int ResponseStatusCode { get; set; }
    
    public string? ResponseBody { get; set; } // Store the response to return for duplicates
    
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    // Reference to the created resource (if applicable)
    public int? CreatedResourceId { get; set; }
    
    [MaxLength(50)]
    public string? CreatedResourceType { get; set; }
    
    // Expiry Management
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(48); // 48 hours default expiry
    
    // Business Methods
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    
    public static IdempotencyRecord Create(string idempotencyKey, string userId, string httpMethod, string requestPath, string? correlationId = null)
    {
        return new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            UserId = userId,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            CorrelationId = correlationId
        };
    }
}