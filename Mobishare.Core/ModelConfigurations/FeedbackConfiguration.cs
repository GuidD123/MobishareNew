using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
    {
        public void Configure(EntityTypeBuilder<Feedback> builder)
        {
            // Conversione enum per valutazione (1-5 stelle)
            builder.Property(f => f.Valutazione)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            // Commento opzionale con lunghezza massima
            builder.Property(f => f.Commento)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            // Data feedback con valore di default
            builder.Property(f => f.DataFeedback)
                   .IsRequired()
                   .HasDefaultValueSql("datetime('now')"); // SQLite syntax

            // Relazione con Utente
            builder.HasOne(f => f.Utente)
                   .WithMany()
                   .HasForeignKey(f => f.IdUtente)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relazione con Corsa
            builder.HasOne(f => f.Corsa)
                   .WithMany()
                   .HasForeignKey(f => f.IdCorsa)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indici per performance (basati sulle query del controller)
            builder.HasIndex(f => f.IdUtente)
                   .HasDatabaseName("IX_Feedback_IdUtente");

            builder.HasIndex(f => f.IdCorsa)
                   .HasDatabaseName("IX_Feedback_IdCorsa");

            builder.HasIndex(f => f.DataFeedback)
                   .HasDatabaseName("IX_Feedback_DataFeedback");

            builder.HasIndex(f => f.Valutazione)
                   .HasDatabaseName("IX_Feedback_Valutazione");

            // Constraint BUSINESS RULE: un utente può dare un solo feedback per corsa
            builder.HasIndex(f => new { f.IdUtente, f.IdCorsa })
                   .IsUnique()
                   .HasDatabaseName("IX_Feedback_Utente_Corsa_Unique");

            // Indici composti per query complesse del business logic
            // Per feedback negativi (Pessimo + Scarso)
            builder.HasIndex(f => new { f.Valutazione, f.DataFeedback })
                   .HasDatabaseName("IX_Feedback_Valutazione_Data");

            // Per JOIN con Corse (feedback per mezzo tramite matricola)
            builder.HasIndex(f => new { f.IdCorsa, f.Valutazione })
                   .HasDatabaseName("IX_Feedback_Corsa_Valutazione");
        }
    }
}