using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using MediatR;

namespace Lingol.Pedagogico.Application.Queries
{
    public record ObterRelatorioDificuldadesAlunoQuery(Guid TurmaId, Guid AlunoId)
    : IRequest<RelatorioDificuldadesAlunoResult>;

    public class ObterRelatorioDificuldadesAlunoQueryHandler
        : IRequestHandler<ObterRelatorioDificuldadesAlunoQuery, RelatorioDificuldadesAlunoResult>
    {
        private readonly IPedagogicoDbContext _dbContext;
        private readonly ICadastroClient _cadastroClient;

        public ObterRelatorioDificuldadesAlunoQueryHandler(
            IPedagogicoDbContext dbContext,
            ICadastroClient cadastroClient)
        {
            _dbContext = dbContext;
            _cadastroClient = cadastroClient;
        }

        public async Task<RelatorioDificuldadesAlunoResult> Handle(
            ObterRelatorioDificuldadesAlunoQuery request,
            CancellationToken cancellationToken)
        {
            var dificuldadesAluno = await _dbContext.GetDificuldadesAlunoAsync(request.TurmaId, request.AlunoId, cancellationToken);

            var respostasAluno = await _dbContext.GetRespostasAlunoAsync(request.TurmaId, request.AlunoId, cancellationToken);

            var notaMedia = respostasAluno.Any()
                ? respostasAluno.Average(r => r.Nota!.Value)
                : (decimal?)null;

            var dificuldadesResumo = dificuldadesAluno
                .GroupBy(d => d.Tipo)
                .Select(g => new DificuldadeResumoDto(
                    Tipo: g.Key,
                    Quantidade: g.Count(),
                    QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                .ToList();

            var cadastroAluno = await _cadastroClient.ObterAlunoAsync(request.AlunoId, cancellationToken);

            return new RelatorioDificuldadesAlunoResult(
                TurmaId: request.TurmaId,
                AlunoId: request.AlunoId,
                NomeAluno: cadastroAluno?.Nome ?? string.Empty,
                NotaMedia: notaMedia,
                Dificuldades: dificuldadesResumo);
        }
    }
}
