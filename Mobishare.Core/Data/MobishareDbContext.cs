using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;

namespace Mobishare.Core.Data
{
    public class MobishareDbContext(DbContextOptions<MobishareDbContext> options) : DbContext(options)
    {

        //tabelle del database
        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Mezzo> Mezzi { get; set; }
        public DbSet<Parcheggio> Parcheggi { get; set; }
        public DbSet<Corsa> Corse { get; set; }
        public DbSet<Ricarica> Ricariche { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Transazione> Transazioni { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Applica automaticamente tutte le configurazioni dalla cartella ModelConfigurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MobishareDbContext).Assembly); 
        }

        //metodo utility richiamabile ovunque per aggiornare il credito
        public async Task RegistraTransazioneAsync(int idUtente, decimal importo, StatoPagamento stato, int? idCorsa = null, int? idRicarica = null)
        {
            var transazione = new Transazione
            {
                IdUtente = idUtente,
                Importo = importo,
                Stato = stato,
                IdCorsa = idCorsa,
                IdRicarica = idRicarica,
                DataTransazione = DateTime.Now
            };

            Transazioni.Add(transazione);
            await SaveChangesAsync();
        }

    }
}
