using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using bcnofficechallengeapi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var sqlConnectionString = builder.Configuration["AZURE_SQL_CONNECTIONSTRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing Azure SQL connection string.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Requisitos de autorización:
// - Scope delegado (flujo con usuario)
// - Role de aplicación (client_credentials sin usuario)
var requiredReadScope = builder.Configuration["Auth:RequiredReadScope"] ?? "access_as_user";
var requiredReadRole = builder.Configuration["Auth:RequiredReadRole"] ?? "Api.Access";

// Autenticación con Microsoft Entra ID (API) + Cookie (Backoffice)
var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
authBuilder.AddCookie("BackofficeCookie", options =>
{
    options.LoginPath = "/Backoffice/Login";
    options.Cookie.Name = "BackofficeAuth";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Autorización por scope O role
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiRead", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
              {
                  // 1) Scope claims (delegated tokens)
                  var scopeClaim = context.User.FindFirst("scp")?.Value
                      ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value
                      ?? context.User.FindFirst("scope")?.Value;

                  var hasScope = !string.IsNullOrWhiteSpace(scopeClaim) &&
                      scopeClaim
                          .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Contains(requiredReadScope, StringComparer.OrdinalIgnoreCase);

                  // 2) Role claims (application tokens)
                  var roleValues = context.User.FindAll("roles").Select(c => c.Value)
                      .Concat(context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

                  var hasRole = roleValues.Contains(requiredReadRole, StringComparer.OrdinalIgnoreCase);

                  return hasScope || hasRole;
              }));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Codemotion API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Escribe tu token JWT. Ejemplo: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed. The application will continue without applying migrations.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Codemotion API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
