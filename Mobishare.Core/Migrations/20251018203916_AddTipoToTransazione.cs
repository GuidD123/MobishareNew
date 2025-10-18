using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mobishare.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoToTransazione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Corse_Utenti_UtenteId",
                table: "Corse");

            migrationBuilder.DropIndex(
                name: "IX_Corse_UtenteId",
                table: "Corse");

            migrationBuilder.DropColumn(
                name: "UtenteId",
                table: "Corse");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Transazioni",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Transazioni");

            migrationBuilder.AddColumn<int>(
                name: "UtenteId",
                table: "Corse",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Corse_UtenteId",
                table: "Corse",
                column: "UtenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Corse_Utenti_UtenteId",
                table: "Corse",
                column: "UtenteId",
                principalTable: "Utenti",
                principalColumn: "Id");
        }
    }
}
