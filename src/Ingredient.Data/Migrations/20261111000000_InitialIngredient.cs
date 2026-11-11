using System;
using Ingredient.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ingredient.Data.Migrations;

[DbContext(typeof(IngredientDbContext))]
[Migration("20261111000000_InitialIngredient")]
public sealed class InitialIngredient : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Ingredients",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                FoodGroup = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                Calories = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Ingredients", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Ingredients_Name", table: "Ingredients", column: "Name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Ingredients");
    }
}
