using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class TransazioneConfiguration : IEntityTypeConfiguration<Transazione>
    {
        public void Configure(EntityTypeBuilder<Transazione> builder)
        {
            // Importo con precisione decimale (può essere positivo per ricariche, negativo per corse)
            builder.Property(t => t.Importo)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // Data transazione con valore di default
            builder.Property(t => t.DataTransazione)
                   .IsRequired()
                   .HasDefaultValueSql("datetime('now')"); // SQLite syntax

            // Conversione enum per stato
            builder.Property(t => t.Stato)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            // IdCorsa opzionale (solo per addebiti di corse)
            builder.Property(t => t.IdCorsa)
                   .IsRequired(false);

            // IdRicarica opzionale (solo per ricariche)
            builder.Property(t => t.IdRicarica)
                   .IsRequired(false);

            // Relazione con Utente
            //builder.HasOne<Utente>()
            //       .WithMany()
            //       .HasForeignKey(t => t.IdUtente)
            //       .OnDelete(DeleteBehavior.Restrict);

            // Relazione con Corsa (opzionale)
            //builder.HasOne<Corsa>()
            //       .WithMany()
            //       .HasForeignKey(t => t.IdCorsa)
            //       .OnDelete(DeleteBehavior.Restrict)
            //       .IsRequired(false);

            builder.HasOne(t => t.Utente)
                   .WithMany(u => u.Transazioni)
                    .HasForeignKey(t => t.IdUtente)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Corsa)
                   .WithMany(c => c.Transazioni)
                   .HasForeignKey(t => t.IdCorsa)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Relazione con Ricarica (opzionale)
            builder.HasOne<Ricarica>()
                   .WithMany()
                   .HasForeignKey(t => t.IdRicarica)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.Property(t => t.Tipo)
                    .HasMaxLength(20)
                    .IsRequired();

            // Indici per performance
            builder.HasIndex(t => t.IdUtente)
                   .HasDatabaseName("IX_Transazioni_IdUtente");

            builder.HasIndex(t => t.DataTransazione)
                   .HasDatabaseName("IX_Transazioni_DataTransazione");

            builder.HasIndex(t => t.Stato)
                   .HasDatabaseName("IX_Transazioni_Stato");

            builder.HasIndex(t => t.IdCorsa)
                   .HasDatabaseName("IX_Transazioni_IdCorsa");

            builder.HasIndex(t => t.IdRicarica)
                   .HasDatabaseName("IX_Transazioni_IdRicarica");

            // Indici composti per query complesse
            builder.HasIndex(t => new { t.IdUtente, t.DataTransazione })
                   .HasDatabaseName("IX_Transazioni_Utente_Data");

            builder.HasIndex(t => new { t.IdUtente, t.Stato })
                   .HasDatabaseName("IX_Transazioni_Utente_Stato");

            // Per report e statistiche
            builder.HasIndex(t => new { t.DataTransazione, t.Stato, t.Importo })
                   .HasDatabaseName("IX_Transazioni_Data_Stato_Importo");

            // Business rule constraint: deve avere IdCorsa O IdRicarica (non entrambi, non nessuno)
            builder.ToTable(t => t.HasCheckConstraint("CK_Transazione_TipoTransazione",
                "(Tipo = 'Corsa' AND IdCorsa IS NOT NULL AND IdRicarica IS NULL) OR " + 
                "(Tipo = 'Ricarica' AND IdRicarica IS NOT NULL AND IdCorsa IS NULL) OR " + 
                "(Tipo = 'Bonus' AND IdCorsa IS NULL AND IdRicarica IS NULL)"));
        }
    }
}