using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Application.Queries
{
    public record ObterRelatorioDificuldadesTurmaQuery(Guid TurmaId)
    : IRequest<RelatorioDificuldadesTurmaResult>;

    public class ObterRelatorioDificuldadesTurmaQueryHandler
        : IRequestHandler<ObterRelatorioDificuldadesTurmaQuery, RelatorioDificuldadesTurmaResult>
    {
        private readonly IPedagogicoDbContext _dbContext;
        private readonly ICadastroClient _cadastroClient;

        public ObterRelatorioDificuldadesTurmaQueryHandler(
            IPedagogicoDbContext dbContext,
            ICadastroClient cadastroClient)
        {
            _dbContext = dbContext;
            _cadastroClient = cadastroClient;
        }

        public async Task<RelatorioDificuldadesTurmaResult> Handle(
            ObterRelatorioDificuldadesTurmaQuery request,
            CancellationToken cancellationToken)
        {
            // carregar dificuldades da turma
            var dificuldades = await _dbContext.GetDificuldadesByTurmaAsync(request.TurmaId, cancellationToken);

            // respostas da turma (para calcular nota média)
            var respostas = await _dbContext.GetRespostasByTurmaAsync(request.TurmaId, cancellationToken);

            var alunosIds = dificuldades.Select(d => d.AlunoId)
                .Union(respostas.Select(r => r.AlunoId))
                .Distinct()
                .ToList();

            // dados de turma e alunos via Cadastro
            var turmaCadastro = await _cadastroClient.ObterTurmaAsync(request.TurmaId, cancellationToken);
            var alunosCadastro = await _cadastroClient.ObterAlunosDaTurmaAsync(request.TurmaId, cancellationToken);

            var alunos = new List<DificuldadePorAlunoDto>();

            foreach (var alunoId in alunosIds)
            {
                var dificuldadesAluno = dificuldades
                    .Where(d => d.AlunoId == alunoId)
                    .ToList();

                var respostasAluno = respostas
                    .Where(r => r.AlunoId == alunoId && r.Nota.HasValue)
                    .ToList();

                var notaMedia = respostasAluno.Any()
                    ? respostasAluno.Average(r => r.Nota!.Value)
                    : (decimal?)null;

                var agrupadoPorTipo = dificuldadesAluno
                    .GroupBy(d => d.Tipo)
                    .Select(g => new DificuldadeResumoDto(
                        Tipo: g.Key,
                        Quantidade: g.Count(),
                        QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                    .ToList();

                var cadastroAluno = alunosCadastro.FirstOrDefault(a => a.Id == alunoId);
                var nomeAluno = cadastroAluno?.Nome ?? string.Empty;

                alunos.Add(new DificuldadePorAlunoDto(
                    AlunoId: alunoId,
                    NomeAluno: nomeAluno,
                    NotaMedia: notaMedia,
                    Dificuldades: agrupadoPorTipo));
            }

            // resumo da turma
            var resumoTurma = dificuldades
                .GroupBy(d => d.Tipo)
                .Select(g => new DificuldadeTurmaResumoDto(
                    Tipo: g.Key,
                    QuantidadeTotal: g.Count(),
                    QuantidadeAlunosAfetados: g.Select(x => x.AlunoId).Distinct().Count(),
                    QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                .ToList();

            return new RelatorioDificuldadesTurmaResult(
                TurmaId: request.TurmaId,
                NomeTurma: turmaCadastro?.Nome ?? string.Empty,
                ResumoTurma: resumoTurma,
                Alunos: alunos);
        }
    }
}
