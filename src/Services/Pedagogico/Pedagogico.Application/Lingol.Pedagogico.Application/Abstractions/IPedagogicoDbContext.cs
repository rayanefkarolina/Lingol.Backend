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

        // Consultas de apoio aos relatórios
        Task<List<DificuldadeAluno>> GetDificuldadesAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default);
        Task<List<RespostaAluno>> GetRespostasAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default);

        Task<List<DificuldadeAluno>> GetDificuldadesByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default);
        Task<List<RespostaAluno>> GetRespostasByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default);

        /// <summary>Adiciona uma entidade ao contexto. Nome em português para não colidir com DbContext.Add.</summary>
        void Adicionar<TEntity>(TEntity entity) where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
