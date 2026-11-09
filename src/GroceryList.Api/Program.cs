using System.Security.Claims;
using GroceryList.Api.Clients;
using GroceryList.Api.Services;
using GroceryList.Data;
using GroceryList.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GroceryList")
    ?? throw new InvalidOperationException("ConnectionStrings:GroceryList is required.");
var ingredientApiBaseUrl = builder.Configuration["IngredientApi:BaseUrl"];
if (!Uri.TryCreate(ingredientApiBaseUrl, UriKind.Absolute, out var ingredientApiUri)
    || ingredientApiUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException("IngredientApi:BaseUrl must be configured as an absolute HTTP or HTTPS URL.");
}
var authority = builder.Configuration["Authentication:Authority"]
    ?? throw new InvalidOperationException("Authentication:Authority is required.");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication:Audience is required.");

builder.Services.AddDbContext<GroceryListDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IGroceryListRepository, GroceryListRepository>();
builder.Services.AddScoped<GroceryListService>();
var ingredientHttpClient = builder.Services.AddHttpClient<HttpIngredientClient>(client =>
{
    client.BaseAddress = ingredientApiUri;
});
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    ingredientHttpClient.ConfigurePrimaryHttpMessageHandler(() => new DevelopmentIngredientHandler());
}

builder.Services.AddTransient<IIngredientClient>(services => services.GetRequiredService<HttpIngredientClient>());
builder.Services.AddExceptionHandler<GroceryListExceptionHandler>();
builder.Services.AddProblemDetails();
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
    options.AddPolicy("grocery-lists.read", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
        .RequireAssertion(context => HasScope(context.User, "grocery-lists.read")));
    options.AddPolicy("grocery-lists.write", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
        .RequireAssertion(context => HasScope(context.User, "grocery-lists.write")));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GroceryListDbContext>().Database.Migrate();
}

app.MapControllers();
static bool HasScope(ClaimsPrincipal user, string requiredScope) =>
    user.Claims
        .Where(claim => claim.Type is "scope" or "scp")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Contains(requiredScope, StringComparer.Ordinal);

app.Run();

public partial class Program;
