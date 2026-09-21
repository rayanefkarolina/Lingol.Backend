using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Lingol.Pedagogico.Infrastructure.Persistence
{
    public class PedagogicoDbContextFactory : IDesignTimeDbContextFactory<PedagogicoDbContext>
    {
        public PedagogicoDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PedagogicoDbContext>();

            // Connection string de desenvolvimento - ajuste conforme seu ambiente local
            var connectionString = "Server=localhost\\SQLEXPRESS;Database=LingolPedagogico;Trusted_Connection=True;TrustServerCertificate=True";

            optionsBuilder.UseSqlServer(connectionString);

            return new PedagogicoDbContext(optionsBuilder.Options);
        }
    }
}
