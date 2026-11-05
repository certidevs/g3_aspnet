using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace G3AspNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSparePartReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "SpareParts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "SpareParts");
        }
    }
}
