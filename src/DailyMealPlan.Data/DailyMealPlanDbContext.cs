using DailyMealPlan.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyMealPlan.Data;

public sealed class DailyMealPlanDbContext(DbContextOptions<DailyMealPlanDbContext> options) : DbContext(options)
{
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<PlanSnack> PlanSnacks => Set<PlanSnack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealPlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Issuer).IsRequired();
            entity.Property(plan => plan.Subject).IsRequired();
            entity.Property(plan => plan.Date).IsRequired();
            entity.Property(plan => plan.TotalCalories).HasPrecision(18, 6);
            entity.Property(plan => plan.CreatedAt).IsRequired();
            entity.Property(plan => plan.UpdatedAt).IsRequired();

            entity.HasIndex(plan => new { plan.Issuer, plan.Subject, plan.Date })
                .IsUnique(false);

            entity.HasMany(plan => plan.Snacks)
                .WithOne()
                .HasForeignKey("MealPlanId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanSnack>(entity =>
        {
            entity.HasKey(snack => snack.Id);
            entity.Property(snack => snack.Name);
            entity.Property(snack => snack.Quantity).HasPrecision(18, 6).IsRequired();
            entity.Property(snack => snack.Unit);
            entity.Property(snack => snack.Calories).HasPrecision(18, 6).IsRequired();
        });
    }
}