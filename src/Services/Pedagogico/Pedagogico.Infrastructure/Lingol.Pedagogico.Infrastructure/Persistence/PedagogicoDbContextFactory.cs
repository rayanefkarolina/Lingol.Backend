using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lingol.Pedagogico.Infrastructure.Persistence
{
    public class PedagogicoDbContextFactory : IDesignTimeDbContextFactory<PedagogicoDbContext>
    {
        // Mesmo UserSecretsId de Lingol.Pedagogico.API.csproj. As ferramentas do EF
        // rodam fora da API, entao precisam do id explicito para achar o cofre.
        private const string UserSecretsId = "48b9a492-dc82-4414-9cac-01db9151cdf6";

        public PedagogicoDbContext CreateDbContext(string[] args)
        {
            // Usada só pelas ferramentas do EF (migrations), nunca pela aplicação.
            // A connection string carrega senha e o repositório é público, então
            // ela vem de fora: user-secrets no seu computador, variável de
            // ambiente no servidor (a variável ganha, se as duas existirem).
            //
            //   dotnet user-secrets set "ConnectionStrings:Pedagogico" "<string>" --project src\Services\Pedagogico\Pedagogico.API\Lingol.Pedagogico.API
            //   dotnet ef database update --project ... --startup-project ...
            var configuracao = new ConfigurationBuilder()
                .AddUserSecrets(UserSecretsId)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuracao.GetConnectionString("Pedagogico")
                ?? throw new InvalidOperationException(
                    "Connection string 'Pedagogico' não encontrada. Grave com "
                    + "dotnet user-secrets set \"ConnectionStrings:Pedagogico\" \"<string>\" "
                    + "--project src/Services/Pedagogico/Pedagogico.API/Lingol.Pedagogico.API, "
                    + "ou defina a variável de ambiente ConnectionStrings__Pedagogico.");

            var optionsBuilder = new DbContextOptionsBuilder<PedagogicoDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new PedagogicoDbContext(optionsBuilder.Options);
        }
    }
}
