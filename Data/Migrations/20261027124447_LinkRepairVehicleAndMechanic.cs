using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace G3AspNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkRepairVehicleAndMechanic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los identificadores antiguos eran enteros y pueden no corresponder a usuarios de Identity.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE RepairRelationshipValidation (
                    Valid INTEGER NOT NULL CONSTRAINT Existing_repairs_need_valid_vehicle_and_mechanic CHECK (Valid = 1)
                );
                INSERT INTO RepairRelationshipValidation (Valid)
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM Repairs AS r
                    WHERE NOT EXISTS (SELECT 1 FROM Vehicles AS v WHERE v.Id = r.VehicleId)
                       OR NOT EXISTS (SELECT 1 FROM AspNetUsers AS u WHERE u.Id = CAST(r.MechanicId AS TEXT))
                ) THEN 0 ELSE 1 END;
                DROP TABLE RepairRelationshipValidation;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MechanicId",
                table: "Repairs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_MechanicId",
                table: "Repairs",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_VehicleId",
                table: "Repairs",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_AspNetUsers_MechanicId",
                table: "Repairs",
                column: "MechanicId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Vehicles_VehicleId",
                table: "Repairs",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_AspNetUsers_MechanicId",
                table: "Repairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Vehicles_VehicleId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_MechanicId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_VehicleId",
                table: "Repairs");

            migrationBuilder.AlterColumn<int>(
                name: "MechanicId",
                table: "Repairs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
