using DailyMealPlan.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyMealPlan.Data.Repositories;

public sealed class EfDailyMealPlanRepository(DailyMealPlanDbContext dbContext) : IDailyMealPlanRepository
{
    public async Task<MealPlan> CreateAsync(
        MealPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.MealPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return plan;
    }

    public Task<MealPlan?> GetByIdAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        return PlansForOwner(issuer, subject)
            .AsNoTracking()
            .Include(plan => plan.Snacks)
            .SingleOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MealPlan>> ListAsync(
        string issuer,
        string subject,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = PlansForOwner(issuer, subject)
            .Include(plan => plan.Snacks)
            .AsNoTracking();

        if (date.HasValue)
        {
            query = query.Where(plan => plan.Date == date.Value);
        }

        return await query
            .OrderBy(plan => plan.Date)
            .ThenBy(plan => plan.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        MealPlan plan,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await PlansForOwner(issuer, subject)
            .Include(current => current.Snacks)
            .SingleOrDefaultAsync(current => current.Id == plan.Id, cancellationToken);

        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        existing.Date = plan.Date;
        existing.BreakfastRecipeId = plan.BreakfastRecipeId;
        existing.LunchRecipeId = plan.LunchRecipeId;
        existing.DinnerRecipeId = plan.DinnerRecipeId;
        dbContext.Entry(existing)
            .Property(current => current.TotalCalories)
            .CurrentValue = plan.TotalCalories;
        existing.UpdatedAt = plan.UpdatedAt;

        dbContext.PlanSnacks.RemoveRange(existing.Snacks);
        existing.Snacks.Clear();
        foreach (var snack in plan.Snacks)
        {
            existing.Snacks.Add(new PlanSnack
            {
                IsRecipe = snack.IsRecipe,
                ItemId = snack.ItemId,
                Name = snack.Name,
                Quantity = snack.Quantity,
                Unit = snack.Unit,
                Calories = snack.Calories
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await PlansForOwner(issuer, subject)
            .SingleOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (plan is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.MealPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private IQueryable<MealPlan> PlansForOwner(string issuer, string subject)
    {
        return dbContext.MealPlans
            .Where(plan => plan.Issuer == issuer && plan.Subject == subject);
    }
}