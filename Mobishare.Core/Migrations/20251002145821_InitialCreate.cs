using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mobishare.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parcheggi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Zona = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Indirizzo = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Capienza = table.Column<int>(type: "INTEGER", nullable: false),
                    Attivo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcheggi", x => x.Id);
                    table.CheckConstraint("CK_Parcheggi_Capienza", "[Capienza] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cognome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Ruolo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Credito = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    DebitoResiduo = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m),
                    Sospeso = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mezzi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Matricola = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Stato = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    LivelloBatteria = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    IdParcheggioCorrente = table.Column<int>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mezzi", x => x.Id);
                    table.UniqueConstraint("AK_Mezzi_Matricola", x => x.Matricola);
                    table.CheckConstraint("CK_Mezzi_LivelloBatteria", "[LivelloBatteria] >= 0 AND [LivelloBatteria] <= 100");
                    table.ForeignKey(
                        name: "FK_Mezzi_Parcheggi_IdParcheggioCorrente",
                        column: x => x.IdParcheggioCorrente,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ricariche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdUtente = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportoRicarica = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DataRicarica = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Stato = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ricariche", x => x.Id);
                    table.CheckConstraint("CK_Ricarica_ImportoPositivo", "ImportoRicarica > 0");
                    table.CheckConstraint("CK_Ricarica_ImportoRange", "ImportoRicarica > 0 AND ImportoRicarica <= 1000");
                    table.ForeignKey(
                        name: "FK_Ricariche_Utenti_IdUtente",
                        column: x => x.IdUtente,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Corse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdUtente = table.Column<int>(type: "INTEGER", nullable: false),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Stato = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "InCorso"),
                    MatricolaMezzo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IdParcheggioPrelievo = table.Column<int>(type: "INTEGER", nullable: false),
                    DataOraInizio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IdParcheggioRilascio = table.Column<int>(type: "INTEGER", nullable: true),
                    DataOraFine = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CostoFinale = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    SegnalazioneProblema = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Corse_Mezzi_MatricolaMezzo",
                        column: x => x.MatricolaMezzo,
                        principalTable: "Mezzi",
                        principalColumn: "Matricola",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corse_Parcheggi_IdParcheggioPrelievo",
                        column: x => x.IdParcheggioPrelievo,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corse_Parcheggi_IdParcheggioRilascio",
                        column: x => x.IdParcheggioRilascio,
                        principalTable: "Parcheggi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corse_Utenti_IdUtente",
                        column: x => x.IdUtente,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Corse_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdUtente = table.Column<int>(type: "INTEGER", nullable: false),
                    IdCorsa = table.Column<int>(type: "INTEGER", nullable: false),
                    Valutazione = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Commento = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DataFeedback = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Corse_IdCorsa",
                        column: x => x.IdCorsa,
                        principalTable: "Corse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Utenti_IdUtente",
                        column: x => x.IdUtente,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transazioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdUtente = table.Column<int>(type: "INTEGER", nullable: false),
                    UtenteId = table.Column<int>(type: "INTEGER", nullable: true),
                    IdCorsa = table.Column<int>(type: "INTEGER", nullable: true),
                    CorsaId = table.Column<int>(type: "INTEGER", nullable: true),
                    IdRicarica = table.Column<int>(type: "INTEGER", nullable: true),
                    Importo = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Stato = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DataTransazione = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transazioni", x => x.Id);
                    table.CheckConstraint("CK_Transazione_TipoTransazione", "(IdCorsa IS NOT NULL AND IdRicarica IS NULL) OR (IdCorsa IS NULL AND IdRicarica IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Transazioni_Corse_CorsaId",
                        column: x => x.CorsaId,
                        principalTable: "Corse",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transazioni_Corse_IdCorsa",
                        column: x => x.IdCorsa,
                        principalTable: "Corse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transazioni_Ricariche_IdRicarica",
                        column: x => x.IdRicarica,
                        principalTable: "Ricariche",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transazioni_Utenti_IdUtente",
                        column: x => x.IdUtente,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transazioni_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Corse_DataOraInizio",
                table: "Corse",
                column: "DataOraInizio");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_IdParcheggioPrelievo",
                table: "Corse",
                column: "IdParcheggioPrelievo");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_IdParcheggioRilascio",
                table: "Corse",
                column: "IdParcheggioRilascio");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_IdUtente",
                table: "Corse",
                column: "IdUtente");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_Matricola_Stato",
                table: "Corse",
                columns: new[] { "MatricolaMezzo", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Corse_MatricolaMezzo",
                table: "Corse",
                column: "MatricolaMezzo");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_Stato",
                table: "Corse",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_Corse_Stato_DataInizio",
                table: "Corse",
                columns: new[] { "Stato", "DataOraInizio" });

            migrationBuilder.CreateIndex(
                name: "IX_Corse_Utente_Stato",
                table: "Corse",
                columns: new[] { "IdUtente", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Corse_UtenteId",
                table: "Corse",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_Corsa_Valutazione",
                table: "Feedbacks",
                columns: new[] { "IdCorsa", "Valutazione" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_DataFeedback",
                table: "Feedbacks",
                column: "DataFeedback");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_IdCorsa",
                table: "Feedbacks",
                column: "IdCorsa");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_IdUtente",
                table: "Feedbacks",
                column: "IdUtente");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_Utente_Corsa_Unique",
                table: "Feedbacks",
                columns: new[] { "IdUtente", "IdCorsa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_Valutazione",
                table: "Feedbacks",
                column: "Valutazione");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_Valutazione_Data",
                table: "Feedbacks",
                columns: new[] { "Valutazione", "DataFeedback" });

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_Matricola_Unique",
                table: "Mezzi",
                column: "Matricola",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_ParcheggioCorrente",
                table: "Mezzi",
                column: "IdParcheggioCorrente");

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_Stato",
                table: "Mezzi",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_Stato_Tipo",
                table: "Mezzi",
                columns: new[] { "Stato", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_Tipo",
                table: "Mezzi",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Mezzi_Tipo_Batteria_Stato",
                table: "Mezzi",
                columns: new[] { "Tipo", "LivelloBatteria", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcheggi_Attivo",
                table: "Parcheggi",
                column: "Attivo");

            migrationBuilder.CreateIndex(
                name: "IX_Parcheggi_Nome_Unique",
                table: "Parcheggi",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parcheggi_Zona",
                table: "Parcheggi",
                column: "Zona");

            migrationBuilder.CreateIndex(
                name: "IX_Parcheggi_Zona_Attivo",
                table: "Parcheggi",
                columns: new[] { "Zona", "Attivo" });

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_Data_Stato",
                table: "Ricariche",
                columns: new[] { "DataRicarica", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_DataRicarica",
                table: "Ricariche",
                column: "DataRicarica");

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_IdUtente",
                table: "Ricariche",
                column: "IdUtente");

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_Stato",
                table: "Ricariche",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_Tipo",
                table: "Ricariche",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_Utente_Stato",
                table: "Ricariche",
                columns: new[] { "IdUtente", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Ricariche_Utente_Stato_Importo",
                table: "Ricariche",
                columns: new[] { "IdUtente", "Stato", "ImportoRicarica" });

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_CorsaId",
                table: "Transazioni",
                column: "CorsaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_Data_Stato_Importo",
                table: "Transazioni",
                columns: new[] { "DataTransazione", "Stato", "Importo" });

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_DataTransazione",
                table: "Transazioni",
                column: "DataTransazione");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_IdCorsa",
                table: "Transazioni",
                column: "IdCorsa");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_IdRicarica",
                table: "Transazioni",
                column: "IdRicarica");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_IdUtente",
                table: "Transazioni",
                column: "IdUtente");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_Stato",
                table: "Transazioni",
                column: "Stato");

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_Utente_Data",
                table: "Transazioni",
                columns: new[] { "IdUtente", "DataTransazione" });

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_Utente_Stato",
                table: "Transazioni",
                columns: new[] { "IdUtente", "Stato" });

            migrationBuilder.CreateIndex(
                name: "IX_Transazioni_UtenteId",
                table: "Transazioni",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Email",
                table: "Utenti",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Transazioni");

            migrationBuilder.DropTable(
                name: "Corse");

            migrationBuilder.DropTable(
                name: "Ricariche");

            migrationBuilder.DropTable(
                name: "Mezzi");

            migrationBuilder.DropTable(
                name: "Utenti");

            migrationBuilder.DropTable(
                name: "Parcheggi");
        }
    }
}
