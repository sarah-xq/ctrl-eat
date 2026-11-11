using System.Security.Claims;
using Ingredient.Data;
using Ingredient.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["Authentication:Authority"] ?? throw new InvalidOperationException("Missing Authentication:Authority");
var audience = builder.Configuration["Authentication:Audience"] ?? "ctrleat-ingredients";
builder.Services.AddDbContext<IngredientDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Ingredient") ?? "Data Source=ingredients.db"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = issuer;
    o.Audience = audience;
    o.MapInboundClaims = false;
    o.RequireHttpsMetadata = !(builder.Environment.IsDevelopment() && new Uri(issuer).IsLoopback);
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer.TrimEnd('/'), ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true, ValidateLifetime = true, RequireSignedTokens = true, ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("ingredients.read", p => p.RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User, "ingredients.read")));
    o.AddPolicy("ingredients.write", p => p.RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User, "ingredients.write")));
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<IngredientDbContext>().Database.Migrate();
app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
var api = app.MapGroup("/api/Ingredients");
api.MapGet("/", async (IngredientDbContext db, string? name, string? foodGroup) =>
{
    var query = db.Ingredients.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Name.Contains(name));
    if (!string.IsNullOrWhiteSpace(foodGroup)) query = query.Where(x => x.FoodGroup == foodGroup);
    return Results.Ok(await query.OrderBy(x => x.Id).ToListAsync());
}).RequireAuthorization("ingredients.read");
api.MapGet("/{id:int}", async (int id, IngredientDbContext db) =>
    await db.Ingredients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } x ? Results.Ok(x) : Results.NotFound())
    .RequireAuthorization("ingredients.read");
api.MapPost("/", async (IngredientInput input, IngredientDbContext db) =>
{
    try { Ingredient.Domain.Ingredient.Validate(input.Name, input.FoodGroup, input.Calories); }
    catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    var x = new Ingredient.Domain.Ingredient { Name = input.Name.Trim(), FoodGroup = input.FoodGroup.Trim(), Calories = input.Calories, ExpiryDate = input.ExpiryDate };
    db.Ingredients.Add(x); await db.SaveChangesAsync();
    return Results.Created($"/api/Ingredients/{x.Id}", x);
}).RequireAuthorization("ingredients.write");
api.MapPut("/{id:int}", async (int id, IngredientInput input, IngredientDbContext db) =>
{
    try { Ingredient.Domain.Ingredient.Validate(input.Name, input.FoodGroup, input.Calories); }
    catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
    var x = await db.Ingredients.FindAsync(id); if (x is null) return Results.NotFound();
    x.Name = input.Name.Trim(); x.FoodGroup = input.FoodGroup.Trim(); x.Calories = input.Calories; x.ExpiryDate = input.ExpiryDate;
    await db.SaveChangesAsync(); return Results.Ok(x);
}).RequireAuthorization("ingredients.write");
api.MapDelete("/{id:int}", async (int id, IngredientDbContext db) =>
{
    var x = await db.Ingredients.FindAsync(id); if (x is null) return Results.NotFound();
    db.Remove(x); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization("ingredients.write");
app.Run();
static bool HasScope(ClaimsPrincipal user, string scope) => user.Claims
    .Where(c => c.Type is "scope" or "scp")
    .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    .Contains(scope, StringComparer.Ordinal);
public sealed record IngredientInput(string Name, string FoodGroup, DateTime? ExpiryDate, decimal Calories);
public partial class Program { }
