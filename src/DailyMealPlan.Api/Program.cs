using System.Security.Claims;
using DailyMealPlan.Api.Clients;
using DailyMealPlan.Api.Services;
using DailyMealPlan.Data;
using DailyMealPlan.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var authority = builder.Configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required.");
        var audience = builder.Configuration["Authentication:Audience"]
            ?? "ctrleat-mealplans";

        builder.Services.AddDbContext<DailyMealPlanDbContext>(options =>
            options.UseSqlite(
                builder.Configuration.GetConnectionString("DailyMealPlan")
                ?? "Data Source=daily-meal-plans.db"));
        builder.Services.AddScoped<IDailyMealPlanRepository, EfDailyMealPlanRepository>();
        builder.Services.AddScoped<DailyMealPlanService>();

        var lookupUrl = builder.Configuration["LookupServices:BaseUrl"]
            ?? "http://localhost:5161";
        var recipeUrl = builder.Configuration["LookupServices:RecipeBaseUrl"] ?? lookupUrl;
        var ingredientUrl = builder.Configuration["LookupServices:IngredientBaseUrl"] ?? lookupUrl;
        builder.Services.AddHttpClient<RecipeLookupClient>(client =>
            client.BaseAddress = new Uri(recipeUrl.TrimEnd('/') + "/")).AddHttpMessageHandler<DownstreamBearerHandler>();
        builder.Services.AddHttpClient<IngredientLookupClient>(client =>
            client.BaseAddress = new Uri(ingredientUrl.TrimEnd('/') + "/")).AddHttpMessageHandler<DownstreamBearerHandler>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<DownstreamBearerHandler>();
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
            options.AddPolicy("daily-meal-plans.read", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasIdentity(context.User))
                .RequireAssertion(context => HasScope(context.User, "daily-meal-plans.read")));
            options.AddPolicy("daily-meal-plans.write", policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasIdentity(context.User))
                .RequireAssertion(context => HasScope(context.User, "daily-meal-plans.write")));
        });

        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider
                .GetRequiredService<DailyMealPlanDbContext>()
                .Database.Migrate();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapControllers();
        app.Run();
    }

    private static bool HasIdentity(ClaimsPrincipal user) =>
        !string.IsNullOrWhiteSpace(user.FindFirst("iss")?.Value)
        && !string.IsNullOrWhiteSpace(user.FindFirst("sub")?.Value);

    private static bool HasScope(ClaimsPrincipal user, string requiredScope) =>
        user.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
}