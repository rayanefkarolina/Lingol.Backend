using Lingol.Pedagogico.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Abstractions
{
    public interface IPedagogicoDbContext
    {
        DbSet<Atividade> Atividades { get; }
        DbSet<Questao> Questoes { get; }
        DbSet<RespostaAluno> RespostasAluno { get; }
        DbSet<RespostaQuestao> RespostasQuestao { get; }
        DbSet<DificuldadeAluno> DificuldadesAluno { get; }

        /// <summary>Adiciona uma entidade ao contexto. Nome em português para não colidir com DbContext.Add.</summary>
        void Adicionar<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
