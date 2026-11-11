using Ingredient.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ingredient.Data;

public sealed class IngredientRepository(IngredientDbContext db) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient.Domain.Ingredient>> SearchAsync(string? name, string? group, string? allergen, CancellationToken ct)
    {
        var query = db.Ingredients.Include(x => x.Nutrition).Include(x => x.Allergens)
            .ThenInclude(x => x.Allergen).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(group)) query = query.Where(x => x.FoodGroup == group);
        if (!string.IsNullOrWhiteSpace(allergen))
        {
            var normalized = allergen.Trim().ToLowerInvariant();
            query = query.Where(x => x.Allergens.Any(a => a.Allergen.Name == normalized));
        }
        return await query.OrderBy(x => x.Id).ToListAsync(ct);
    }

    public Task<Ingredient.Domain.Ingredient?> GetAsync(int id, CancellationToken ct) =>
        db.Ingredients.Include(x => x.Nutrition).Include(x => x.Allergens)
            .ThenInclude(x => x.Allergen).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Ingredient.Domain.Ingredient ingredient, CancellationToken ct)
    {
        await db.Ingredients.AddAsync(ingredient, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task DeleteAsync(Ingredient.Domain.Ingredient ingredient, CancellationToken ct)
    {
        db.Ingredients.Remove(ingredient);
        return Task.CompletedTask;
    }

    public async Task<Allergen> GetOrCreateAllergenAsync(string name, CancellationToken ct)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var pending = db.Allergens.Local.FirstOrDefault(x => x.Name == normalized);
        if (pending is not null) return pending;
        var existing = await db.Allergens.FirstOrDefaultAsync(x => x.Name == normalized, ct);
        if (existing is not null) return existing;
        var created = new Allergen { Name = normalized };
        db.Allergens.Add(created);
        return created;
    }
}
