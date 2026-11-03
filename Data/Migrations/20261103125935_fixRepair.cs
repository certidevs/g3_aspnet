using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace G3AspNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class fixRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HourLyRate",
                table: "Repairs",
                newName: "HourlyRate");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Repairs",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Repairs");

            migrationBuilder.RenameColumn(
                name: "HourlyRate",
                table: "Repairs",
                newName: "HourLyRate");
        }
    }
}
