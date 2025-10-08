using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Mobishare.Core.Models;
using Mobishare.Core.Enums;

namespace Mobishare.Core.Data
{
    public class MobishareDbContext(DbContextOptions<MobishareDbContext> options) : DbContext(options)
    {

        // Definiamo le tabelle del database
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

    }
}
