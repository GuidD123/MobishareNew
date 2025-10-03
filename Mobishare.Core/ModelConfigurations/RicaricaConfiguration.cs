using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class RicaricaConfiguration : IEntityTypeConfiguration<Ricarica>
    {
        public void Configure(EntityTypeBuilder<Ricarica> builder)
        {
            // Importo ricarica obbligatorio con precisione decimale
            builder.Property(r => r.ImportoRicarica)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // Data ricarica con valore di default UTC
            builder.Property(r => r.DataRicarica)
                   .IsRequired();
            //.HasDefaultValueSql("datetime('now')"); // SQLite syntax

            // Conversioni enum in stringa con valori di default
            builder.Property(r => r.Tipo)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();
            //.HasDefaultValue(Core.Enums.TipoRicarica.CartaDiCredito);

            builder.Property(r => r.Stato)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();
                   //.HasDefaultValue(Core.Enums.StatoPagamento.InSospeso);

            // Relazione con Utente
            builder.HasOne(r => r.Utente)
                   .WithMany(u => u.RicaricheUtente)
                   .HasForeignKey(r => r.IdUtente)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indici per performance (basati sulle query nel controller)
            builder.HasIndex(r => r.IdUtente)
                   .HasDatabaseName("IX_Ricariche_IdUtente");

            builder.HasIndex(r => r.DataRicarica)
                   .HasDatabaseName("IX_Ricariche_DataRicarica");

            builder.HasIndex(r => r.Stato)
                   .HasDatabaseName("IX_Ricariche_Stato");

            builder.HasIndex(r => r.Tipo)
                   .HasDatabaseName("IX_Ricariche_Tipo");

            // Indici composti per query complesse del business logic
            builder.HasIndex(r => new { r.IdUtente, r.Stato })
                   .HasDatabaseName("IX_Ricariche_Utente_Stato");

            // Per calcoli saldi (ricariche completate per utente)
            builder.HasIndex(r => new { r.IdUtente, r.Stato, r.ImportoRicarica })
                   .HasDatabaseName("IX_Ricariche_Utente_Stato_Importo");

            // Per statistiche temporali
            builder.HasIndex(r => new { r.DataRicarica, r.Stato })
                   .HasDatabaseName("IX_Ricariche_Data_Stato");

            // Check constraint per importo positivo (business rule)
            builder.ToTable(t => t.HasCheckConstraint("CK_Ricarica_ImportoPositivo", "ImportoRicarica > 0"));
            //Check su ricariche 
            builder.ToTable(t =>
                    t.HasCheckConstraint("CK_Ricarica_ImportoRange", "ImportoRicarica > 0 AND ImportoRicarica <= 1000"));

        }
    }
}