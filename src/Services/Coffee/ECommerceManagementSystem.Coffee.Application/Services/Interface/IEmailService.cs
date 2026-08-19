using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface IEmailService
{
    Task<SendEmailResult> SendEmailVerificationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendOtpEmailRequest request,
        CancellationToken cancellationToken = default
    );

    Task<SendEmailResult> SendOrderConfirmationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendConfirmOrderEmailRequest request,
        CancellationToken cancellationToken = default
    );

    Task<SendEmailResult> SendPasswordResetLinkAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendPasswordResetLinkEmailRequest request,
        CancellationToken cancellationToken = default
    );

    Task<SendEmailResult> SendPasswordChangeNotificationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        string toEmail,
        string customerName,
        string? brandName,
        DateTime changeTime,
        string? ipAddress = null,
        CancellationToken cancellationToken = default
    );

    Task<SendEmailResult> SendEmailConsultantAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string customerFullName,
        string customerEmail,
        string customerPhone,
        string customerMessage,
        CancellationToken cancellationToken = default
    );
}