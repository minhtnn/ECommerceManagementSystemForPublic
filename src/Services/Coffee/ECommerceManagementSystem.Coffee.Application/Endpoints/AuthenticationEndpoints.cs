using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ChangePassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerGoogleLoginAndRegister;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerNormalRegister;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ForgotPassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Login;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Logout;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.LogoutAllDevices;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.RefreshToken;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResendOTP.Email;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResetPassword;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.UpdateAccount;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ValidateResetToken;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.VerifyCustomerEmail;
using ECommerceManagementSystem.Coffee.Application.Features.Authentication.Query.GetAccountDetail;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Models.Authentication;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class AuthenticationEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Authentication.AuthenticationEndpoint)
            .WithTags(ApiEndpointConstants.Authentication.Tag);
        // group.MapPost(ApiEndpointConstants.Authentication.Create, CreateAccount)
        //     .WithName(nameof(CreateAccount));
        group.MapPost(ApiEndpointConstants.Authentication.UpdateInformation, UpdateInformation)
            .WithName(nameof(UpdateInformation))
            .DisableAntiforgery()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.Login, Login)
            .WithName(nameof(Login))
            .DisableAntiforgery()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.CustomerGoogleLoginAndRegister,
                CustomerGoogleLoginAndRegister)
            .WithName(nameof(CustomerGoogleLoginAndRegister))
            .DisableAntiforgery()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.Refresh, RefreshToken)
            .WithName(nameof(RefreshToken))
            .AllowAnonymous()
            .DisableAntiforgery()
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized);
        group.MapGet(ApiEndpointConstants.Authentication.AccountDetail, GetAccountDetail)
            .RequireAuthorization()
            .WithName(nameof(GetAccountDetail))
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.CustomerNormalRegister, CustomerNormalRegisterAccount)
            .WithName(nameof(CustomerNormalRegisterAccount))
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.ChangePassword, ChangePassword)
            .WithName(nameof(ChangePassword))
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.CustomerVerifyEmail, CustomerVerifyEmail)
            .WithName(nameof(CustomerVerifyEmail))
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.CustomerResendOtpVerifyEmail, CustomerResendOtpVerifyEmail)
            .WithName(nameof(CustomerResendOtpVerifyEmail))
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Authentication.Logout, Logout)
            .WithName(nameof(Logout))
            .AllowAnonymous()
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status200OK);
        group.MapPost(ApiEndpointConstants.Authentication.LogoutAllDevices, LogoutAllDevices)
            .WithName(nameof(LogoutAllDevices))
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized);
        group.MapPost(ApiEndpointConstants.Authentication.ForgotPassword, ForgotPassword)
            .WithName(nameof(ForgotPassword))
            .WithSummary("Request password reset link")
            .WithDescription("Send password reset link to email if account exists")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
        group.MapPost(ApiEndpointConstants.Authentication.ValidateResetToken, ValidateResetToken)
            .WithName(nameof(ValidateResetToken))
            .WithSummary("Validate password reset token")
            .WithDescription("Check if reset token is valid before showing reset password form")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
        group.MapPost(ApiEndpointConstants.Authentication.ResetPassword, ResetPassword)
            .WithName(nameof(ResetPassword))
            .WithSummary("Reset password with token")
            .WithDescription("Reset password using token from email")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }

    public async Task<IResult> UpdateInformation(IMediator mediator,
        [FromForm] UpdateAccountCommand command,
        ValidationUtil<UpdateAccountCommand> validationUtil)
    {
        // Validate the command using the ValidationUtil
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> ForgotPassword(IMediator mediator, [FromBody] ForgotPasswordCommand command,
        ValidationUtil<ForgotPasswordCommand> validationUtil)
    {
        var result = await mediator.Send(command);
        return Results.Json(result, statusCode: result.Status);
    }

    public async Task<IResult> ValidateResetToken(IMediator mediator, [FromBody] ValidateResetTokenCommand command,
        ValidationUtil<ValidateResetTokenCommand> validationUtil)
    {
        var result = await mediator.Send(command);
        return Results.Json(result);
    }

    public async Task<IResult> ResetPassword(IMediator mediator, [FromBody] ResetPasswordCommand command,
        ValidationUtil<ResetPasswordCommand> validationUtil)
    {
        var result = await mediator.Send(command);
        return Results.Json(result);
    }

    public async Task<IResult> GetAccountDetail(IMediator mediator)
    {
        var request = new GetAccountDetailQuery();
        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateAccount(IMediator mediator, [FromBody] CreateAccountCommand command,
        ValidationUtil<CreateAccountCommand> validationUtil)
    {
        // Validate the command using the ValidationUtil
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> ChangePassword(IMediator mediator, [FromBody] ChangePasswordCommand command,
        ValidationUtil<ChangePasswordCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CustomerNormalRegisterAccount(IMediator mediator,
        [FromForm] CustomerNormalRegisterCommand command,
        ValidationUtil<CustomerNormalRegisterCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CustomerResendOtpVerifyEmail(IMediator mediator,
        [FromBody] ResendEmailOTPCommand command,
        ValidationUtil<ResendEmailOTPCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CustomerVerifyEmail(IMediator mediator, HttpContext httpContext,
        [FromBody] VerifyCustomerEmailCommand command,
        ValidationUtil<VerifyCustomerEmailCommand> validationUtil)
    {
        // Validate the command using the ValidationUtil
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        if (apiResponse.Status == StatusCodes.Status200OK && apiResponse.Data is LoginResponse loginResponse &&
            loginResponse.CookieInfo is { } cookieInfo)
        {
            DeleteRefreshTokenCookie(httpContext);
            SetRefreshTokenCookie(httpContext, cookieInfo.RefreshToken, cookieInfo.Expiry, cookieInfo.Domain);
            loginResponse.CookieInfo = null;
        }

        return Results.Json(apiResponse);
    }

    public async Task<IResult> Login(IMediator mediator, HttpContext httpContext, [FromBody] LoginCommand command,
        ValidationUtil<LoginCommand> validationUtil)
    {
        // Validate the command using the ValidationUtil
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        if (apiResponse.Status == StatusCodes.Status200OK && apiResponse.Data is LoginResponse loginResponse &&
            loginResponse.CookieInfo is { } cookieInfo)
        {
            DeleteRefreshTokenCookie(httpContext);
            SetRefreshTokenCookie(httpContext, cookieInfo.RefreshToken, cookieInfo.Expiry, cookieInfo.Domain);
            loginResponse.CookieInfo = null;
        }

        return Results.Json(apiResponse);
    }

    public async Task<IResult> CustomerGoogleLoginAndRegister(
        IMediator mediator,
        HttpContext httpContext,
        [FromBody] CustomerGoogleLoginAndRegisterCommand andRegisterCommand,
        ValidationUtil<CustomerGoogleLoginAndRegisterCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(andRegisterCommand);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(andRegisterCommand);

        if (apiResponse.Status == StatusCodes.Status200OK &&
            apiResponse.Data is LoginResponse loginResponse && loginResponse.CookieInfo is { } cookieInfo)
        {
            DeleteRefreshTokenCookie(httpContext);
            SetRefreshTokenCookie(httpContext, cookieInfo.RefreshToken, cookieInfo.Expiry, cookieInfo.Domain);
            loginResponse.CookieInfo = null;
        }

        return Results.Json(apiResponse);
    }

    public async Task<IResult> RefreshToken(IMediator mediator, HttpContext httpContext,
        [FromBody] RefreshTokenCommand command)
    {
        var apiResponse = await mediator.Send(command);

        if (apiResponse.Status == StatusCodes.Status200OK &&
            apiResponse.Data is LoginResponse loginResponse && loginResponse.CookieInfo is { } cookieInfo)
        {
            if (loginResponse.ShouldUpdateRefreshToken)
            {
                DeleteRefreshTokenCookie(httpContext);
                SetRefreshTokenCookie(httpContext, cookieInfo.RefreshToken, cookieInfo.Expiry, cookieInfo.Domain);
            }
            loginResponse.CookieInfo = null;
            loginResponse.ShouldUpdateRefreshToken = false;
        }

        return Results.Json(apiResponse);
    }

    private void SetRefreshTokenCookie(
        HttpContext httpContext, 
        string refreshToken, 
        DateTime? expiry,
        string domain)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiry ?? DateTimeOffset.UtcNow.AddDays(30),
            Path = "/",
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain
        };

        httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void DeleteRefreshTokenCookie(HttpContext httpContext)
    {
        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Domain = ".unicoffeeroastery.vn",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };
        httpContext.Response.Cookies.Delete("refreshToken", deleteOptions);
        httpContext.Response.Cookies.Append("refreshToken", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Domain = ".unicoffeeroastery.vn",
            Expires = DateTimeOffset.UtcNow.AddYears(-1),
            MaxAge = TimeSpan.Zero
        });
    }

    public async Task<IResult> Logout(
        IMediator mediator,
        HttpContext httpContext)
    {
        var command = new LogoutCommand();
        var apiResponse = await mediator.Send(command);

        DeleteRefreshTokenCookie(httpContext);

        return Results.Json(apiResponse);
    }

    public async Task<IResult> LogoutAllDevices(
        IMediator mediator,
        HttpContext httpContext)
    {
        var command = new LogoutAllDevicesCommand();
        var apiResponse = await mediator.Send(command);

        // XÓA REFRESH TOKEN COOKIE CỦA DEVICE HIỆN TẠI
        DeleteRefreshTokenCookie(httpContext);

        return Results.Json(apiResponse);
    }
}