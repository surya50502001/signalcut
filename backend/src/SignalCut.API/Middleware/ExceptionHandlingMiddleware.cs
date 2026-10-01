using System.Net;
using System.Text.Json;
using SignalCut.Application.Common;

namespace SignalCut.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object responseObj;
        int statusCode;

        if (exception is InsufficientCreditsException ice)
        {
            statusCode = (int)HttpStatusCode.PaymentRequired;
            responseObj = new
            {
                success = false,
                error = new
                {
                    code = ice.Code,
                    message = ice.Message,
                    requiredCredits = ice.RequiredCredits,
                    availableCredits = ice.AvailableCredits
                }
            };
        }
        else if (exception is UnauthorizedMediaException ume)
        {
            statusCode = (int)HttpStatusCode.Forbidden;
            responseObj = new
            {
                success = false,
                error = new
                {
                    code = ume.Code,
                    message = ume.Message,
                    sourceId = ume.SourceId
                }
            };
        }
        else if (exception is ValidationException ve)
        {
            statusCode = (int)HttpStatusCode.UnprocessableEntity;
            responseObj = new
            {
                success = false,
                error = new
                {
                    code = ve.Code,
                    message = ve.Message,
                    validationErrors = ve.Errors
                }
            };
        }
        else if (exception is SignalCutException sce)
        {
            statusCode = sce.StatusCode;
            responseObj = new
            {
                success = false,
                error = new
                {
                    code = sce.Code,
                    message = sce.Message
                }
            };
        }
        else
        {
            _logger.LogError(exception, "Unhandled system exception occurred: {Message}", exception.Message);
            statusCode = (int)HttpStatusCode.InternalServerError;
            // Never expose stack traces or internal errors to users in production
            responseObj = new
            {
                success = false,
                error = new
                {
                    code = "INTERNAL_SERVER_ERROR",
                    message = "An unexpected error occurred. Please try again or contact support."
                }
            };
        }

        context.Response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(responseObj);
        await context.Response.WriteAsync(json);
    }
}
