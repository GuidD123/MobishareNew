using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class UtenteConfiguration : IEntityTypeConfiguration<Utente>
    {
        public void Configure(EntityTypeBuilder<Utente> builder)
        {
            // Configurazioni per le proprietà
            builder.Property(u => u.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.Cognome)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.Password)
                   .IsRequired()
                   .HasMaxLength(500); // Per hash password

            // Conversione enum in stringa
            builder.Property(u => u.Ruolo)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            // Configurazione per decimali
            builder.Property(u => u.Credito)
                   .HasColumnType("decimal(10,2)")
                   .HasDefaultValue(0);

            builder.Property(u => u.DebitoResiduo)
                   .HasColumnType("decimal(10,2)")
                   .HasDefaultValue(0);

            // Configurazione per il bool
            builder.Property(u => u.Sospeso)
                   .HasDefaultValue(false);

            // Indici per performance
            builder.HasIndex(u => u.Email)
                   .IsUnique();
        }
    }
}