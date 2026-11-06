using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLocation.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialStorageLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    MinimumTemperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaximumTemperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageLocationIngredients",
                columns: table => new
                {
                    StorageLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocationIngredients", x => new { x.StorageLocationId, x.IngredientId });
                    table.CheckConstraint("CK_StorageLocationIngredients_IngredientId_Positive", "\"IngredientId\" > 0");
                    table.ForeignKey(
                        name: "FK_StorageLocationIngredients_StorageLocations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocationIngredients_StorageLocationId_Position",
                table: "StorageLocationIngredients",
                columns: new[] { "StorageLocationId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageLocationIngredients");

            migrationBuilder.DropTable(
                name: "StorageLocations");
        }
    }
}
