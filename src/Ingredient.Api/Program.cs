using System.Security.Claims;
using Ingredient.Api.Contracts;
using Ingredient.Api.Services;
using Ingredient.Data;
using Ingredient.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Missing Authentication:Authority");
var audience = builder.Configuration["Authentication:Audience"] ?? "ctrleat-ingredients";
builder.Services.AddDbContext<IngredientDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Ingredient") ?? "Data Source=ingredients.db"));
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IngredientApplicationService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = issuer;
    options.Audience = audience;
    options.MapInboundClaims = false;
    options.RequireHttpsMetadata = !(builder.Environment.IsDevelopment() && new Uri(issuer).IsLoopback);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer.TrimEnd('/'),
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, ValidateLifetime = true,
        RequireSignedTokens = true, ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ingredients.read", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(c => HasScope(c.User, "ingredients.read")));
    options.AddPolicy("ingredients.write", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(c => HasScope(c.User, "ingredients.write")));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IngredientDbContext>().Database.Migrate();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
var api = app.MapGroup("/api/Ingredients");
api.MapGet("/", async (IngredientApplicationService service, string? name, string? foodGroup, string? allergen, CancellationToken ct) =>
    Results.Ok(await service.SearchAsync(name, foodGroup, allergen, ct))).RequireAuthorization("ingredients.read");
api.MapGet("/alerts", async (IngredientApplicationService service, CancellationToken ct) =>
    Results.Ok(await service.AlertsAsync(ct))).RequireAuthorization("ingredients.read");
api.MapGet("/{id:int}", async (int id, IngredientApplicationService service, CancellationToken ct) =>
    await service.GetAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
    .RequireAuthorization("ingredients.read");
api.MapPost("/", async (IngredientInput input, IngredientApplicationService service, CancellationToken ct) =>
{
    try
    {
        var result = await service.CreateAsync(input, ct);
        return Results.Created($"/api/Ingredients/{result.Id}", result);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization("ingredients.write");
api.MapPut("/{id:int}", async (int id, IngredientInput input, IngredientApplicationService service, CancellationToken ct) =>
{
    try
    {
        var result = await service.UpdateAsync(id, input, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization("ingredients.write");
api.MapDelete("/{id:int}", async (int id, IngredientApplicationService service, CancellationToken ct) =>
    await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization("ingredients.write");
app.Run();

static bool HasScope(ClaimsPrincipal user, string scope) => user.Claims
    .Where(c => c.Type is "scope" or "scp")
    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    .Contains(scope, StringComparer.Ordinal);

public partial class Program { }
