using System.Security.Claims;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

/// <summary>
/// Service để truy cập claims từ JWT token một cách dễ dàng
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// Lấy Account ID của user hiện tại (Required)
    /// </summary>
    /// <returns>Account ID dạng Guid</returns>
    /// <exception cref="UnauthorizedAccessException">Khi không tìm thấy AccountId trong token</exception>
    /// <exception cref="InvalidOperationException">Khi AccountId không đúng định dạng Guid</exception>
    Guid GetCurrentAccountId();

    /// <summary>
    /// Lấy Username của user hiện tại (Required)
    /// </summary>
    /// <returns>Username</returns>
    /// <exception cref="UnauthorizedAccessException">Khi không tìm thấy Username trong token</exception>
    string GetCurrentUsername();

    /// <summary>
    /// Lấy Email của user hiện tại (Optional)
    /// </summary>
    /// <returns>Email hoặc null nếu không có</returns>
    string? GetCurrentEmail();

    /// <summary>
    /// Lấy Role của user hiện tại (Required)
    /// </summary>
    /// <returns>Role dạng string</returns>
    /// <exception cref="UnauthorizedAccessException">Khi không tìm thấy Role trong token</exception>
    string GetCurrentRole();

    /// <summary>
    /// Lấy Role của user dạng Enum
    /// </summary>
    /// <returns>ERoleName enum</returns>
    ERole GetCurrentRoleEnum();
    
    /// <summary>
    /// Lấy ReferenceId của user
    /// </summary>
    /// <returns>Id</returns>
    Guid GetCurrentReferenceId();

    /// <summary>
    /// Kiểm tra user có role cụ thể không
    /// </summary>
    /// <param name="role">Role name hoặc ERoleName</param>
    /// <returns>True nếu user có role đó</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Kiểm tra user có claim cụ thể không
    /// </summary>
    /// <param name="claimType">Loại claim (VD: "BrandId")</param>
    /// <returns>True nếu claim tồn tại</returns>
    bool HasClaim(string claimType);

    /// <summary>
    /// Lấy giá trị của claim bất kỳ
    /// </summary>
    /// <param name="claimType">Loại claim</param>
    /// <returns>Giá trị claim hoặc null</returns>
    string? GetClaimValue(string claimType);

    /// <summary>
    /// Lấy tất cả claims của user (Debug/Logging)
    /// </summary>
    /// <returns>Danh sách claims</returns>
    IEnumerable<Claim> GetAllClaims();

    /// <summary>
    /// Lấy JWT Token ID (jti)
    /// </summary>
    /// <returns>JWT ID</returns>
    string? GetJwtId();

    /// <summary>
    /// Lấy thời gian token được phát hành (iat)
    /// </summary>
    /// <returns>DateTime hoặc null</returns>
    DateTime? GetIssuedAt();
}