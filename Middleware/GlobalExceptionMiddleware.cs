using System.Net;
using System.Text.Json;

namespace StrayCareAPI.Middleware
{
    /// <summary>
    /// 全局异常处理中间件
    /// 捕获所有未处理的异常，返回统一的 JSON 错误格式
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Map exception type to HTTP status code and error code
            var (statusCode, errorCode) = exception switch
            {
                ArgumentNullException     => (HttpStatusCode.BadRequest,            "BAD_REQUEST"),
                ArgumentException         => (HttpStatusCode.BadRequest,            "BAD_REQUEST"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized,        "UNAUTHORIZED"),
                KeyNotFoundException      => (HttpStatusCode.NotFound,              "NOT_FOUND"),
                FileNotFoundException     => (HttpStatusCode.NotFound,              "NOT_FOUND"),
                InvalidOperationException => (HttpStatusCode.Conflict,              "CONFLICT"),
                NotSupportedException     => (HttpStatusCode.BadRequest,            "NOT_SUPPORTED"),
                _                         => (HttpStatusCode.InternalServerError,   "INTERNAL_ERROR")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Build response object
            var response = new Dictionary<string, object>
            {
                { "message", GetUserFriendlyMessage(exception, statusCode) },
                { "errorCode", errorCode },
                { "traceId", context.TraceIdentifier }
            };

            // In development, include stack trace for debugging
            if (_env.IsDevelopment())
            {
                response.Add("detail", exception.ToString());
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Returns a user-friendly message.
        /// For 500 errors in production, hides the real exception message.
        /// For 4xx errors, the exception message is usually safe to show.
        /// </summary>
        private string GetUserFriendlyMessage(Exception exception, HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.InternalServerError && !_env.IsDevelopment())
            {
                return "An unexpected error occurred. Please try again later.";
            }

            return exception.Message;
        }
    }
}
