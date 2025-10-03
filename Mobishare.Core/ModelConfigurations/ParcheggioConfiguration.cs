using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class ParcheggioConfiguration : IEntityTypeConfiguration<Parcheggio>
    {
        public void Configure(EntityTypeBuilder<Parcheggio> builder)
        {
            // Configurazione per Nome (obbligatorio)
            builder.Property(p => p.Nome)
                   .IsRequired()
                   .HasMaxLength(100);

            // Configurazione per Zona (obbligatoria)  
            builder.Property(p => p.Zona)
                   .IsRequired()
                   .HasMaxLength(100);

            // Indirizzo opzionale
            builder.Property(p => p.Indirizzo)
                   .HasMaxLength(255)
                   .IsRequired(false);

            // Capienza deve essere positiva
            builder.Property(p => p.Capienza)
                   .IsRequired();
            builder.ToTable(p =>
            {
                p.HasCheckConstraint("CK_Parcheggi_Capienza", "[Capienza] >= 0");
            });

            // Attivo di default true
            builder.Property(p => p.Attivo)
                   .IsRequired()
                   .HasDefaultValue(true);

            // Indici per performance
            builder.HasIndex(p => p.Nome)
                   .IsUnique() // Nome univoco per parcheggio
                   .HasDatabaseName("IX_Parcheggi_Nome_Unique");

            builder.HasIndex(p => p.Zona)
                   .HasDatabaseName("IX_Parcheggi_Zona");

            builder.HasIndex(p => p.Attivo)
                   .HasDatabaseName("IX_Parcheggi_Attivo");

            // Indice composto per query filtrate per zona e stato attivo
            builder.HasIndex(p => new { p.Zona, p.Attivo })
                   .HasDatabaseName("IX_Parcheggi_Zona_Attivo");

            // Configurazione navigation property verso Mezzi 
            // (è già configurata in MezzoConfiguration con WithMany(p => p.Mezzi))
        }
    }
}