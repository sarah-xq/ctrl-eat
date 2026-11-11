using Microsoft.EntityFrameworkCore;
using Recipe.Domain;

namespace Recipe.Data;

public sealed class RecipeRepository(RecipeDbContext db) : IRecipeRepository
{
    public async Task<IReadOnlyList<Recipe.Domain.Recipe>> SearchAsync(string? name, string? author, CancellationToken ct)
    {
        var query = db.Recipes.Include(x => x.Ingredients).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => x.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(author)) query = query.Where(x => x.Author == author);
        return await query.OrderBy(x => x.Id).ToListAsync(ct);
    }

    public Task<Recipe.Domain.Recipe?> GetAsync(int id, CancellationToken ct) =>
        db.Recipes.Include(x => x.Ingredients).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Recipe.Domain.Recipe recipe, CancellationToken ct)
    {
        await db.Recipes.AddAsync(recipe, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task DeleteAsync(Recipe.Domain.Recipe recipe, CancellationToken ct)
    {
        db.Recipes.Remove(recipe);
        return Task.CompletedTask;
    }

    public void ReplaceIngredients(Recipe.Domain.Recipe recipe, List<RecipeIngredient> ingredients)
    {
        db.RecipeIngredients.RemoveRange(recipe.Ingredients);
        recipe.Ingredients = ingredients;
    }
}
