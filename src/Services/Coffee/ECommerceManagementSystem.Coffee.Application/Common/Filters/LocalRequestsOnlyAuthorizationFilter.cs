using Hangfire.Dashboard;

namespace ECommerceManagementSystem.Coffee.Application.Common.Filters;

/// <summary>
/// Chỉ cho phép truy cập Hangfire Dashboard từ localhost.
/// Dùng cho môi trường Development/Staging để tránh expose dashboard ra ngoài.
/// </summary>
public class LocalRequestsOnlyAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Cho phép nếu request đến từ loopback (127.0.0.1 hoặc ::1)
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp == null) return false;

        return System.Net.IPAddress.IsLoopback(remoteIp);
    }
}