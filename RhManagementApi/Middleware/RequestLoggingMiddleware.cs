
using System.Diagnostics;
using System.Text;

namespace RhManagementApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            var traceId = context.TraceIdentifier;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userName = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity!.Name : null;

            string? requestBody = null;
            const int bodyMaxChars = 4000;
            if (ShouldLogBody(method, path))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (requestBody.Length > bodyMaxChars)
                {
                    requestBody = requestBody[..bodyMaxChars] + $"... (truncated at {bodyMaxChars} chars)";
                }
            }

            long? responseLength = null;
            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state!;
                responseLength = httpContext.Response.ContentLength;
                return Task.CompletedTask;
            }, context);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request failed {Method} {Path}{Query} | Status={StatusCode} CorrId={CorrelationId} TraceId={TraceId} User={User} ClientIP={ClientIP}",
                    method, path, query, context.Response.StatusCode);
                throw;
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "HTTP {Method} {Path}{Query} => {StatusCode} in {DurationMs} ms | CorrId={CorrelationId} TraceId={TraceId} User={User} ClientIP={ClientIP} RespLen={ResponseLength} BodyLogged={BodyLogged}",
                    method, path, query, statusCode, sw.ElapsedMilliseconds, responseLength, requestBody is not null);

                if (requestBody is not null)
                {
                    _logger.LogDebug("RequestBody {Method} {Path}: {Body}", method, path, requestBody);
                }
            }
        }

        private static bool ShouldLogBody(string method, string path)
        {
            // Example: allowlist POST/PUT on a subset
            if (method is "POST" or "PUT")
            {
                if (path.StartsWith("/api/orders", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (path.StartsWith("/api/customers", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

}
