using System.Threading.Channels;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Queue;
using Lingol.Pedagogico.Domain.Entities;
using MediatR;

namespace Lingol.Pedagogico.Application.Commands;

public class CriarAtividadeCommandHandler
    : IRequestHandler<CriarAtividadeCommand, CriarAtividadeResult>
{
    private readonly IPedagogicoDbContext _db;
    private readonly ICadastroClient _cadastroClient;
    private readonly ChannelWriter<GerarAtividadeQueueItem> _queueWriter;

    public CriarAtividadeCommandHandler(
        IPedagogicoDbContext db,
        ICadastroClient cadastroClient,
        ChannelWriter<GerarAtividadeQueueItem> queueWriter)
    {
        _db = db;
        _cadastroClient = cadastroClient;
        _queueWriter = queueWriter;
    }

    public async Task<CriarAtividadeResult> Handle(
        CriarAtividadeCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validar que a turma existe e pertence ao professor autenticado.
        var turma = await _cadastroClient.ObterTurmaAsync(command.TurmaId, cancellationToken)
            ?? throw new InvalidOperationException("Turma não encontrada no serviço de Cadastro.");

        if (turma.ProfessorId != command.ProfessorId)
            throw new UnauthorizedAccessException("A turma informada não pertence a este professor.");

        // 2. Criar a Atividade em estado "Pendente".
        var numQuestoes = command.NumQuestoes <= 0 ? 10 : command.NumQuestoes;

        // O tabuleiro gamificado tem 10 slots de itens, então esse é o teto do modo.
        if (command.Modo == ModoGamificacao.Rpg && numQuestoes > Atividade.MaxQuestoesGamificada)
        {
            throw new InvalidOperationException(
                $"A atividade gamificada aceita no máximo {Atividade.MaxQuestoesGamificada} questões.");
        }

        var tema = string.IsNullOrWhiteSpace(command.Tema) ? TemasAtividade.Fantasia : command.Tema;

        if (!TemasAtividade.EhValido(tema))
            throw new InvalidOperationException($"Tema '{tema}' não está disponível.");

        var atividade = new Atividade(
            command.TurmaId,
            command.ProfessorId,
            command.Livro,
            command.CapituloOuAssunto,
            string.IsNullOrWhiteSpace(command.Materia) ? turma.Materia : command.Materia,
            numQuestoes,
            command.Modo,
            tema);

        _db.Adicionar(atividade);
        await _db.SaveChangesAsync(cancellationToken);

        // 3. Enfileirar para processamento em background (a tela do professor não trava).
        var queueItem = new GerarAtividadeQueueItem
        {
            AtividadeId = atividade.Id,
            TurmaId = command.TurmaId,
            Livro = command.Livro,
            CapituloOuAssunto = command.CapituloOuAssunto,
            Materia = atividade.Materia,
            Ano = turma.Ano,
            NumQuestoes = numQuestoes,
            Modo = command.Modo,
            Tema = tema,
            PerfilAeeContexto = command.PerfilAeeContexto
        };

        await _queueWriter.WriteAsync(queueItem, cancellationToken);

        // 4. Devolver 202 (Accepted) — o processamento continua em segundo plano.
        return new CriarAtividadeResult(
            AtividadeId: atividade.Id,
            Status: atividade.Status.ToString(),
            CriadoEm: atividade.DataCriacao);
    }
}
