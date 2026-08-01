
using Lingol.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lingol.Cadastro.Infrastructure.Persistence
{
    public class CadastroDbContext : DbContext
    {
        public CadastroDbContext(DbContextOptions<CadastroDbContext> options)
            : base(options)
        {
        }

        public DbSet<Professor> Professores => Set<Professor>();
        public DbSet<Turma> Turmas => Set<Turma>();
        public DbSet<Aluno> Alunos => Set<Aluno>();
        public DbSet<PerfilAee> PerfisAee => Set<PerfilAee>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Professor
            modelBuilder.Entity<Professor>(entity =>
            {
                entity.ToTable("Professores");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Nome)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Email)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(p => p.Email)
                    .IsUnique();

                entity.Property(p => p.SenhaHash)
                    .HasMaxLength(500)
                    .IsRequired();
            });

            // Turma
            modelBuilder.Entity<Turma>(entity =>
            {
                entity.ToTable("Turmas");

                entity.HasKey(t => t.Id);

                entity.Property(t => t.Nome)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(t => t.Materia)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(t => t.ProfessorId)
                    .IsRequired();

                entity.HasOne<Professor>()
                    .WithMany()
                    .HasForeignKey(t => t.ProfessorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Aluno
            modelBuilder.Entity<Aluno>(entity =>
            {
                entity.ToTable("Alunos");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Nome)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(a => a.Matricula)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(a => new { a.Matricula, a.TurmaId })
                    .IsUnique();

                entity.Property(a => a.TurmaId)
                    .IsRequired();

                entity.HasOne<Turma>()
                    .WithMany(t => t.Alunos)
                    .HasForeignKey(a => a.TurmaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PerfilAee
            modelBuilder.Entity<PerfilAee>(entity =>
            {
                entity.ToTable("PerfisAee");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.AlunoId)
                    .IsRequired();

                entity.Property(p => p.TipoNecessidade)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(p => p.Observacoes)
                    .HasMaxLength(2000);

                entity.HasOne<Aluno>()
                    .WithOne(a => a.PerfilAee!)
                    .HasForeignKey<PerfilAee>(p => p.AlunoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
