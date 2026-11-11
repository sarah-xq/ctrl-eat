using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Recipe.Data;
using Recipe.Domain;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Missing Authentication:Authority");
var audience = builder.Configuration["Authentication:Audience"] ?? "ctrleat-recipes";
builder.Services.AddDbContext<RecipeDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Recipe") ?? "Data Source=recipes.db"));
builder.Services.AddHttpClient("IngredientApi", c => c.BaseAddress = new Uri(builder.Configuration["IngredientApi:BaseUrl"] ?? "http://localhost:5161/"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = issuer; o.Audience = audience; o.MapInboundClaims = false;
    o.RequireHttpsMetadata = !(builder.Environment.IsDevelopment() && new Uri(issuer).IsLoopback);
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer.TrimEnd('/'), ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, ValidateLifetime = true, RequireSignedTokens = true, ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("recipes.read", p => p.RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User, "recipes.read")));
    o.AddPolicy("recipes.write", p => p.RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User, "recipes.write")));
});
var app = builder.Build();
using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<RecipeDbContext>().Database.Migrate();
app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
var api = app.MapGroup("/api/recipes");
api.MapGet("/", async (RecipeDbContext db, string? name, string? author) =>
{
    var q = db.Recipes.Include(r => r.Ingredients).AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(name)) q = q.Where(r => r.Name.Contains(name));
    if (!string.IsNullOrWhiteSpace(author)) q = q.Where(r => r.Author == author);
    return Results.Ok(await q.OrderBy(r => r.Id).ToListAsync());
}).RequireAuthorization("recipes.read");
api.MapGet("/{id:int}", async (int id, RecipeDbContext db) =>
    await db.Recipes.Include(x => x.Ingredients).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } r ? Results.Ok(r) : Results.NotFound())
    .RequireAuthorization("recipes.read");
api.MapPost("/", async (RecipeInput input, RecipeDbContext db, IHttpClientFactory factory, HttpContext context, CancellationToken ct) =>
{
    var result = await MakeRecipeAsync(input, factory, context, ct);
    if (result.error is not null) return result.error;
    var r = result.recipe!; db.Recipes.Add(r); await db.SaveChangesAsync(ct);
    return Results.Created($"/api/recipes/{r.Id}", r);
}).RequireAuthorization("recipes.write");
api.MapPut("/{id:int}", async (int id, RecipeInput input, RecipeDbContext db, IHttpClientFactory factory, HttpContext context, CancellationToken ct) =>
{
    var existing = await db.Recipes.Include(x => x.Ingredients).FirstOrDefaultAsync(x => x.Id == id, ct);
    if (existing is null) return Results.NotFound();
    var result = await MakeRecipeAsync(input, factory, context, ct);
    if (result.error is not null) return result.error;
    var updated = result.recipe!;
    existing.Name = updated.Name; existing.Author = updated.Author; existing.TotalTimeMinutes = updated.TotalTimeMinutes;
    existing.CookTimeMinutes = updated.CookTimeMinutes; existing.Description = updated.Description; existing.TotalCalories = updated.TotalCalories;
    db.RecipeIngredients.RemoveRange(existing.Ingredients); existing.Ingredients = updated.Ingredients;
    await db.SaveChangesAsync(ct); return Results.Ok(existing);
}).RequireAuthorization("recipes.write");
api.MapDelete("/{id:int}", async (int id, RecipeDbContext db) =>
{
    var r = await db.Recipes.FindAsync(id); if (r is null) return Results.NotFound();
    db.Remove(r); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization("recipes.write");
app.Run();

static async Task<(Recipe.Domain.Recipe? recipe, IResult? error)> MakeRecipeAsync(
    RecipeInput input, IHttpClientFactory factory, HttpContext context, CancellationToken ct)
{
    try { Recipe.Domain.Recipe.Validate(input.Name, input.TotalTimeMinutes, input.CookTimeMinutes,
        (input.Ingredients ?? new()).Select(x => (x.IngredientId, x.Quantity))); }
    catch (ArgumentException e) { return (null, Results.BadRequest(new { error = e.Message })); }
    var recipe = new Recipe.Domain.Recipe {
        Name = input.Name.Trim(), Author = input.Author, Description = input.Description ?? "",
        TotalTimeMinutes = input.TotalTimeMinutes, CookTimeMinutes = input.CookTimeMinutes
    };
    // Forward the caller's bearer token only; don't use a privileged bypass for cross-service validation.
    var bearer = context.Request.Headers.Authorization.ToString();
    if (!bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return (null, Results.Unauthorized());
    var client = factory.CreateClient("IngredientApi");
    foreach (var req in input.Ingredients ?? new())
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/Ingredients/{req.IngredientId}");
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(bearer);
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, ct); }
        catch (HttpRequestException) { return (null, Results.Problem("Ingredient service unavailable.", statusCode: 503)); }
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                return (null, Results.BadRequest(new { error = $"Ingredient {req.IngredientId} not found." }));
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, Results.Problem("Ingredient lookup token was not authorized.", statusCode: 502));
            if (!response.IsSuccessStatusCode)
                return (null, Results.Problem("Ingredient lookup failed.", statusCode: 503));
            var ingredient = await response.Content.ReadFromJsonAsync<IngredientLookup>(cancellationToken: ct);
            if (ingredient is null || ingredient.Id != req.IngredientId)
                return (null, Results.Problem("Invalid Ingredient response.", statusCode: 502));
            recipe.Ingredients.Add(new RecipeIngredient { IngredientId = req.IngredientId,
                IngredientName = ingredient.Name, FoodGroup = ingredient.FoodGroup, CaloriesPerUnit = ingredient.Calories,
                Quantity = req.Quantity, Unit = req.Unit });
            recipe.TotalCalories += ingredient.Calories * req.Quantity;
        }
    }
    return (recipe, null);
}
static bool HasScope(ClaimsPrincipal user, string scope) => user.Claims
    .Where(c => c.Type is "scope" or "scp").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    .Contains(scope, StringComparer.Ordinal);
public sealed record RecipeIngredientInput(int IngredientId, decimal Quantity, string Unit);
public sealed record RecipeInput(string Name, string? Author, int TotalTimeMinutes, int CookTimeMinutes, string? Description, List<RecipeIngredientInput>? Ingredients);
public sealed record IngredientLookup(int Id, string Name, string FoodGroup, decimal Calories);
public partial class Program { }
