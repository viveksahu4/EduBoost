namespace EduBoost.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _log;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> log)
        { _next = next; _log = log; }

        public async Task InvokeAsync(HttpContext ctx)
        {
            var start = DateTime.Now;
            await _next(ctx);
            _log.LogInformation("{Method} {Path} -> {Status} in {Ms} ms",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode,
                (DateTime.Now - start).TotalMilliseconds);
        }
    }
}