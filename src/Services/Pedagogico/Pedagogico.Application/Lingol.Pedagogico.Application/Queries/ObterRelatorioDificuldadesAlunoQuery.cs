using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Queries
{
    public record ObterRelatorioDificuldadesAlunoQuery(Guid TurmaId, Guid AlunoId)
        : IRequest<RelatorioDificuldadesAlunoResult>;

    /// <summary>
    /// Visão individual: histórico de entregas do aluno e as dificuldades que a IA
    /// identificou, agrupadas por tipo.
    /// </summary>
    public class ObterRelatorioDificuldadesAlunoQueryHandler
        : IRequestHandler<ObterRelatorioDificuldadesAlunoQuery, RelatorioDificuldadesAlunoResult>
    {
        private readonly IPedagogicoDbContext _db;
        private readonly ICadastroClient _cadastroClient;

        public ObterRelatorioDificuldadesAlunoQueryHandler(
            IPedagogicoDbContext db,
            ICadastroClient cadastroClient)
        {
            _db = db;
            _cadastroClient = cadastroClient;
        }

        public async Task<RelatorioDificuldadesAlunoResult> Handle(
            ObterRelatorioDificuldadesAlunoQuery request,
            CancellationToken cancellationToken)
        {
            var aluno = await _cadastroClient.ObterAlunoAsync(request.AlunoId, cancellationToken)
                ?? throw new InvalidOperationException("Aluno não encontrado no serviço de Cadastro.");

            var dificuldades = await _db.DificuldadesAluno
                .Where(d => d.TurmaId == request.TurmaId && d.AlunoId == request.AlunoId)
                .ToListAsync(cancellationToken);

            var entregas = await (
                from r in _db.RespostasAluno
                join a in _db.Atividades on r.AtividadeId equals a.Id
                where r.TurmaId == request.TurmaId && r.AlunoId == request.AlunoId
                orderby r.DataEnvio descending
                select new EntregaResumoDto(
                    r.Id,
                    a.Id,
                    a.CapituloOuAssunto,
                    r.Acertos,
                    r.Erros,
                    r.TotalQuestoes,
                    r.Nota,
                    r.DataEnvio,
                    r.CorrecaoProcessada,
                    r.FeedbackGeral))
                .ToListAsync(cancellationToken);

            var comNota = entregas.Where(e => e.Nota.HasValue).ToList();

            return new RelatorioDificuldadesAlunoResult(
                TurmaId: request.TurmaId,
                AlunoId: request.AlunoId,
                NomeAluno: aluno.Nome,
                PerfilAee: aluno.TipoNecessidade,
                AtividadesEntregues: entregas.Count,
                TotalAcertos: entregas.Sum(e => e.Acertos),
                TotalErros: entregas.Sum(e => e.Erros),
                NotaMedia: comNota.Count > 0
                    ? Math.Round(comNota.Average(e => e.Nota!.Value), 2)
                    : null,
                Dificuldades: ObterRelatorioDificuldadesTurmaQueryHandler.AgruparPorTipo(dificuldades),
                Entregas: entregas);
        }
    }
}
