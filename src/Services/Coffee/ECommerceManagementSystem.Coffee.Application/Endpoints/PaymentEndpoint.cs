using Carter;
using ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.CancelPayment;
using ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.HandlePaymentCallback;
using ECommerceManagementSystem.Coffee.Application.Features.Payments.Query.GetPaymentStatus;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;
using PayOS.Models.Webhooks;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class PaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Payment.PaymentsEndpoint)
            .WithTags(ApiEndpointConstants.Payment.Tag);

        group.MapPost(ApiEndpointConstants.Payment.CallBackPayOs, CallBackPayOs)
            .AllowAnonymous() // PayOS calls this endpoint
            .WithName(nameof(CallBackPayOs))
            .WithDescription("PayOS webhook/IPN callback endpoint")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces<object>(StatusCodes.Status500InternalServerError);

        group.MapGet(ApiEndpointConstants.Payment.ReturnPayOs, ReturnPayOs)
            .AllowAnonymous() // User redirected here
            .WithName(nameof(ReturnPayOs))
            .WithDescription("PayOS return URL - where user is redirected after payment")
            .Produces(StatusCodes.Status302Found)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);

        group.MapGet(ApiEndpointConstants.Payment.GetPaymentStatus, GetPaymentStatus)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetPaymentStatus))
            .WithDescription("Get payment status by order ID")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound);

        group.MapPost(ApiEndpointConstants.Payment.CancelPayment, CancelPayment)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(CancelPayment))
            .WithDescription("Cancel payment/order")
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// PayOS IPN Callback - Called by PayOS server when payment status changes
    /// This endpoint MUST return 200 OK with specific format for PayOS to mark webhook as successful
    /// </summary>
    public static async Task<IResult> CallBackPayOs(
        [FromBody] Webhook webhookData,
        IMediator mediator,
        ILogger logger)
    {
        logger.Information("🔔 PayOS webhook received");

        if (webhookData == null)
        {
            logger.Warning("⚠️ Webhook data is null");
            return Results.BadRequest(new { code = "01", desc = "Invalid callback data" });
        }

        var command = new HandlePaymentCallbackCommand
        {
            CallbackData = webhookData,
            PaymentGatewayCode = "PAYOS"
        };

        try
        {
            var result = await mediator.Send(command);
            return Results.Ok(new { code = "00", desc = "success", message = result.Message });
            // if (result.Status == StatusCodes.Status200OK)
            // {
            //     logger.Information("✅ PayOS callback processed successfully");
            //     // PayOS expects this specific format
            //     return Results.Ok(new { code = "00", desc = "success" });
            // }
            //
            // logger.Warning("⚠️ Callback processing returned non-200 status: {Status}", result.Status);
            // return Results.BadRequest(new { code = "01", desc = result.Message ?? "Processing failed" });
        }
        catch (Exception ex)
        {
            logger.Error(ex, "❌ Error processing PayOS callback");
            return Results.BadRequest(new { code = "01", desc = "Internal error" });
        }
    }

    /// <summary>
    /// PayOS Return URL - User is redirected here after completing/canceling payment
    /// This is for UI flow - actual payment verification happens in CallBackPayOs
    /// </summary>
    public static Task<IResult> ReturnPayOs(
        HttpContext httpContext,
        ILogger logger,
        [FromQuery] string? code = null,
        [FromQuery] string? id = null,
        [FromQuery] string? orderCode = null,
        [FromQuery] string? cancel = null)
    {
        logger.Information(
            "🔄 PayOS Return - code={Code}, id={Id}, orderCode={OrderCode}, cancel={Cancel}",
            code, id, orderCode, cancel);

        try
        {
            // Payment successful
            if (code == "00")
            {
                logger.Information("✅ User returned from successful payment");
                return Task.FromResult(Results.Redirect($"/payment/success?orderCode={orderCode}"));
            }

            // User cancelled payment
            if (cancel == "true")
            {
                logger.Information("⚠️ User cancelled payment");
                return Task.FromResult(Results.Redirect($"/payment/cancelled?orderCode={orderCode}"));
            }

            // Payment failed
            logger.Warning("❌ User returned from failed payment - code={Code}", code);
            return Task.FromResult(Results.Redirect($"/payment/failed?code={code}&orderCode={orderCode}"));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error handling PayOS return");
            return Task.FromResult(Results.Redirect("/payment/error"));
        }
    }

    /// <summary>
    /// Get payment status by order ID - For frontend polling
    /// GET api/payments/status/{orderId}
    /// </summary>
    public static async Task<IResult> GetPaymentStatus(
        IMediator mediator,
        [FromRoute] Guid orderId,
        [FromQuery] string timeZone = null)
    {
        var query = new GetPaymentStatusQuery
        {
            OrderId = orderId,
            TimeZone = timeZone
        };

        var result = await mediator.Send(query);
        return Results.Json(result, statusCode: result.Status);
    }

    /// <summary>
    /// Cancel payment and order
    /// POST api/payments/cancel/{orderId}
    /// </summary>
    public static async Task<IResult> CancelPayment(
        IMediator mediator,
        [FromRoute] Guid orderId,
        [FromBody] CancelPaymentRequest? request)
    {
        var command = new CancelPaymentCommand
        {
            OrderId = orderId,
            CancelReason = request?.CancelReason
        };

        var result = await mediator.Send(command);
        return Results.Json(result, statusCode: result.Status);
    }
}

public class CancelPaymentRequest
{
    public string? CancelReason { get; set; }
}