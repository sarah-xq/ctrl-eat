using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace User.Data.Migrations;

public partial class AddUserProfilePicture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureStorageKey",
            table: "Profiles",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureFormat",
            table: "Profiles",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ProfilePictureSizeBytes",
            table: "Profiles",
            type: "INTEGER",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProfilePictureStorageKey",
            table: "Profiles");
        migrationBuilder.DropColumn(
            name: "ProfilePictureFormat",
            table: "Profiles");
        migrationBuilder.DropColumn(
            name: "ProfilePictureSizeBytes",
            table: "Profiles");
    }
}
