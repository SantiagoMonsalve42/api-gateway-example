using System.Collections.Concurrent;

namespace ApiGateway.Middleware
{
    public class CircuitBreakerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CircuitBreakerMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, CircuitBreakerState> _states = new();

        public CircuitBreakerMiddleware(RequestDelegate next, ILogger<CircuitBreakerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var protectedRoutes = new[] { "/v1/orders" };
            var isProtected = protectedRoutes.Any(route => context.Request.Path.StartsWithSegments(route));

            if (isProtected)
            {
                var routeKey = "/orders";
                var state = _states.GetOrAdd(routeKey, _ => new CircuitBreakerState());

                // Verificar si el circuito está abierto
                if (state.IsOpen)
                {
                    _logger.LogWarning($"Circuit breaker ABIERTO - Rechazando petición a {routeKey}");
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new { error = "Service temporarily unavailable" });
                    return;
                }

                // Interceptar la respuesta
                var originalBodyStream = context.Response.Body;
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                await _next(context);

                // Verificar status code
                var statusCode = context.Response.StatusCode;
                if (statusCode >= 500)
                {
                    state.RecordFailure();
                    _logger.LogWarning($"HTTP {statusCode} para {routeKey} - Fallos: {state.FailureCount}/3");

                    if (state.IsOpen)
                    {
                        _logger.LogError($"Circuit breaker ABIERTO para {routeKey} (próximo intento en 10s)");
                    }
                }
                else
                {
                    state.Reset();
                    _logger.LogInformation($"HTTP {statusCode} - Circuit breaker reseteo");
                }

                // Copiar respuesta (resetear position primero)
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
            else
            {
                await _next(context);
            }
        }
    }

    public class CircuitBreakerState
    {
        private int _failureCount = 0;
        private DateTime? _openedAt;
        private readonly object _lock = new object();

        public int FailureCount => _failureCount;

        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    if (_openedAt == null) return false;
                    if (DateTime.UtcNow - _openedAt >= TimeSpan.FromSeconds(10))
                    {
                        _openedAt = null;
                        _failureCount = 0;
                        return false;
                    }
                    return true;
                }
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                if (_failureCount >= 3)
                {
                    _openedAt = DateTime.UtcNow;
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _openedAt = null;
            }
        }
    }
}
