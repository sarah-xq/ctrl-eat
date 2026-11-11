using System;
using Recipe.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Recipe.Data.Migrations;

[DbContext(typeof(RecipeDbContext))]
[Migration("20261111000000_InitialRecipe")]
public sealed class InitialRecipe : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Recipes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Author = table.Column<string>(type: "TEXT", nullable: true),
                TotalTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                CookTimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                TotalCalories = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Recipes", x => x.Id));
        migrationBuilder.CreateTable(
            name: "RecipeIngredients",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                IngredientName = table.Column<string>(type: "TEXT", nullable: false),
                FoodGroup = table.Column<string>(type: "TEXT", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                Unit = table.Column<string>(type: "TEXT", nullable: false),
                CaloriesPerUnit = table.Column<decimal>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                table.ForeignKey("FK_RecipeIngredients_Recipes_RecipeId", x => x.RecipeId, "Recipes", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_RecipeIngredients_RecipeId_IngredientId", table: "RecipeIngredients", columns: new[] { "RecipeId", "IngredientId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RecipeIngredients");
        migrationBuilder.DropTable(name: "Recipes");
    }
}
