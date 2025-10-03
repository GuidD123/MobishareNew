using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mobishare.Core.Models;

namespace Mobishare.Core.ModelConfigurations
{
    public class MezzoConfiguration : IEntityTypeConfiguration<Mezzo>
    {
        public void Configure(EntityTypeBuilder<Mezzo> builder)
        {
            // Configurazione per la matricola - campo obbligatorio e univoco
            // Dal tuo controller vedo che ha validazione per max 10 caratteri
            builder.Property(m => m.Matricola)
                   .IsRequired()
                   .HasMaxLength(10);

            // Indice univoco per matricola (come visto nel controller)
            builder.HasIndex(m => m.Matricola)
                   .IsUnique()
                   .HasDatabaseName("IX_Mezzi_Matricola_Unique");

            // Conversioni enum in stringa per Tipo
            // TipoMezzo: BiciMuscolare, BiciElettrica, MonopattinoElettrico
            builder.Property(m => m.Tipo)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            // Conversioni enum in stringa per Stato
            // StatoMezzo: Disponibile, InUso, NonPrelevabile, Manutenzione
            builder.Property(m => m.Stato)
                   .HasConversion<string>()
                   .HasMaxLength(30)
                   .IsRequired();

            // Livello batteria con range 0-100 (validato nel controller)
            builder.Property(m => m.LivelloBatteria)
                   .IsRequired()
                   .HasDefaultValue(100);

            builder.ToTable(m => { m.HasCheckConstraint("CK_Mezzi_LivelloBatteria", "[LivelloBatteria] >= 0 AND [LivelloBatteria] <= 100"); });

            // Relazione con Parcheggio corrente - navigazione property
            builder.HasOne(m => m.ParcheggioCorrente)
                   .WithMany(p => p.Mezzi)
                   .HasForeignKey(m => m.IdParcheggioCorrente)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indici per performance (basati sulle query nel controller)
            builder.HasIndex(m => m.Stato)
                   .HasDatabaseName("IX_Mezzi_Stato");

            builder.HasIndex(m => m.Tipo)
                   .HasDatabaseName("IX_Mezzi_Tipo");

            builder.HasIndex(m => m.IdParcheggioCorrente)
                   .HasDatabaseName("IX_Mezzi_ParcheggioCorrente");

            // Indice composto per query filtrate per stato e tipo
            builder.HasIndex(m => new { m.Stato, m.Tipo })
                   .HasDatabaseName("IX_Mezzi_Stato_Tipo");

            // Indice per mezzi elettrici con batteria bassa (query frequente nel controller)
            builder.HasIndex(m => new { m.Tipo, m.LivelloBatteria, m.Stato })
                   .HasDatabaseName("IX_Mezzi_Tipo_Batteria_Stato");
        }
    }
}