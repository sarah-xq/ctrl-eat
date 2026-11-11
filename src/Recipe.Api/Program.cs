using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Recipe.Api.Contracts;
using Recipe.Api.Services;
using Recipe.Data;
using Recipe.Domain;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Missing Authentication:Authority");
var audience = builder.Configuration["Authentication:Audience"] ?? "ctrleat-recipes";
builder.Services.AddDbContext<RecipeDbContext>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Recipe") ?? "Data Source=recipes.db"));
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<RecipeApplicationService>();
builder.Services.AddHttpClient("IngredientApi", c => c.BaseAddress = new Uri(
    builder.Configuration["IngredientApi:BaseUrl"] ?? "http://localhost:5161/"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = issuer; o.Audience = audience; o.MapInboundClaims = false;
    o.RequireHttpsMetadata = !(builder.Environment.IsDevelopment() && new Uri(issuer).IsLoopback);
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer.TrimEnd('/'),
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, ValidateLifetime = true,
        RequireSignedTokens = true, ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("recipes.read", p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => HasScope(c.User, "recipes.read")));
    o.AddPolicy("recipes.write", p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => HasScope(c.User, "recipes.write")));
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<RecipeDbContext>().Database.Migrate();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
var api = app.MapGroup("/api/recipes");
api.MapGet("/", async (RecipeApplicationService service, string? name, string? author, CancellationToken ct) =>
    Results.Ok(await service.SearchAsync(name, author, ct))).RequireAuthorization("recipes.read");
api.MapGet("/{id:int}", async (int id, RecipeApplicationService service, CancellationToken ct) =>
    await service.GetAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
    .RequireAuthorization("recipes.read");
api.MapPost("/", async (RecipeInput input, RecipeApplicationService service, HttpContext ctx, CancellationToken ct) =>
{
    var outcome = await service.CreateAsync(input, ctx.Request.Headers.Authorization.ToString(), ct);
    return ToResult(outcome);
}).RequireAuthorization("recipes.write");
api.MapPut("/{id:int}", async (int id, RecipeInput input, RecipeApplicationService service, HttpContext ctx, CancellationToken ct) =>
{
    var outcome = await service.UpdateAsync(id, input, ctx.Request.Headers.Authorization.ToString(), ct);
    return ToResult(outcome);
}).RequireAuthorization("recipes.write");
api.MapDelete("/{id:int}", async (int id, RecipeApplicationService service, CancellationToken ct) =>
    await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization("recipes.write");
app.Run();

static IResult ToResult(RecipeOutcome outcome) => outcome.Status switch
{
    200 => Results.Ok(outcome.Value),
    201 => Results.Created($"/api/recipes/{outcome.Value!.Id}", outcome.Value),
    400 => Results.BadRequest(new { error = outcome.Error }),
    401 => Results.Unauthorized(),
    404 => Results.NotFound(),
    502 or 503 => Results.Problem(outcome.Error, statusCode: outcome.Status),
    _ => Results.Problem("Unexpected outcome", statusCode: 500)
};

static bool HasScope(ClaimsPrincipal user, string scope) => user.Claims
    .Where(c => c.Type is "scope" or "scp")
    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    .Contains(scope, StringComparer.Ordinal);

public partial class Program { }
