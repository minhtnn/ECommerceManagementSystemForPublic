
using System.Net;
using ECommerceManagementSystem.Coffee.Application.Common.Exceptions;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

namespace ECommerceManagementSystem.Coffee.Application.Common.Middlewares;

public class GlobalException
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    public GlobalException(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
        {
            await HandleBadRequestException(context, ex);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private static Task HandleValidationException(HttpContext context, ValidationException ex)
    {
        var errors = ex.Errors.Select(error => new
            {
                PropertyName = error.Key, 
                ErrorMessage = error.Value
            })
            .DistinctBy(error => error.PropertyName)
            .ToArray();
    
        int statusCode = (int)HttpStatusCode.BadRequest;
        var errorResponse = new ApiResponse()
        {
            Status = (int) HttpStatusCode.BadRequest,
            Message = "Lỗi kiểm tra dữ liệu",
            Data = errors
        };
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        return context.Response.WriteAsync(JsonUtil.JsonString(errorResponse));
    }
    
    private static Task HandleBadRequestException(HttpContext context, BadHttpRequestException ex)
    {
        var statusCode = HttpStatusCode.BadRequest;
        var errorResponse = new ApiResponse()
        {
            Status = (int) statusCode,
            Message = "Lỗi kiểm tra dữ liệu",
            Data = ex.Message,
        };
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        return context.Response.WriteAsync(JsonUtil.JsonString(errorResponse));
    }

    private static Task HandleException(HttpContext context, Exception ex)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var errorResponse = new ApiResponse()
        {
            Status = (int) statusCode,
            Message = "Một lỗi không mong muốn đã xảy ra",
            Data = ex.Message,
        };
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        return context.Response.WriteAsync(JsonUtil.JsonString(errorResponse));
    }
}