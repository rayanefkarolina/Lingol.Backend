using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Cadastro.Infrastructure.Persistence
{
    public class CadastroDbContextFactory : IDesignTimeDbContextFactory<CadastroDbContext>
    {
        public CadastroDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CadastroDbContext>();

            // Connection string de desenvolvimento.
            // Você pode pegar de um appsettings, mas em factory é comum usar uma fixa simples.
            var connectionString = "Server=localhost\\SQLEXPRESS03;Database=LingolCadastro;Trusted_Connection=True;TrustServerCertificate=True";

            optionsBuilder.UseSqlServer(connectionString);

            return new CadastroDbContext(optionsBuilder.Options);
        }
    }
}
