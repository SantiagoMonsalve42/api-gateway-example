using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Configurar JWT
var key = Encoding.ASCII.GetBytes("tu-clave-secreta-super-segura-de-256-bits-minimo");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Sin tolerancia para expiración
    };
    
    // Events para loguear errores de validación
    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Validation Failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Valid for: {context.Principal?.FindFirst("client_id")?.Value}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOcelot()
    .AddPolly();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Middleware de Circuit Breaker ANTES de JWT y Ocelot
app.UseMiddleware<CircuitBreakerMiddleware>();

// Middleware para validar JWT en rutas protegidas ANTES de Ocelot
app.Use(async (context, next) =>
{
    var protectedRoutes = new[] { "/v1/sales" };
    var isProtectedRoute = protectedRoutes.Any(route => context.Request.Path.StartsWithSegments(route));

    if (isProtectedRoute)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Authorization header requerido" });
            return;
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Bearer token requerido" });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            
            // Token válido, asignar a context.User
            context.User = principal;
            Console.WriteLine($"✓ JWT válido para cliente: {principal.FindFirst("client_id")?.Value}");
        }
        catch (SecurityTokenExpiredException ex)
        {
            context.Response.StatusCode = 401;
            Console.WriteLine($"✗ Token expirado: {ex.Message}");
            await context.Response.WriteAsJsonAsync(new { message = "Token expirado" });
            return;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            context.Response.StatusCode = 401;
            Console.WriteLine($"✗ Firma inválida: {ex.Message}");
            await context.Response.WriteAsJsonAsync(new { message = "Token con firma inválida" });
            return;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 401;
            Console.WriteLine($"✗ Error validating token: {ex.Message}");
            await context.Response.WriteAsJsonAsync(new { message = $"Token inválido: {ex.Message}" });
            return;
        }
    }

    await next();
});

// Middleware para el endpoint /auth/token (sin protección)
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/auth/token" && context.Request.Method == "POST")
    {
        try
        {
            var clientId = "user-demo";
            if (string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { message = "ClientId es requerido" });
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", clientId),
                    new Claim("client_id", clientId)
                }),
                Expires = DateTime.UtcNow.AddMinutes(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                token = tokenString,
                expiresIn = 300,
                tokenType = "Bearer"
            });
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { message = $"Error: {ex.Message}" });
        }
        return;
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.Run();