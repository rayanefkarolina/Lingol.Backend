using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Application.Queries
{
    public record ObterRelatorioAtividadeQuery(Guid AtividadeId)
        : IRequest<RelatorioAtividadeResult>;

    /// <summary>
    /// Desempenho questão a questão de uma atividade: mostra exatamente em qual
    /// item a turma travou.
    /// </summary>
    public class ObterRelatorioAtividadeQueryHandler
        : IRequestHandler<ObterRelatorioAtividadeQuery, RelatorioAtividadeResult>
    {
        private readonly IPedagogicoDbContext _db;
        private readonly ICadastroClient _cadastroClient;

        public ObterRelatorioAtividadeQueryHandler(
            IPedagogicoDbContext db,
            ICadastroClient cadastroClient)
        {
            _db = db;
            _cadastroClient = cadastroClient;
        }

        public async Task<RelatorioAtividadeResult> Handle(
            ObterRelatorioAtividadeQuery request,
            CancellationToken cancellationToken)
        {
            var atividade = await _db.Atividades
                .Include(a => a.Questoes)
                .FirstOrDefaultAsync(a => a.Id == request.AtividadeId, cancellationToken)
                ?? throw new InvalidOperationException("Atividade não encontrada.");

            var entregas = await _db.RespostasAluno
                .Where(r => r.AtividadeId == atividade.Id)
                .ToListAsync(cancellationToken);

            var itens = await (
                from item in _db.RespostasQuestao
                join entrega in _db.RespostasAluno on item.RespostaAlunoId equals entrega.Id
                where entrega.AtividadeId == atividade.Id
                select item)
                .ToListAsync(cancellationToken);

            var questoes = atividade.Questoes
                .OrderBy(q => q.Ordem)
                .Select(q =>
                {
                    var respostas = itens.Where(i => i.QuestaoId == q.Id).ToList();
                    var acertos = respostas.Count(i => i.EstaCorreta);
                    var erros = respostas.Count - acertos;

                    return new QuestaoDesempenhoDto(
                        QuestaoId: q.Id,
                        Ordem: q.Ordem,
                        Enunciado: q.Enunciado,
                        Habilidade: q.Habilidade ?? q.TipoQuestao,
                        Acertos: acertos,
                        Erros: erros,
                        PercentualAcerto: respostas.Count == 0
                            ? 0m
                            : Math.Round((decimal)acertos / respostas.Count * 100m, 1));
                })
                .ToList();

            var dificuldades = await _db.DificuldadesAluno
                .Where(d => d.AtividadeId == atividade.Id)
                .ToListAsync(cancellationToken);

            var resumoDificuldades = dificuldades
                .GroupBy(d => d.Tipo)
                .Select(g => new DificuldadeTurmaResumoDto(
                    Tipo: g.Key,
                    QuantidadeTotal: g.Count(),
                    QuantidadeAlunosAfetados: g.Select(x => x.AlunoId).Distinct().Count(),
                    QuestoesComDificuldade: g.Select(x => x.QuestaoId).Distinct().ToList()))
                .OrderByDescending(x => x.QuantidadeTotal)
                .ToList();

            var alunosDaTurma = await _cadastroClient.ObterAlunosDaTurmaAsync(atividade.TurmaId, cancellationToken);
            var comNota = entregas.Where(r => r.Nota.HasValue).ToList();

            return new RelatorioAtividadeResult(
                AtividadeId: atividade.Id,
                TurmaId: atividade.TurmaId,
                Livro: atividade.Livro,
                Assunto: atividade.CapituloOuAssunto,
                Status: atividade.Status.ToString(),
                TotalQuestoes: atividade.Questoes.Count,
                TotalAlunos: alunosDaTurma.Count,
                Entregas: entregas.Count,
                NotaMedia: comNota.Count > 0
                    ? Math.Round(comNota.Average(r => r.Nota!.Value), 2)
                    : null,
                Questoes: questoes,
                ResumoDificuldades: resumoDificuldades);
        }
    }
}
