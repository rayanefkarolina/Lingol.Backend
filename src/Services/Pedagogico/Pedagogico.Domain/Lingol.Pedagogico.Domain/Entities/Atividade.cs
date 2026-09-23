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

        public StatusAtividade Status { get; private set; } = StatusAtividade.Pendente;
        public DateTime DataCriacao { get; private set; } = DateTime.UtcNow;
        public string? MensagemErro { get; private set; }
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
            string? tema = null)
        {
            TurmaId = turmaId;
            ProfessorId = professorId;
            Livro = livro;
            CapituloOuAssunto = capituloOuAssunto;
            Materia = materia;
            NumQuestoes = numQuestoes <= 0 ? 10 : numQuestoes;
            Modo = modo;
            Tema = string.IsNullOrWhiteSpace(tema) ? TemasAtividade.Fantasia : tema;
            Status = StatusAtividade.Pendente;
            DataCriacao = DateTime.UtcNow;
        }

        /// <summary>O tabuleiro do modo gamificado tem 10 slots de itens.</summary>
        public const int MaxQuestoesGamificada = 10;

        public void AdicionarQuestao(Questao questao)
        {
            _questoes.Add(questao);
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
