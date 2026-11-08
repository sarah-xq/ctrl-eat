using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StorageLocation.Api.Clients;
using StorageLocation.Api.Services;
using StorageLocation.Data;
using StorageLocation.Data.Repositories;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required.");
        var audience = builder.Configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is required.");
        var connectionString = builder.Configuration.GetConnectionString("StorageLocation")
            ?? throw new InvalidOperationException("ConnectionStrings:StorageLocation is required.");
        var ingredientApiBaseUrl = builder.Configuration["IngredientApi:BaseUrl"];
        if (!Uri.TryCreate(ingredientApiBaseUrl, UriKind.Absolute, out var ingredientApiUri)
            || ingredientApiUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("IngredientApi:BaseUrl must be configured as an absolute HTTP or HTTPS URL.");
        }

        builder.Services.AddDbContext<StorageLocationDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
        builder.Services.AddScoped<StorageLocationService>();
        var ingredientClient = builder.Services.AddHttpClient<HttpIngredientClient>(client =>
        {
            client.BaseAddress = ingredientApiUri;
        });
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            ingredientClient.ConfigurePrimaryHttpMessageHandler(() => new DevelopmentIngredientHandler());
        }

        builder.Services.AddTransient<IIngredientClient>(services => services.GetRequiredService<HttpIngredientClient>());
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
                    AuthenticationType = JwtBearerDefaults.AuthenticationScheme,
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
            options.AddPolicy("storage-locations.read", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireAssertion(context => HasScope(context.User, "storage-locations.read")));
            options.AddPolicy("storage-locations.write", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireAssertion(context => HasScope(context.User, "storage-locations.write")));
        });

        var app = builder.Build();
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<StorageLocationDbContext>().Database.Migrate();
        }

        app.MapControllers();
        app.Run();
    }

    private static bool HasScope(ClaimsPrincipal user, string requiredScope) =>
        user.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
}
