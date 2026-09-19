using System.Net;
using System.Text.Json;
using TaskManagement.Api.Interfaces;

namespace TaskManagement.Api.Middleware
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public IDictionary<string, string[]>? Errors { get; set; }
    }

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
            catch (ApiException apiEx)
            {
                // Expected, "business" errors (409 conflict, 400 validation, etc.) - safe to return message as-is.
                _logger.LogWarning(apiEx, "Handled API exception: {Message}", apiEx.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = apiEx.StatusCode;
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    new ErrorResponse { Message = apiEx.Message }));
            }
            catch (Exception ex)
            {
                // Unexpected failure - log full detail server-side, never leak it to the client.
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(JsonSerializer.Serialize(
                    new ErrorResponse { Message = "An unexpected error occurred. Please try again later." }));
            }
        }
    }
}
