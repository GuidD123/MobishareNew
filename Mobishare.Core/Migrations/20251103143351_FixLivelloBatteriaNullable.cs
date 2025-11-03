using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mobishare.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixLivelloBatteriaNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Mezzi_LivelloBatteria",
                table: "Mezzi");

            migrationBuilder.AlterColumn<int>(
                name: "LivelloBatteria",
                table: "Mezzi",
                type: "INTEGER",
                nullable: true,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Mezzi_LivelloBatteria",
                table: "Mezzi",
                sql: "[LivelloBatteria] IS NULL OR ([LivelloBatteria] >= 0 AND [LivelloBatteria] <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Mezzi_LivelloBatteria",
                table: "Mezzi");

            migrationBuilder.AlterColumn<int>(
                name: "LivelloBatteria",
                table: "Mezzi",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true,
                oldDefaultValue: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Mezzi_LivelloBatteria",
                table: "Mezzi",
                sql: "[LivelloBatteria] >= 0 AND [LivelloBatteria] <= 100");
        }
    }
}
