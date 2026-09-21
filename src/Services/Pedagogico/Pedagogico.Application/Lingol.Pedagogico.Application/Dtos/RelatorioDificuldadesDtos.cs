using Lingol.Pedagogico.Domain.Entities;

namespace Lingol.Pedagogico.Application.Dtos
{
    // ================================================================
    // Blocos reutilizados
    // ================================================================

    public record DificuldadeResumoDto(
        TipoDificuldade Tipo,
        int Quantidade,
        List<Guid> QuestoesComDificuldade);

    public record DificuldadeTurmaResumoDto(
        TipoDificuldade Tipo,
        int QuantidadeTotal,
        int QuantidadeAlunosAfetados,
        List<Guid> QuestoesComDificuldade);

    /// <summary>Questão que mais derrubou a turma — o "mapa de lacunas".</summary>
    public record QuestaoCriticaDto(
        Guid QuestaoId,
        Guid AtividadeId,
        int Ordem,
        string Enunciado,
        string Habilidade,
        int Respostas,
        int Erros,
        decimal PercentualErro);

    public record EntregaResumoDto(
        Guid RespostaAlunoId,
        Guid AtividadeId,
        string Assunto,
        int Acertos,
        int Erros,
        int TotalQuestoes,
        decimal? Nota,
        DateTime DataEnvio,
        bool DiagnosticoPronto,
        string? FeedbackGeral);

    // ================================================================
    // Relatório da turma (dashboard do professor)
    // ================================================================

    public record DificuldadePorAlunoDto(
        Guid AlunoId,
        string NomeAluno,
        string? PerfilAee,
        bool Entregou,
        int AtividadesEntregues,
        int TotalAcertos,
        int TotalErros,
        decimal? NotaMedia,
        List<DificuldadeResumoDto> Dificuldades);

    public record RelatorioDificuldadesTurmaResult(
        Guid TurmaId,
        string NomeTurma,
        int Ano,
        int TotalAlunos,
        int AlunosQueEntregaram,
        int AtividadesPublicadas,
        decimal? NotaMediaTurma,
        List<DificuldadeTurmaResumoDto> ResumoTurma,
        List<QuestaoCriticaDto> QuestoesMaisErradas,
        List<DificuldadePorAlunoDto> Alunos);

    // ================================================================
    // Relatório individual do aluno
    // ================================================================

    public record RelatorioDificuldadesAlunoResult(
        Guid TurmaId,
        Guid AlunoId,
        string NomeAluno,
        string? PerfilAee,
        int AtividadesEntregues,
        int TotalAcertos,
        int TotalErros,
        decimal? NotaMedia,
        List<DificuldadeResumoDto> Dificuldades,
        List<EntregaResumoDto> Entregas);

    // ================================================================
    // Relatório de uma atividade específica
    // ================================================================

    public record QuestaoDesempenhoDto(
        Guid QuestaoId,
        int Ordem,
        string Enunciado,
        string Habilidade,
        int Acertos,
        int Erros,
        decimal PercentualAcerto);

    public record RelatorioAtividadeResult(
        Guid AtividadeId,
        Guid TurmaId,
        string Livro,
        string Assunto,
        string Status,
        int TotalQuestoes,
        int TotalAlunos,
        int Entregas,
        decimal? NotaMedia,
        List<QuestaoDesempenhoDto> Questoes,
        List<DificuldadeTurmaResumoDto> ResumoDificuldades);
}
