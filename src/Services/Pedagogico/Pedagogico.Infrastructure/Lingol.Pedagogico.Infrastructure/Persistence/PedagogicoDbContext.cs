using Lingol.Pedagogico.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Lingol.Pedagogico.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.Infrastructure.Persistence
{
    public class PedagogicoDbContext : DbContext, IPedagogicoDbContext
    {
        public PedagogicoDbContext(DbContextOptions<PedagogicoDbContext> options)
        : base(options)
        {
        }

        // Implementação dos métodos da abstração IPedagogicoDbContext
        public async Task<List<DificuldadeAluno>> GetDificuldadesAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default)
        {
            return await DificuldadesAluno
                .Where(d => d.TurmaId == turmaId && d.AlunoId == alunoId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RespostaAluno>> GetRespostasAlunoAsync(Guid turmaId, Guid alunoId, CancellationToken cancellationToken = default)
        {
            // join RespostasAluno with Atividades para filtrar pela turma
            return await (from r in RespostasAluno
                          join a in Atividades on r.AtividadeId equals a.Id
                          where a.TurmaId == turmaId
                                && r.AlunoId == alunoId
                                && r.CorrecaoProcessada
                                && r.Nota.HasValue
                          select r)
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
            return await (from r in RespostasAluno
                          join a in Atividades on r.AtividadeId equals a.Id
                          where a.TurmaId == turmaId && r.CorrecaoProcessada
                          select r)
                .ToListAsync(cancellationToken);
        }

        public DbSet<Atividade> Atividades => Set<Atividade>();
        public DbSet<Questao> Questoes => Set<Questao>();
        public DbSet<RespostaAluno> RespostasAluno => Set<RespostaAluno>();
        public DbSet<DificuldadeAluno> DificuldadesAluno => Set<DificuldadeAluno>();

        // Implementações simples das operações de escrita expostas pela abstração
        public void AddAtividade(Atividade atividade) => Atividades.Add(atividade);
        public void AddQuestao(Questao questao) => Questoes.Add(questao);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Atividade
            modelBuilder.Entity<Atividade>(entity =>
            {
                entity.ToTable("Atividades");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Livro)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.CapituloOuAssunto)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.TurmaId)
                    .IsRequired();

                entity.HasMany(a => a.Questoes)
                    .WithOne()
                    .HasForeignKey(q => q.AtividadeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Questao
            modelBuilder.Entity<Questao>(entity =>
            {
                entity.ToTable("Questoes");

                entity.HasKey(q => q.Id);

                entity.Property(q => q.Enunciado)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(q => q.TipoQuestao)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(q => q.GabaritoOuCriterio)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(q => q.AtividadeId)
                    .IsRequired();

                entity.Property(q => q.AlternativasJson)
                    .HasColumnName("AlternativasJson")
                    .HasMaxLength(4000);
            });

            // RespostaAluno
            modelBuilder.Entity<RespostaAluno>(entity =>
            {
                entity.ToTable("RespostasAluno");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.AtividadeId)
                    .IsRequired();

                entity.Property(r => r.AlunoId)
                    .IsRequired();

                entity.Property(r => r.CorrecaoProcessada)
                    .HasDefaultValue(false);

                entity.Property(r => r.Nota);

                entity.Property(r => r.FeedbackGeral)
                    .HasMaxLength(2000);
            });

            // DificuldadeAluno
            modelBuilder.Entity<DificuldadeAluno>(entity =>
            {
                entity.ToTable("DificuldadesAluno");

                entity.HasKey(d => d.Id);

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
            });
        }
    }
}
