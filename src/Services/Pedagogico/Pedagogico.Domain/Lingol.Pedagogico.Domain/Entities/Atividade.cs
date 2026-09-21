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
            ModoGamificacao modo = ModoGamificacao.Simples)
        {
            TurmaId = turmaId;
            ProfessorId = professorId;
            Livro = livro;
            CapituloOuAssunto = capituloOuAssunto;
            Materia = materia;
            NumQuestoes = numQuestoes <= 0 ? 10 : numQuestoes;
            Modo = modo;
            Status = StatusAtividade.Pendente;
            DataCriacao = DateTime.UtcNow;
        }

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
