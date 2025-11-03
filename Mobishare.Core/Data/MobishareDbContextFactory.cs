using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mobishare.Core.Data
{
    /// <summary>
    /// Factory per il DbContext utilizzata da Entity Framework Tools a design-time.
    /// Necessaria per eseguire le migrations dal progetto Core.
    /// </summary>
    public class MobishareDbContextFactory : IDesignTimeDbContextFactory<MobishareDbContext>
    {
        public MobishareDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MobishareDbContext>();

            // Connection string temporanea per le migrations
            // IMPORTANTE: questo deve corrispondere al path del database usato in Mobishare.API
            optionsBuilder.UseSqlite("Data Source=../Mobishare.API/mobishare.db");

            return new MobishareDbContext(optionsBuilder.Options);
        }
    }
}
