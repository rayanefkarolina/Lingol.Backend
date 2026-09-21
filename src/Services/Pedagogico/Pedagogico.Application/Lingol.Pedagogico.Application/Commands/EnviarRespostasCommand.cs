using MediatR;

namespace Lingol.Pedagogico.Application.Commands;

public record RespostaAlunoDto(
    Guid QuestaoId,
    string RespostaEscolhida,
    int TempoSegundos
);

public record EnviarRespostasCommand(
    Guid AtividadeId,
    Guid AlunoId,
    List<RespostaAlunoDto> Respostas
) : IRequest<EnviarRespostasResult>;

/// <summary>Correção questão a questão devolvida ao aluno logo após o envio.</summary>
public record CorrecaoQuestaoDto(
    Guid QuestaoId,
    bool EstaCorreta,
    string GabaritoOuCriterio,
    string? Explicacao
);

public record EnviarRespostasResult(
    Guid RespostaAlunoId,
    int Acertos,
    int Erros,
    int TotalQuestoes,
    decimal? Nota,
    List<CorrecaoQuestaoDto> Correcoes
);
