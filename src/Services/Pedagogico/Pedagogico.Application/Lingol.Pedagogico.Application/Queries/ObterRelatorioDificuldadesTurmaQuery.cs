using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Queries
{
    public record ObterRelatorioDificuldadesTurmaQuery(Guid TurmaId)
        : IRequest<RelatorioDificuldadesTurmaResult>;

    /// <summary>
    /// Dashboard diagnóstico da turma: mapa de lacunas por tipo de dificuldade,
    /// questões que mais derrubaram a turma e a situação aluno a aluno — incluindo
    /// quem ainda não entregou.
    /// </summary>
    public class ObterRelatorioDificuldadesTurmaQueryHandler
        : IRequestHandler<ObterRelatorioDificuldadesTurmaQuery, RelatorioDificuldadesTurmaResult>
    {
        private readonly IPedagogicoDbContext _db;
        private readonly ICadastroClient _cadastroClient;

        public ObterRelatorioDificuldadesTurmaQueryHandler(
            IPedagogicoDbContext db,
            ICadastroClient cadastroClient)
        {
            _db = db;
            _cadastroClient = cadastroClient;
        }

        public async Task<RelatorioDificuldadesTurmaResult> Handle(
            ObterRelatorioDificuldadesTurmaQuery request,
            CancellationToken cancellationToken)
        {
            var turma = await _cadastroClient.ObterTurmaAsync(request.TurmaId, cancellationToken)
                ?? throw new InvalidOperationException("Turma não encontrada no serviço de Cadastro.");

            var alunosCadastro = await _cadastroClient.ObterAlunosDaTurmaAsync(request.TurmaId, cancellationToken);

            var entregas = await _db.RespostasAluno
                .Where(r => r.TurmaId == request.TurmaId)
                .ToListAsync(cancellationToken);

            var dificuldades = await _db.DificuldadesAluno
                .Where(d => d.TurmaId == request.TurmaId)
                .ToListAsync(cancellationToken);

            var atividadesPublicadas = await _db.Atividades
                .CountAsync(a => a.TurmaId == request.TurmaId, cancellationToken);

            // ----------------------------------------------------------
            // Visão por aluno: a lista base é o Cadastro, não as entregas,
            // para que quem não respondeu apareça no painel.
            // ----------------------------------------------------------
            var alunos = new List<DificuldadePorAlunoDto>();

            foreach (var aluno in alunosCadastro.OrderBy(a => a.Nome))
            {
                var entregasAluno = entregas.Where(r => r.AlunoId == aluno.Id).ToList();
                var dificuldadesAluno = dificuldades.Where(d => d.AlunoId == aluno.Id).ToList();

                var comNota = entregasAluno.Where(r => r.Nota.HasValue).ToList();

                alunos.Add(new DificuldadePorAlunoDto(
                    AlunoId: aluno.Id,
                    NomeAluno: aluno.Nome,
                    PerfilAee: aluno.TipoNecessidade,
                    Entregou: entregasAluno.Count > 0,
                    AtividadesEntregues: entregasAluno.Count,
                    TotalAcertos: entregasAluno.Sum(r => r.Acertos),
                    TotalErros: entregasAluno.Sum(r => r.Erros),
                    NotaMedia: comNota.Count > 0
                        ? Math.Round(comNota.Average(r => r.Nota!.Value), 2)
                        : null,
                    Dificuldades: AgruparPorTipo(dificuldadesAluno)));
            }

            // ----------------------------------------------------------
            // Mapa de lacunas da turma
            // ----------------------------------------------------------
            var resumoTurma = dificuldades
                .GroupBy(d => d.Tipo)
                .Select(g => new DificuldadeTurmaResumoDto(
                    Tipo: g.Key,
                    QuantidadeTotal: g.Count(),
                    QuantidadeAlunosAfetados: g.Select(x => x.AlunoId).Distinct().Count(),
                    QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                .OrderByDescending(x => x.QuantidadeTotal)
                .ToList();

            var questoesMaisErradas = await ObterQuestoesCriticasAsync(request.TurmaId, cancellationToken);

            var entregasComNota = entregas.Where(r => r.Nota.HasValue).ToList();

            return new RelatorioDificuldadesTurmaResult(
                TurmaId: request.TurmaId,
                NomeTurma: turma.Nome,
                Ano: turma.Ano,
                TotalAlunos: alunosCadastro.Count,
                AlunosQueEntregaram: entregas.Select(r => r.AlunoId).Distinct().Count(),
                AtividadesPublicadas: atividadesPublicadas,
                NotaMediaTurma: entregasComNota.Count > 0
                    ? Math.Round(entregasComNota.Average(r => r.Nota!.Value), 2)
                    : null,
                ResumoTurma: resumoTurma,
                QuestoesMaisErradas: questoesMaisErradas,
                Alunos: alunos);
        }

        internal static List<DificuldadeResumoDto> AgruparPorTipo(
            IEnumerable<Domain.Entities.DificuldadeAluno> dificuldades) =>
            dificuldades
                .GroupBy(d => d.Tipo)
                .Select(g => new DificuldadeResumoDto(
                    Tipo: g.Key,
                    Quantidade: g.Count(),
                    QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                .OrderByDescending(x => x.Quantidade)
                .ToList();

        /// <summary>
        /// As 10 questões com maior percentual de erro na turma — é o que o professor
        /// precisa bater o olho para saber qual conceito revisar.
        /// </summary>
        private async Task<List<QuestaoCriticaDto>> ObterQuestoesCriticasAsync(
            Guid turmaId,
            CancellationToken ct)
        {
            var dados = await (
                from item in _db.RespostasQuestao
                join entrega in _db.RespostasAluno on item.RespostaAlunoId equals entrega.Id
                join questao in _db.Questoes on item.QuestaoId equals questao.Id
                where entrega.TurmaId == turmaId
                group new { item, questao } by new
                {
                    questao.Id,
                    questao.AtividadeId,
                    questao.Ordem,
                    questao.Enunciado,
                    questao.Habilidade
                }
                into g
                select new
                {
                    g.Key,
                    Respostas = g.Count(),
                    Erros = g.Count(x => !x.item.EstaCorreta)
                })
                .ToListAsync(ct);

            return dados
                .Where(x => x.Erros > 0)
                .Select(x => new QuestaoCriticaDto(
                    QuestaoId: x.Key.Id,
                    AtividadeId: x.Key.AtividadeId,
                    Ordem: x.Key.Ordem,
                    Enunciado: x.Key.Enunciado,
                    Habilidade: x.Key.Habilidade ?? string.Empty,
                    Respostas: x.Respostas,
                    Erros: x.Erros,
                    PercentualErro: Math.Round((decimal)x.Erros / x.Respostas * 100m, 1)))
                .OrderByDescending(x => x.PercentualErro)
                .ThenByDescending(x => x.Erros)
                .Take(10)
                .ToList();
        }
    }
}
