using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyMealPlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateDailyMealPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BreakfastRecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    LunchRecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    DinnerRecipeId = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalCalories = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanSnacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsRecipe = table.Column<bool>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Calories = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    MealPlanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSnacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanSnacks_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_Issuer_Subject_Date",
                table: "MealPlans",
                columns: new[] { "Issuer", "Subject", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanSnacks_MealPlanId",
                table: "PlanSnacks",
                column: "MealPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanSnacks");

            migrationBuilder.DropTable(
                name: "MealPlans");
        }
    }
}
