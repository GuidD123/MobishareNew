using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class CorsaConfiguration : IEntityTypeConfiguration<Corsa>
    {
        public void Configure(EntityTypeBuilder<Corsa> builder)
        {
            // Conversione enum per stato con default InCorso
            builder.Property(c => c.Stato)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired()
                   .HasDefaultValue(Core.Enums.StatoCorsa.InCorso);

            //Configura la relazione tra Corsa e Mezzo: 
            //ogni corsa è legata ad un mezzo tramite la sua matricola, ogni mezzo può avere + corse (storico), la FK è la stringa MatricolaMezzo in Corsa
            //La chiave principale è Mezzo.Matricola
            //OndDelete.Restrict evita che cancellando un mezzo vengano cancellate anche le sue corse (storico)
            builder.HasOne(c => c.Mezzo)
                   .WithMany(m => m.Corse)
                   .HasForeignKey(c => c.MatricolaMezzo)
                   .HasPrincipalKey(m => m.Matricola)
                   .OnDelete(DeleteBehavior.Restrict);

            // Matricola mezzo obbligatoria (max 10 caratteri come validato nel DTO)
            builder.Property(c => c.MatricolaMezzo)
                   .IsRequired()
                   .HasMaxLength(10);

            // Data inizio sempre valorizzata
            builder.Property(c => c.DataOraInizio)
                   .IsRequired();

            // Data fine opzionale (null finché la corsa non termina)
            builder.Property(c => c.DataOraFine)
                   .IsRequired(false);

            // Costo finale opzionale (calcolato alla fine)
            builder.Property(c => c.CostoFinale)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired(false);

            // Segnalazione problema di default false
            builder.Property(c => c.SegnalazioneProblema)
                   .IsRequired()
                   .HasDefaultValue(false);

            // Relazione con ParcheggioPrelievo (obbligatoria)
            builder.HasOne(c => c.ParcheggioPrelievo)
                   .WithMany(p => p.CorsePrelievo)
                   .HasForeignKey(c => c.IdParcheggioPrelievo)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relazione con ParcheggioRilascio (opzionale)
            builder.HasOne(c => c.ParcheggioRilascio)
                   .WithMany(p => p.CorseRilascio)
                   .HasForeignKey(c => c.IdParcheggioRilascio)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            // Relazione con Utente (tramite IdUtente)
            builder.HasOne<Utente>(c => c.Utente)
                   .WithMany(u => u.Corse)
                   .HasForeignKey(c => c.IdUtente)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indici per performance (basati sulle query più comuni)
            builder.HasIndex(c => c.IdUtente)
                   .HasDatabaseName("IX_Corse_IdUtente");

            builder.HasIndex(c => c.MatricolaMezzo)
                   .HasDatabaseName("IX_Corse_MatricolaMezzo");

            builder.HasIndex(c => c.Stato)
                   .HasDatabaseName("IX_Corse_Stato");

            builder.HasIndex(c => c.DataOraInizio)
                   .HasDatabaseName("IX_Corse_DataOraInizio");

            // Indici composti per query complesse
            builder.HasIndex(c => new { c.IdUtente, c.Stato })
                   .HasDatabaseName("IX_Corse_Utente_Stato");

            builder.HasIndex(c => new { c.Stato, c.DataOraInizio })
                   .HasDatabaseName("IX_Corse_Stato_DataInizio");

            // Per trovare corse in corso per matricola (business logic comune)
            builder.HasIndex(c => new { c.MatricolaMezzo, c.Stato })
                   .HasDatabaseName("IX_Corse_Matricola_Stato");
        }
    }
}