namespace Shared.Constants;

/// <summary>
/// Security-related constants used across the application
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// Standard claim types used in the system
    /// </summary>
    public static class Claims
    {
        public const string Subject = "sub";
        public const string Id = "id";
        public const string UserId = "user_id";
        public const string Email = "email";
        public const string Name = "name";
        public const string Role = "role";
        public const string Scope = "scope";
    }

    /// <summary>
    /// Standard roles in the system
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Manager = "Manager";
    }

    /// <summary>
    /// Standard scopes in the system
    /// </summary>
    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public const string AdminManage = "admin.manage";
        public const string OrdersRead = "orders.read";
        public const string OrdersWrite = "orders.write";
        public const string OrdersManage = "orders.manage";
    }

    /// <summary>
    /// HTTP headers used for request context
    /// </summary>
    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-ID";
        public const string RequestId = "X-Request-ID";
        public const string ForwardedFor = "X-Forwarded-For";
        public const string RealIp = "X-Real-IP";
        public const string UserAgent = "User-Agent";
    }
}