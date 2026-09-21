using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lingol.Pedagogico.Infrastructure.Persistence
{
    public class PedagogicoDbContext : DbContext, IPedagogicoDbContext
    {
        public PedagogicoDbContext(DbContextOptions<PedagogicoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Atividade> Atividades => Set<Atividade>();
        public DbSet<Questao> Questoes => Set<Questao>();
        public DbSet<RespostaAluno> RespostasAluno => Set<RespostaAluno>();
        public DbSet<RespostaQuestao> RespostasQuestao => Set<RespostaQuestao>();
        public DbSet<DificuldadeAluno> DificuldadesAluno => Set<DificuldadeAluno>();

        public void Adicionar<TEntity>(TEntity entity) where TEntity : class
            => Set<TEntity>().Add(entity);

        // ----------------------------------------------------------------
        // Consultas de apoio aos relatórios (IPedagogicoDbContext)
        // ----------------------------------------------------------------

        public async Task<List<DificuldadeAluno>> GetDificuldadesAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default)
        {
            return await DificuldadesAluno
                .Where(d => d.TurmaId == turmaId && d.AlunoId == alunoId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RespostaAluno>> GetRespostasAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default)
        {
            return await RespostasAluno
                .Where(r => r.TurmaId == turmaId && r.AlunoId == alunoId && r.Nota.HasValue)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DificuldadeAluno>> GetDificuldadesByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default)
        {
            return await DificuldadesAluno
                .Where(d => d.TurmaId == turmaId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RespostaAluno>> GetRespostasByTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default)
        {
            return await RespostasAluno
                .Where(r => r.TurmaId == turmaId && r.Nota.HasValue)
                .ToListAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Atividade
            modelBuilder.Entity<Atividade>(entity =>
            {
                entity.ToTable("Atividades");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).ValueGeneratedNever();

                entity.Property(a => a.TurmaId).IsRequired();
                entity.Property(a => a.ProfessorId).IsRequired();

                entity.Property(a => a.Livro)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.CapituloOuAssunto)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.Materia)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(a => a.NumQuestoes)
                    .IsRequired()
                    .HasDefaultValue(10);

                entity.Property(a => a.Modo)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(a => a.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(a => a.DataCriacao)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(a => a.MensagemErro)
                    .HasMaxLength(1000);

                entity.HasIndex(a => a.TurmaId);

                entity.HasMany(a => a.Questoes)
                    .WithOne()
                    .HasForeignKey(q => q.AtividadeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Navigation(a => a.Questoes)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            // Questao
            modelBuilder.Entity<Questao>(entity =>
            {
                entity.ToTable("Questoes");
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Id).ValueGeneratedNever();

                entity.Property(q => q.AtividadeId).IsRequired();
                entity.Property(q => q.Ordem).IsRequired();

                entity.Property(q => q.Enunciado)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(q => q.TipoQuestao)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(q => q.GabaritoOuCriterio)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(q => q.Explicacao)
                    .HasMaxLength(2000);

                entity.Property(q => q.AlternativasJson)
                    .HasColumnName("AlternativasJson")
                    .HasMaxLength(4000);

                entity.HasIndex(q => new { q.AtividadeId, q.Ordem });
            });

            // RespostaAluno (entrega da atividade)
            modelBuilder.Entity<RespostaAluno>(entity =>
            {
                entity.ToTable("RespostasAluno");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedNever();

                entity.Property(r => r.AtividadeId).IsRequired();
                entity.Property(r => r.TurmaId).IsRequired();
                entity.Property(r => r.AlunoId).IsRequired();

                entity.Property(r => r.DataEnvio)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(r => r.Acertos).IsRequired();
                entity.Property(r => r.Erros).IsRequired();
                entity.Property(r => r.TotalQuestoes).IsRequired();

                entity.Property(r => r.CorrecaoProcessada)
                    .HasDefaultValue(false);

                entity.Property(r => r.Nota)
                    .HasPrecision(5, 2);

                entity.Property(r => r.FeedbackGeral)
                    .HasMaxLength(2000);

                entity.HasIndex(r => new { r.AtividadeId, r.AlunoId }).IsUnique();
                entity.HasIndex(r => r.TurmaId);

                entity.HasMany(r => r.Itens)
                    .WithOne()
                    .HasForeignKey(i => i.RespostaAlunoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Navigation(r => r.Itens)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            // RespostaQuestao (resposta item a item)
            modelBuilder.Entity<RespostaQuestao>(entity =>
            {
                entity.ToTable("RespostasQuestao");
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Id).ValueGeneratedNever();

                entity.Property(i => i.RespostaAlunoId).IsRequired();
                entity.Property(i => i.QuestaoId).IsRequired();

                entity.Property(i => i.RespostaEscolhida)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(i => i.EstaCorreta).IsRequired();
                entity.Property(i => i.TempoSegundos).IsRequired();

                entity.HasIndex(i => i.QuestaoId);
            });

            // DificuldadeAluno
            modelBuilder.Entity<DificuldadeAluno>(entity =>
            {
                entity.ToTable("DificuldadesAluno");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id).ValueGeneratedNever();

                entity.Property(d => d.RespostaAlunoId).IsRequired();
                entity.Property(d => d.AtividadeId).IsRequired();
                entity.Property(d => d.TurmaId).IsRequired();
                entity.Property(d => d.AlunoId).IsRequired();
                entity.Property(d => d.QuestaoId).IsRequired();

                entity.Property(d => d.Tipo)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(d => d.Descricao)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.HasIndex(d => new { d.TurmaId, d.AlunoId });
            });
        }
    }
}
