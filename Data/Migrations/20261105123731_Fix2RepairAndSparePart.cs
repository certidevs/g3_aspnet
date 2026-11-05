using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace G3AspNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix2RepairAndSparePart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "SpareParts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepairPartId",
                table: "SpareParts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RepairPartId1",
                table: "SpareParts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_RepairPartId1",
                table: "SpareParts",
                column: "RepairPartId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SpareParts_RepairParts_RepairPartId1",
                table: "SpareParts",
                column: "RepairPartId1",
                principalTable: "RepairParts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpareParts_RepairParts_RepairPartId1",
                table: "SpareParts");

            migrationBuilder.DropIndex(
                name: "IX_SpareParts_RepairPartId1",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "RepairPartId",
                table: "SpareParts");

            migrationBuilder.DropColumn(
                name: "RepairPartId1",
                table: "SpareParts");
        }
    }
}
