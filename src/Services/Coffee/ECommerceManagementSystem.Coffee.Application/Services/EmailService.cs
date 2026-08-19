using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ECommerceManagementSystem.Coffee.Application.Services;

/// <summary>
/// Email service implementation - Pure email sending only
/// NO Entity, NO IUnitOfWork, NO Database operations
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger _logger;

    public EmailService(ILogger logger)
    {
        _logger = logger;
    }

    private SendGridClient CreateClient(string apiKey) => new SendGridClient(apiKey);

    #region OTP Verification Email

    public async Task<SendEmailResult> SendEmailVerificationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendOtpEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var color = mainColor ?? "#4CAF50";
            var subject = $"Xác thực Email - {request.BrandName ?? "Uni Coffee"}";

            var htmlContent = BuildOtpVerificationTemplate(
                brandName: request.BrandName ?? "Unknown brand",
                brandLogo: request.BrandLogoBase64,
                customerName: request.CustomerName ?? "Khách hàng",
                otpCode: request.OtpCode ?? "",
                expiredTime: request.ExpiredTime,
                timeMeasureUnit: request.TimeMeasureUnit ?? "phút",
                mainColor: color
            );

            var plainTextContent = $"Mã OTP của bạn là: {request.OtpCode}. " +
                                   $"Có hiệu lực trong {request.ExpiredTime} {request.TimeMeasureUnit}.";

            var result = await SendEmailInternalAsync(
                client: CreateClient(apiKey),
                fromEmail: fromEmail,
                fromName: fromName ?? request.BrandName ?? "",
                toEmail: request.ToEmail ?? "",
                toName: request.CustomerName ?? "",
                subject: subject,
                htmlContent: htmlContent,
                plainTextContent: plainTextContent,
                cancellationToken: cancellationToken
            );

            return new SendEmailResult
                { IsSuccess = result.IsSuccess, Message = result.Message, Content = htmlContent };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending OTP verification email to {Email}", request.ToEmail);
            return new SendEmailResult
                { IsSuccess = false, Message = $"Lỗi gửi email: {ex.Message}", Content = string.Empty };
        }
    }

    #endregion

    #region Order Confirmation Email

    public async Task<SendEmailResult> SendOrderConfirmationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendConfirmOrderEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var color = mainColor ?? "#4CAF50";
            var subject = $"Xác nhận đơn hàng #{request.OrderCode} - {request.BrandName}";

            var htmlContent = BuildOrderConfirmationTemplate(
                brandName: request.BrandName,
                brandLogo: request.BrandLogoBase64,
                customerName: request.CustomerName,
                orderCode: request.OrderCode,
                orderDate: request.OrderDate ?? DateTime.UtcNow,
                receiveNumber: request.ReceiveNumber,
                receiveAddress: request.ReceiveAddress,
                subTotal: request.SubTotal,
                discountAmount: request.DiscountAmount,
                shippingAmount: request.ShippingAmount,
                totalAmount: request.TotalAmount,
                orderDetails: request.OrderDetails,
                mainColor: color
            );

            var plainTextContent = $"Đơn hàng {request.OrderCode} của bạn đã được xác nhận. " +
                                   $"Tổng tiền: {request.TotalAmount:N0}₫";

            var result = await SendEmailInternalAsync(
                client: CreateClient(apiKey),
                fromEmail: fromEmail,
                fromName: fromName ?? request.BrandName ?? "",
                toEmail: request.CustomerEmail,
                toName: request.CustomerName,
                subject: subject,
                htmlContent: htmlContent,
                plainTextContent: plainTextContent,
                cancellationToken: cancellationToken
            );

            return new SendEmailResult
                { IsSuccess = result.IsSuccess, Message = result.Message, Content = htmlContent };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending order confirmation email to {Email}", request.CustomerEmail);
            return new SendEmailResult
                { IsSuccess = false, Message = $"Lỗi gửi email: {ex.Message}", Content = string.Empty };
        }
    }

    #endregion

    #region Password Reset Email

    public async Task<SendEmailResult> SendPasswordResetLinkAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        SendPasswordResetLinkEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var color = mainColor ?? "#4CAF50";
            var subject = $"Đặt lại mật khẩu - {request.BrandName ?? "Uni Coffee"}";

            var htmlContent = BuildPasswordResetTemplate(
                brandName: request.BrandName ?? "Unknown brand",
                brandLogo: request.BrandLogoBase64,
                customerName: request.CustomerName ?? "Khách hàng",
                toEmail: request.ToEmail,
                resetUrl: request.ResetUrl,
                expiryTime: request.ExpiryTime,
                timeMeasureUnit: request.TimeMeasureUnit ?? "phút",
                mainColor: color
            );

            var plainTextContent = $"Bạn đã yêu cầu đặt lại mật khẩu. " +
                                   $"Vui lòng truy cập link: {request.ResetUrl} " +
                                   $"(Có hiệu lực đến {request.ExpiryTime:dd/MM/yyyy HH:mm} UTC)";

            var result = await SendEmailInternalAsync(
                client: CreateClient(apiKey),
                fromEmail: fromEmail,
                fromName: fromName ?? request.BrandName ?? "",
                toEmail: request.ToEmail,
                toName: request.CustomerName ?? "",
                subject: subject,
                htmlContent: htmlContent,
                plainTextContent: plainTextContent,
                cancellationToken: cancellationToken
            );

            return new SendEmailResult
                { IsSuccess = result.IsSuccess, Message = result.Message, Content = htmlContent };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send password reset email to {Email}", request.ToEmail);
            return new SendEmailResult
                { IsSuccess = false, Message = $"Failed to send email: {ex.Message}", Content = string.Empty };
        }
    }

    #endregion

    #region Password Change Notification

    public async Task<SendEmailResult> SendPasswordChangeNotificationAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string? mainColor,
        string toEmail,
        string customerName,
        string? brandName,
        DateTime changeTime,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var color = mainColor ?? "#28a745";
            var subject = $"Mật khẩu đã được thay đổi - {brandName ?? "Uni Coffee"}";

            var htmlContent = BuildPasswordChangeNotificationTemplate(
                brandName: brandName ?? "Uni Coffee",
                customerName: customerName,
                toEmail: toEmail,
                changeTime: changeTime,
                ipAddress: ipAddress,
                mainColor: color
            );

            var plainTextContent = $"Mật khẩu của bạn đã được thay đổi lúc {changeTime:dd/MM/yyyy HH:mm:ss} UTC. " +
                                   $"Nếu bạn không thực hiện, vui lòng liên hệ support@unicoffeeroastery.vn";

            var result = await SendEmailInternalAsync(
                client: CreateClient(apiKey),
                fromEmail: fromEmail,
                fromName: fromName ?? brandName ?? "",
                toEmail: toEmail,
                toName: customerName,
                subject: subject,
                htmlContent: htmlContent,
                plainTextContent: plainTextContent,
                cancellationToken: cancellationToken
            );

            return new SendEmailResult
                { IsSuccess = result.IsSuccess, Message = result.Message, Content = htmlContent };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send password change notification to {Email}", toEmail);
            return new SendEmailResult
                { IsSuccess = false, Message = $"Failed to send notification: {ex.Message}", Content = string.Empty };
        }
    }

    public async Task<SendEmailResult> SendEmailConsultantAsync(
        string apiKey,
        string fromEmail,
        string? fromName,
        string customerFullName,
        string customerEmail,
        string customerPhone,
        string customerMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"[Yêu cầu tư vấn] từ {customerFullName}";
            var htmlContent = BuildCustomerConsultTemplate(
                customerFullName,
                customerEmail,
                customerPhone,
                customerMessage
            );
            var result = await SendEmailInternalAsync(
                client: CreateClient(apiKey),
                fromEmail: fromEmail,
                fromName: fromName ?? "",
                toEmail: fromEmail,
                toName: fromName ?? "",
                subject: subject,
                htmlContent: htmlContent,
                plainTextContent: null,
                cancellationToken: cancellationToken
            );
            return new SendEmailResult
                { IsSuccess = result.IsSuccess, Message = result.Message, Content = htmlContent };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send password change notification to {Email}", fromEmail);
            return new SendEmailResult
                { IsSuccess = false, Message = $"Failed to send notification: {ex.Message}", Content = string.Empty };
        }
    }

    #endregion

    #region Core Send Email Method

    private async Task<(bool IsSuccess, string Message)> SendEmailInternalAsync(
        SendGridClient client,
        string fromEmail,
        string fromName,
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        string plainTextContent,
        CancellationToken cancellationToken)
    {
        try
        {
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await client.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.Information("Email sent successfully to {Email}. Subject: {Subject}", toEmail, subject);
                return (true, "Email sent successfully");
            }

            var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.Error("Failed to send email to {Email}. Status: {Status}. Error: {Error}",
                toEmail, response.StatusCode, errorBody);

            return (false, $"SendGrid error: {response.StatusCode} - {errorBody}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while sending email to {Email}", toEmail);
            return (false, $"Exception: {ex.Message}");
        }
    }

    #endregion

    #region HTML Templates

    private string BuildOtpVerificationTemplate(
        string brandName,
        string? brandLogo,
        string customerName,
        string otpCode,
        int expiredTime,
        string timeMeasureUnit,
        string mainColor)
    {
        var logoHtml = !string.IsNullOrEmpty(brandLogo)
            ? $"<img src='{brandLogo}' alt='{brandName}' style='max-width: 150px; height: auto;' />"
            : $"<h1 style='color: {mainColor}; margin: 0;'>{brandName}</h1>";

        return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ text-align: center; padding: 20px 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 8px; }}
                            .otp-box {{ background-color: #e7f3ff; padding: 20px; text-align: center; margin: 20px 0; border-radius: 4px; }}
                            .otp-code {{ font-size: 32px; font-weight: bold; color: {mainColor}; letter-spacing: 5px; }}
                            .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                {logoHtml}
                            </div>
                            <div class='content'>
                                <h2 style='color: {mainColor};'>Xác thực Email</h2>
                                <p>Xin chào <strong>{customerName}</strong>,</p>
                                <p>Mã OTP xác thực email của bạn là:</p>
                                <div class='otp-box'>
                                    <div class='otp-code'>{otpCode}</div>
                                </div>
                                <p>Mã này có hiệu lực trong <strong>{expiredTime} {timeMeasureUnit}</strong>.</p>
                                <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                                <p>Trân trọng,<br><strong>{brandName}</strong></p>
                            </div>
                            <div class='footer'>
                                <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                                <p>&copy; {DateTime.UtcNow.Year} {brandName}. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";
    }

    private string BuildOrderConfirmationTemplate(
        string brandName,
        string? brandLogo,
        string customerName,
        string orderCode,
        DateTime orderDate,
        string? receiveNumber,
        string? receiveAddress,
        decimal subTotal,
        decimal discountAmount,
        decimal shippingAmount,
        decimal totalAmount,
        List<SendConfirmOrderDetailEmailRequest>? orderDetails,
        string mainColor)
    {
        var logoHtml = !string.IsNullOrEmpty(brandLogo)
            ? $"<img src='{brandLogo}' alt='{brandName}' style='max-width: 150px; height: auto;' />"
            : $"<h1 style='color: {mainColor}; margin: 0;'>{brandName}</h1>";

        var orderDetailsHtml = string.Empty;
        if (orderDetails != null && orderDetails.Any())
        {
            orderDetailsHtml = $@"
                                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                        <thead>
                                            <tr style='background-color: #f5f5f5;'>
                                                <th style='padding: 10px; text-align: left;'>Sản phẩm</th>
                                                <th style='padding: 10px; text-align: center;'>Số lượng</th>
                                                <th style='padding: 10px; text-align: right;'>Đơn giá</th>
                                                <th style='padding: 10px; text-align: right;'>Thành tiền</th>
                                            </tr>
                                        </thead>
                                        <tbody>";

            foreach (var detail in orderDetails)
            {
                // Chỉ hiển thị ảnh nếu KHÔNG phải gift item và có ảnh
                var productImageHtml = (!detail.IsGiftItem && !string.IsNullOrEmpty(detail.ProductImageBase64))
                    ? $"<img src='{detail.ProductImageBase64}' alt='{detail.ProductName}' style='width: 50px; height: 50px; object-fit: cover; margin-right: 10px; border-radius: 4px; vertical-align: middle;' />"
                    : "";

                // Badge quà tặng cho gift item
                var giftBadgeHtml = detail.IsGiftItem
                    ? $"<span style='background-color: #FF6B6B; color: white; font-size: 11px; padding: 2px 6px; border-radius: 10px; margin-left: 6px; vertical-align: middle;'>🎁 Quà tặng</span>"
                    : "";

                // Gift item thì giá = 0, hiển thị "Miễn phí"
                var unitPriceHtml = detail.IsGiftItem
                    ? "<span style='color: #FF6B6B; font-weight: bold;'>Miễn phí</span>"
                    : $"{detail.UnitPriceSnapshot:N0}₫";

                var totalPriceHtml = detail.IsGiftItem
                    ? "<span style='color: #FF6B6B; font-weight: bold;'>Miễn phí</span>"
                    : $"{detail.TotalPriceSnapshot:N0}₫";

                orderDetailsHtml += $@"
                <tr style='border-bottom: 1px solid #eee;'>
                    <td style='padding: 12px 10px; vertical-align: middle;'>
                        {productImageHtml}
                        <span style='vertical-align: middle;'>{detail.ProductName}</span>
                        {giftBadgeHtml}
                    </td>
                    <td style='padding: 12px 10px; text-align: center; vertical-align: middle;'>{detail.Quantity}</td>
                    <td style='padding: 12px 10px; text-align: right; vertical-align: middle;'>{unitPriceHtml}</td>
                    <td style='padding: 12px 10px; text-align: right; vertical-align: middle;'>{totalPriceHtml}</td>
                </tr>";
            }

            orderDetailsHtml += "</tbody></table>";
        }

        return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ text-align: center; padding: 20px 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 8px; }}
                            .order-info {{ background-color: #e7f3ff; padding: 15px; margin: 20px 0; border-radius: 4px; }}
                            .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
                            table {{ width: 100%; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                {logoHtml}
                            </div>
                            <div class='content'>
                                <h2 style='color: {mainColor};'>✓ Xác nhận đơn hàng</h2>
                                <p>Xin chào <strong>{customerName}</strong>,</p>
                                <p>Đơn hàng <strong>#{orderCode}</strong> của bạn đã được xác nhận thành công!</p>

                                <div class='order-info'>
                                    <p><strong>Mã đơn hàng:</strong> {orderCode}</p>
                                    <p><strong>Ngày đặt:</strong> {orderDate:dd/MM/yyyy HH:mm}</p>
                                    {(!string.IsNullOrEmpty(receiveNumber) ? $"<p><strong>SĐT nhận hàng:</strong> {receiveNumber}</p>" : "")}
                                    {(!string.IsNullOrEmpty(receiveAddress) ? $"<p><strong>Địa chỉ nhận hàng:</strong> {receiveAddress}</p>" : "")}
                                </div>

                                {orderDetailsHtml}

                                <table style='margin-top: 20px;'>
                                    <tr>
                                        <td style='text-align: right; padding: 5px;'>Tạm tính:</td>
                                        <td style='text-align: right; padding: 5px; width: 120px;'>{subTotal:N0}₫</td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: right; padding: 5px;'>Giảm giá:</td>
                                        <td style='text-align: right; padding: 5px; color: red;'>-{discountAmount:N0}₫</td>
                                    </tr>
                                    <tr>
                                        <td style='text-align: right; padding: 5px;'>Phí vận chuyển:</td>
                                        <td style='text-align: right; padding: 5px;'>{shippingAmount:N0}₫</td>
                                    </tr>
                                    <tr style='border-top: 2px solid {mainColor}; font-weight: bold; font-size: 18px;'>
                                        <td style='text-align: right; padding: 10px 5px;'>Tổng cộng:</td>
                                        <td style='text-align: right; padding: 10px 5px; color: {mainColor};'>{totalAmount:N0}₫</td>
                                    </tr>
                                </table>

                                <p>Chúng tôi sẽ thông báo cho bạn khi đơn hàng được xử lý.</p>
                                <p>Trân trọng,<br><strong>{brandName}</strong></p>
                            </div>
                            <div class='footer'>
                                <p>&copy; {DateTime.UtcNow.Year} {brandName}. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";
    }

    private string BuildPasswordResetTemplate(
        string brandName,
        string? brandLogo,
        string customerName,
        string toEmail,
        string resetUrl,
        DateTime expiryTime,
        string timeMeasureUnit,
        string mainColor)
    {
        var logoHtml = !string.IsNullOrEmpty(brandLogo)
            ? $"<img src='{brandLogo}' alt='{brandName}' style='max-width: 150px; height: auto;' />"
            : $"<h1 style='color: {mainColor}; margin: 0;'>{brandName}</h1>";

        return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                            .header {{ text-align: center; padding: 20px 0; }}
                            .content {{ background-color: white; padding: 30px; border-radius: 8px; }}
                            .button {{ display: inline-block; padding: 12px 24px; background-color: {mainColor}; color: white !important; text-decoration: none; border-radius: 4px; margin: 20px 0; font-weight: bold; }}
                            .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 12px; margin: 20px 0; }}
                            .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
                            .link-text {{ color: #666; word-break: break-all; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                {logoHtml}
                            </div>
                            <div class='content'>
                                <h2 style='color: {mainColor};'>Đặt lại mật khẩu</h2>
                                <p>Xin chào <strong>{customerName}</strong>,</p>
                                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản <strong>{toEmail}</strong>.</p>
                                <p>Vui lòng nhấn vào nút bên dưới để đặt lại mật khẩu của bạn:</p>
                                <div style='text-align: center;'>
                                    <a href='{resetUrl}' class='button'>Đặt lại mật khẩu</a>
                                </div>
                                <p>Hoặc copy link sau vào trình duyệt:</p>
                                <p class='link-text'>{resetUrl}</p>
                                <div class='warning'>
                                    <strong>⚠️ Lưu ý quan trọng:</strong>
                                    <ul style='margin: 10px 0;'>
                                        <li>Link này sẽ <strong>hết hạn sau 15 {timeMeasureUnit}</strong> ({expiryTime:dd/MM/yyyy HH:mm} UTC)</li>
                                        <li>Link chỉ có thể sử dụng <strong>một lần duy nhất</strong></li>
                                        <li>Không chia sẻ link này với bất kỳ ai</li>
                                    </ul>
                                </div>
                                <p>Nếu bạn <strong>không yêu cầu</strong> đặt lại mật khẩu, vui lòng bỏ qua email này. Mật khẩu của bạn sẽ không bị thay đổi.</p>
                                <p>Trân trọng,<br><strong>{brandName}</strong></p>
                            </div>
                            <div class='footer'>
                                <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                                <p>&copy; {DateTime.UtcNow.Year} {brandName}. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";
    }

    private string BuildPasswordChangeNotificationTemplate(
        string brandName,
        string customerName,
        string toEmail,
        DateTime changeTime,
        string? ipAddress,
        string mainColor)
    {
        var locationInfo = !string.IsNullOrEmpty(ipAddress)
            ? $"<li>Địa chỉ IP: <strong>{ipAddress}</strong></li>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
        .content {{ background-color: white; padding: 30px; border-radius: 8px; }}
        .alert {{ background-color: #d4edda; border-left: 4px solid {mainColor}; padding: 15px; margin: 20px 0; }}
        .warning {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; }}
        .info-box {{ background-color: #e7f3ff; border: 1px solid #b3d9ff; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='content'>
            <h2 style='color: {mainColor};'>✓ Mật khẩu đã được thay đổi</h2>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            <div class='alert'>
                <p style='margin: 0;'>Mật khẩu cho tài khoản <strong>{toEmail}</strong> đã được thay đổi thành công.</p>
            </div>
            <div class='info-box'>
                <p style='margin: 0 0 10px 0;'><strong>Thông tin thay đổi:</strong></p>
                <ul style='margin: 5px 0;'>
                    <li>Thời gian: <strong>{changeTime:dd/MM/yyyy HH:mm:ss} UTC</strong></li>
                    {locationInfo}
                </ul>
            </div>
            <div class='warning'>
                <p style='margin: 0;'><strong>⚠️ Bạn không thực hiện thay đổi này?</strong></p>
                <p style='margin: 10px 0 0 0;'>
                    Nếu bạn không yêu cầu thay đổi mật khẩu, tài khoản của bạn có thể đã bị xâm nhập. 
                    Vui lòng liên hệ với chúng tôi ngay lập tức qua email: 
                    <strong>support@unicoffeeroastery.vn</strong>
                </p>
            </div>
            <p>Trân trọng,<br><strong>{brandName}</strong></p>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p>&copy; {DateTime.UtcNow.Year} {brandName}. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string BuildCustomerConsultTemplate(
        string customerFullName,
        string customerEmail,
        string customerPhone,
        string customerMessage
    )
    {
        return $@"
            <h2>Yêu cầu tư vấn mới</h2>
            <p><strong>Họ và tên:</strong> {customerFullName}</p>
            <p><strong>Email:</strong> {customerEmail}</p>
            <p><strong>Số điện thoại:</strong> {customerPhone}</p>
            <p><strong>Nội dung:</strong> {customerMessage}</p>
        ";
    }

    #endregion
}