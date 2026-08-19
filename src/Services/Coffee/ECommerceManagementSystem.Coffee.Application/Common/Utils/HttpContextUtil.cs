using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

/// <summary>
/// Utility functions for extracting information from HttpContext
/// NO DEPENDENCIES - Pure functions only
/// </summary>
public static class HttpContextUtil
{
    /// <summary>
    /// Extract client IP address from HTTP context
    /// Handles X-Forwarded-For, X-Real-IP headers
    /// </summary>
    public static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // Check X-Forwarded-For header (behind proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take first IP in the chain
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Extract user agent from HTTP context
    /// Truncates to 500 characters if too long
    /// </summary>
    public static string? GetUserAgent(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        
        // Truncate if too long (database limit)
        if (!string.IsNullOrEmpty(userAgent) && userAgent.Length > 500)
        {
            return userAgent.Substring(0, 500);
        }

        return userAgent;
    }
}