using Recipe.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Recipe.Data.Migrations;

[DbContext(typeof(RecipeDbContext))]
[Migration("20261111000100_RecipeNutritionAllergens")]
public sealed class RecipeNutritionAllergens : Migration
{
    protected override void Up(MigrationBuilder b)
    {
        foreach (var name in new[] { "ProteinGrams", "CarbohydratesGrams", "FatGrams",
            "FiberGrams", "SugarGrams", "SodiumMilligrams", "CholesterolMilligrams" })
            b.AddColumn<decimal>(name, "Recipes", type: "TEXT", nullable: false, defaultValue: 0m);
        b.AddColumn<string>("VitaminsMineralsJson", "Recipes", type: "TEXT", nullable: false, defaultValue: "{}");
        b.AddColumn<string>("AllergensJson", "Recipes", type: "TEXT", nullable: false, defaultValue: "[]");
    }
    protected override void Down(MigrationBuilder b)
    {
        foreach (var name in new[] { "ProteinGrams", "CarbohydratesGrams", "FatGrams",
            "FiberGrams", "SugarGrams", "SodiumMilligrams", "CholesterolMilligrams" })
            b.DropColumn(name, "Recipes");
        b.DropColumn("VitaminsMineralsJson", "Recipes");
        b.DropColumn("AllergensJson", "Recipes");
    }
}
