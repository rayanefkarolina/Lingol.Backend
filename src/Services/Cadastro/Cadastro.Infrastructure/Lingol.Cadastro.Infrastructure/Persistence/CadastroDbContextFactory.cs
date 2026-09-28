using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lingol.Cadastro.Infrastructure.Persistence
{
    public class CadastroDbContextFactory : IDesignTimeDbContextFactory<CadastroDbContext>
    {
        // Mesmo UserSecretsId de Lingol.Cadastro.API.csproj. As ferramentas do EF
        // rodam fora da API, entao precisam do id explicito para achar o cofre.
        private const string UserSecretsId = "219f46cb-c3e3-417a-b8ad-47359d0c607b";

        public CadastroDbContext CreateDbContext(string[] args)
        {
            // Usada só pelas ferramentas do EF (migrations), nunca pela aplicação.
            // A connection string carrega senha e o repositório é público, então
            // ela vem de fora: user-secrets no seu computador, variável de
            // ambiente no servidor (a variável ganha, se as duas existirem).
            //
            //   dotnet user-secrets set "ConnectionStrings:Cadastro" "<string>" --project src\Services\Cadastro\Cadastro.API\Lingol.Cadastro.API
            //   dotnet ef database update --project ... --startup-project ...
            var configuracao = new ConfigurationBuilder()
                .AddUserSecrets(UserSecretsId)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuracao.GetConnectionString("Cadastro")
                ?? throw new InvalidOperationException(
                    "Connection string 'Cadastro' não encontrada. Grave com "
                    + "dotnet user-secrets set \"ConnectionStrings:Cadastro\" \"<string>\" "
                    + "--project src/Services/Cadastro/Cadastro.API/Lingol.Cadastro.API, "
                    + "ou defina a variável de ambiente ConnectionStrings__Cadastro.");

            var optionsBuilder = new DbContextOptionsBuilder<CadastroDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new CadastroDbContext(optionsBuilder.Options);
        }
    }
}
