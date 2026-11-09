using Microsoft.EntityFrameworkCore.Migrations;

namespace GroceryList.Data.Migrations;

public partial class InitialGroceryLists : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GroceryLists",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_GroceryLists", list => list.Id));

        migrationBuilder.CreateTable(
            name: "GroceryListIngredients",
            columns: table => new
            {
                GroceryListId = table.Column<int>(type: "INTEGER", nullable: false),
                IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroceryListIngredients", ingredient => new { ingredient.GroceryListId, ingredient.IngredientId });
                table.CheckConstraint("CK_GroceryListIngredients_IngredientId_Positive", "\"IngredientId\" > 0");
                table.ForeignKey(
                    name: "FK_GroceryListIngredients_GroceryLists_GroceryListId",
                    column: ingredient => ingredient.GroceryListId,
                    principalTable: "GroceryLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GroceryListIngredients_GroceryListId_Position",
            table: "GroceryListIngredients",
            columns: new[] { "GroceryListId", "Position" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GroceryListIngredients");
        migrationBuilder.DropTable(name: "GroceryLists");
    }
}