using DailyMealPlan.Domain.Models;

namespace DailyMealPlan.Data.Repositories;

public interface IDailyMealPlanRepository
{
    Task<MealPlan> CreateAsync(MealPlan plan, CancellationToken cancellationToken = default);

    Task<MealPlan?> GetByIdAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlan>> ListAsync(
        string issuer,
        string subject,
        DateOnly? date = null,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        MealPlan plan,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);
}