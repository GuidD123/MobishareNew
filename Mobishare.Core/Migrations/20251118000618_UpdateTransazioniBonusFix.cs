using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mobishare.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransazioniBonusFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transazioni_Corse_CorsaId",
                table: "Transazioni");

            migrationBuilder.DropForeignKey(
                name: "FK_Transazioni_Utenti_UtenteId",
                table: "Transazioni");

            migrationBuilder.DropColumn(
                name: "CorsaId",
                table: "Transazioni");

            migrationBuilder.DropColumn(
                name: "UtenteId",
                table: "Transazioni");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transazione_TipoTransazione",
                table: "Transazioni",
                sql: "(Tipo = 'Corsa' AND IdCorsa IS NOT NULL AND IdRicarica IS NULL) OR (Tipo = 'Ricarica' AND IdRicarica IS NOT NULL AND IdCorsa IS NULL) OR (Tipo = 'Bonus' AND IdCorsa IS NULL AND IdRicarica IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Transazione_TipoTransazione",
                table: "Transazioni");

            migrationBuilder.AddColumn<int>(
                name: "CorsaId",
                table: "Transazioni",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtenteId",
                table: "Transazioni",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_CorsaId",
                table: "Transazioni",
                column: "CorsaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_UtenteId",
                table: "Transazioni",
                column: "UtenteId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transazione_TipoTransazione",
                table: "Transazioni",
                sql: "(IdCorsa IS NOT NULL AND IdRicarica IS NULL) OR (IdCorsa IS NULL AND IdRicarica IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Transazioni_Corse_CorsaId",
                table: "Transazioni",
                column: "CorsaId",
                principalTable: "Corse",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transazioni_Utenti_UtenteId",
                table: "Transazioni",
                column: "UtenteId",
                principalTable: "Utenti",
                principalColumn: "Id");
        }
    }
}
