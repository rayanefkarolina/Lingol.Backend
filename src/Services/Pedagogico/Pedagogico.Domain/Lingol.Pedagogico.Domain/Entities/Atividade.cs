using Lingol.Core;

namespace Lingol.Pedagogico.Domain.Entities
{
    /// <summary>
    /// Status de processamento de uma atividade (IA).
    /// Pendente: Aguardando processamento
    /// Processando: Sendo gerada pela IA
    /// Pronta: Disponível para alunos
    /// Erro: Falha no processamento
    /// </summary>
    public enum StatusAtividade
    {
        Pendente = 0,
        Processando = 1,
        Pronta = 2,
        Erro = 3
    }

    /// <summary>
    /// Temas disponíveis para a atividade gamificada. Novos temas entram aqui e
    /// no arquivo de configuração visual correspondente no frontend.
    /// </summary>
    public static class TemasAtividade
    {
        public const string Fantasia = "Fantasia";

        public static readonly string[] Disponiveis = { Fantasia };

        public static bool EhValido(string? tema) =>
            !string.IsNullOrWhiteSpace(tema) &&
            Disponiveis.Any(t => string.Equals(t, tema, StringComparison.OrdinalIgnoreCase));
    }

    public class Atividade : EntityBase
    {
        private readonly List<Questao> _questoes = new();

        public Guid TurmaId { get; private set; }
        public Guid ProfessorId { get; private set; }
        public string Livro { get; private set; } = default!;
        public string CapituloOuAssunto { get; private set; } = default!;
        public string Materia { get; private set; } = "Língua Portuguesa";

        /// <summary>Quantidade de questões solicitada pelo professor (padrão 10).</summary>
        public int NumQuestoes { get; private set; } = 10;

        /// <summary>Forma escolhida pelo professor: questionário simples ou atividade gamificada.</summary>
        public ModoGamificacao Modo { get; private set; } = ModoGamificacao.Simples;

        /// <summary>
        /// Tema visual/narrativo da atividade gamificada (ex.: "Fantasia" = Lingolgard).
        /// Influencia o vocabulário das questões geradas pela IA e a arte do frontend.
        /// </summary>
        public string Tema { get; private set; } = TemasAtividade.Fantasia;

        /// <summary>
        /// Quando preenchido, a atividade e individual (revisao gerada para um
        /// aluno especifico). Nulo significa que vale para a turma inteira.
        /// </summary>
        public Guid? AlunoId { get; private set; }

        /// <summary>Atividade que originou esta revisao, quando houver.</summary>
        public Guid? AtividadeOrigemId { get; private set; }

        public bool EhRevisao => AlunoId.HasValue;

        public StatusAtividade Status { get; private set; } = StatusAtividade.Pendente;
        public DateTime DataCriacao { get; private set; } = DateTime.UtcNow;
        public string? MensagemErro { get; private set; }

        /// <summary>
        /// Cópia do item que foi para a fila de geração, em JSON. A fila vive na
        /// memória do processo: se o servidor reinicia no meio da geração, é por
        /// aqui que a varredura de recuperação sabe exatamente o que reenfileirar
        /// (inclusive o foco da revisão e o contexto AEE, que não ficam em colunas).
        /// </summary>
        public string? PayloadGeracaoJson { get; private set; }
        public IReadOnlyCollection<Questao> Questoes => _questoes;

        private Atividade() { }

        public Atividade(
            Guid turmaId,
            Guid professorId,
            string livro,
            string capituloOuAssunto,
            string materia = "Língua Portuguesa",
            int numQuestoes = 10,
            ModoGamificacao modo = ModoGamificacao.Simples,
            string? tema = null,
            Guid? alunoId = null,
            Guid? atividadeOrigemId = null)
        {
            TurmaId = turmaId;
            ProfessorId = professorId;
            Livro = livro;
            CapituloOuAssunto = capituloOuAssunto;
            Materia = materia;
            NumQuestoes = numQuestoes <= 0 ? 10 : numQuestoes;
            Modo = modo;
            Tema = string.IsNullOrWhiteSpace(tema) ? TemasAtividade.Fantasia : tema;
            AlunoId = alunoId;
            AtividadeOrigemId = atividadeOrigemId;
            Status = StatusAtividade.Pendente;
            DataCriacao = DateTime.UtcNow;
        }

        /// <summary>O tabuleiro do modo gamificado tem 10 slots de itens.</summary>
        public const int MaxQuestoesGamificada = 10;

        public void AdicionarQuestao(Questao questao)
        {
            _questoes.Add(questao);
        }

        public void RegistrarPayloadGeracao(string json)
        {
            PayloadGeracaoJson = json;
        }

        /// <summary>
        /// Devolve a atividade para a fila depois de uma interrupção (reinício do
        /// servidor, por exemplo). Só faz sentido enquanto ela não ficou pronta.
        /// </summary>
        public void MudarStatusParaPendente()
        {
            Status = StatusAtividade.Pendente;
            MensagemErro = null;
        }

        public void MudarStatusParaProcessando()
        {
            Status = StatusAtividade.Processando;
            MensagemErro = null;
        }

        public void MudarStatusParaPronta()
        {
            Status = StatusAtividade.Pronta;
            MensagemErro = null;
        }

        public void MudarStatusParaErro(string mensagem)
        {
            Status = StatusAtividade.Erro;
            MensagemErro = mensagem;
        }
    }
}
