using Lingol.Pedagogico.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Abstractions
{
    public interface IPedagogicoDbContext
    {
        Task<List<DificuldadeAluno>> GetDificuldadesAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default);
        Task<List<RespostaAluno>> GetRespostasAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default);

        Task<List<DificuldadeAluno>> GetDificuldadesByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default);
        Task<List<RespostaAluno>> GetRespostasByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Operações de escrita usadas pela camada de aplicação
        void AddAtividade(Atividade atividade);
        void AddQuestao(Questao questao);
    }
}
