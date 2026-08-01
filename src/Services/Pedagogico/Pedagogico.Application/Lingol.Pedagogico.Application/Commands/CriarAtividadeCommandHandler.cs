using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;

namespace Lingol.Pedagogico.Application.Commands;

public class CriarAtividadeCommandHandler
    : IRequestHandler<CriarAtividadeCommand, CriarAtividadeResult>
{
    private readonly IPedagogicoDbContext _db;
    private readonly IIaAtividadeService _iaService;

    public CriarAtividadeCommandHandler(
        IPedagogicoDbContext db,
        IIaAtividadeService iaService)
    {
        _db = db;
        _iaService = iaService;
    }

    public async Task<CriarAtividadeResult> Handle(
        CriarAtividadeCommand command,
        CancellationToken cancellationToken)
    {
        // Opcional: validar se a turma existe via CadastroClient ou cache local

        // Chama IA para gerar questões
        var questoesGeradas = await _iaService.GerarAtividadeAsync(
            command.Livro,
            command.CapituloOuAssunto,
            command.Materia,
            command.PerfilAeeContexto,
            cancellationToken);

        // Cria entidade Atividade
        var atividade = new Atividade(
            command.TurmaId,
            command.Livro,
            command.CapituloOuAssunto);

        // Converte QuestaoGeradaDto -> Questao (domain)
        foreach (var q in questoesGeradas)
        {
            var questao = new Questao(
                atividade.Id,           // depende do seu construtor de Questao
                q.Enunciado,
                q.Tipo,
                q.GabaritoOuCriterio,
                q.Alternativas);

            atividade.AdicionarQuestao(questao);
            _db.AddQuestao(questao);
        }

        _db.AddAtividade(atividade);
        await _db.SaveChangesAsync(cancellationToken);

        return new CriarAtividadeResult(
            AtividadeId: atividade.Id,
            Questoes: questoesGeradas);
    }
}
