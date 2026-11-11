using System;
using Ingredient.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ingredient.Data.Migrations;

[DbContext(typeof(IngredientDbContext))]
[Migration("20261111000100_IngredientEnhancements")]
public sealed class IngredientEnhancements : Migration
{
    protected override void Up(MigrationBuilder b)
    {
        b.AddColumn<string>("StorageMethod", "Ingredients", type: "TEXT", maxLength: 30,
            nullable: false, defaultValue: "RoomTemperature");
        b.AddColumn<string>("BatchNumber", "Ingredients", type: "TEXT", maxLength: 100, nullable: true);
        b.AddColumn<int>("AlertThresholdDays", "Ingredients", type: "INTEGER", nullable: false, defaultValue: 7);
        b.CreateTable("NutritionalInfos", columns: t => new
        {
            IngredientId = t.Column<int>(type: "INTEGER", nullable: false),
            ProteinGrams = t.Column<decimal>(type: "TEXT", nullable: false),
            CarbohydratesGrams = t.Column<decimal>(type: "TEXT", nullable: false),
            FatGrams = t.Column<decimal>(type: "TEXT", nullable: false),
            FiberGrams = t.Column<decimal>(type: "TEXT", nullable: false),
            SugarGrams = t.Column<decimal>(type: "TEXT", nullable: false),
            SodiumMilligrams = t.Column<decimal>(type: "TEXT", nullable: false),
            CholesterolMilligrams = t.Column<decimal>(type: "TEXT", nullable: false),
            VitaminsMineralsJson = t.Column<string>(type: "TEXT", nullable: false),
            ServingSize = t.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            DataSource = t.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
            SourceDate = t.Column<DateTime>(type: "TEXT", nullable: true)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_NutritionalInfos", x => x.IngredientId);
            t.ForeignKey("FK_NutritionalInfos_Ingredients_IngredientId", x => x.IngredientId,
                "Ingredients", "Id", onDelete: ReferentialAction.Cascade);
        });
        b.CreateTable("Allergens", columns: t => new
        {
            Id = t.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            Name = t.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
        }, constraints: t => t.PrimaryKey("PK_Allergens", x => x.Id));
        b.CreateIndex("IX_Allergens_Name", "Allergens", "Name", unique: true);
        b.CreateTable("IngredientAllergens", columns: t => new
        {
            IngredientId = t.Column<int>(type: "INTEGER", nullable: false),
            AllergenId = t.Column<int>(type: "INTEGER", nullable: false),
            Presence = t.Column<int>(type: "INTEGER", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_IngredientAllergens", x => new { x.IngredientId, x.AllergenId });
            t.ForeignKey("FK_IngredientAllergens_Ingredients_IngredientId", x => x.IngredientId,
                "Ingredients", "Id", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("FK_IngredientAllergens_Allergens_AllergenId", x => x.AllergenId,
                "Allergens", "Id", onDelete: ReferentialAction.Restrict);
        });
        b.CreateIndex("IX_IngredientAllergens_AllergenId", "IngredientAllergens", "AllergenId");
    }

    protected override void Down(MigrationBuilder b)
    {
        b.DropTable("IngredientAllergens");
        b.DropTable("Allergens");
        b.DropTable("NutritionalInfos");
        b.DropColumn("BatchNumber", "Ingredients");
        b.DropColumn("StorageMethod", "Ingredients");
        b.DropColumn("AlertThresholdDays", "Ingredients");
    }
}
