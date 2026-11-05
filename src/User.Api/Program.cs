using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using User.Api.Services;
using User.Data;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required.");
        var audience = builder.Configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is required.");

        builder.Services.AddDbContext<UserDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("User")));
        builder.Services.AddScoped<UserProfileRepository>();
        builder.Services.AddScoped<UserProfileService>();
        builder.Services.AddControllers();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !(builder.Environment.IsDevelopment()
                    && Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri)
                    && authorityUri.Host is "localhost" or "127.0.0.1" or "::1");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidateIssuer = true,
                    ValidIssuer = authority.TrimEnd('/'),
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Principal?.FindFirst("iss")?.Value)
                            || string.IsNullOrWhiteSpace(context.Principal.FindFirst("sub")?.Value))
                        {
                            context.Fail("The token must contain iss and sub claims.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("users.read", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireAssertion(context => HasScope(context.User, "users.read")));
            options.AddPolicy("users.write", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireAssertion(context => HasScope(context.User, "users.write")));
        });

        var clientOrigin = builder.Environment.IsDevelopment()
            ? "http://localhost:5173"
            : builder.Configuration["Cors:ClientOrigin"]
                ?? throw new InvalidOperationException("Cors:ClientOrigin is required outside Development.");
        builder.Services.AddCors(options => options.AddPolicy("Client", policy =>
            policy.WithOrigins(clientOrigin).AllowAnyHeader().AllowAnyMethod()));

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<UserDbContext>().Database.Migrate();
        }

        app.UseCors("Client");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapControllers();
        app.Run();
    }

    private static bool HasScope(ClaimsPrincipal user, string requiredScope) =>
        user.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
}